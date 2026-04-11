using Microsoft.Data.SqlClient;
using SupportingAgentConsoleApp.Alerting;
using SupportingAgentConsoleApp.Evaluation.Scorers;
using SupportingAgentConsoleApp.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportingAgentConsoleApp.Evaluation;

public class EvalPipeline
{
    private readonly RelevanceScorer _relevanceScorer;
    private readonly HallucinationScorer _hallucinationScorer;
    private readonly SafetyScorer _safetyScorer;
    private readonly AlertDispatcher _alertDispatcher;
    private readonly string _evalConnString;

    public EvalPipeline(RelevanceScorer relevance,
        HallucinationScorer hallucination,
        SafetyScorer safety,
        AlertDispatcher alertDispatcher,
        string evalConnString)
    {
        _relevanceScorer = relevance;
        _hallucinationScorer = hallucination;
        _safetyScorer = safety;
        _alertDispatcher = alertDispatcher;
        _evalConnString = evalConnString;
    }

    public async Task RunAndPersistAsync(
        string runId, string ticketId, string query, string response)
    {
        using var activity = AgentTelemetry.Source.StartActivity("eval.pipeline");
        activity?.SetTag("run.id", runId);
        activity?.SetTag("ticket.id", ticketId);

        // ── Run all three scorers in parallel ─────────────────────────────
        // Each scorer is independent — no reason to wait for one before
        // starting the next. Task.WhenAll gives us the wall-clock time of
        // the slowest scorer, not the sum of all three.
        var relevanceTask = _relevanceScorer.ScoreAsync(query, response);
        var hallucinationTask = _hallucinationScorer.ScoreAsync(query, response);
        var safetyTask = _safetyScorer.ScoreAsync(response);

        await Task.WhenAll(relevanceTask, hallucinationTask, safetyTask);

        var relevanceResult = relevanceTask.Result;
        var hallucinationResult = hallucinationTask.Result;
        var safetyResult = safetyTask.Result;

        // ── Extract plain double scores ───────────────────────────────────
        // Implicit conversion operators on each result record handle this
        double relevanceScore = relevanceResult;
        double hallucinationScore = hallucinationResult;
        double safetyScore = safetyResult;

       // Record as OTel metrics (visible in App Insights custom metrics)
        AgentTelemetry.EvalScore.Record(relevanceScore, new KeyValuePair<string, object?>("eval.dimension", "relevance"), new KeyValuePair<string, object?>("ticket.id", ticketId));
        AgentTelemetry.EvalScore.Record(hallucinationScore, new KeyValuePair<string, object?>("eval.dimension", "hallucination"), new KeyValuePair<string, object?>("ticket.id", ticketId));
        AgentTelemetry.EvalScore.Record(safetyScore, new KeyValuePair<string, object?>("eval.dimension", "safety"), new KeyValuePair<string, object?>("ticket.id", ticketId));

        if (safetyScore < 0.5)
            AgentTelemetry.SafetyViolations.Add(1, new KeyValuePair<string, object?>("ticket.id", ticketId));

        // Log all scores to console so they are visible during local dev
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"[Eval] Ticket={ticketId} " +
                          $"Relevance={relevanceScore:F2} " +
                          $"Hallucination={hallucinationScore:F2} " +
                          $"Safety={safetyScore:F2}");
        Console.ResetColor();

        // ── Dispatch alerts if needed ─────────────────────────────────────
        // Checks all three scores against thresholds in a single call.
        // Handles suppression, Seq span events, and webhook delivery.
        await _alertDispatcher.EvaluateAndAlertAsync(
            ticketId: ticketId,
            runId: runId,
            relevanceScore: relevanceScore,
            hallucinationScore: hallucinationScore,
            safetyScore: safetyScore,
            agentResponse: response);

        // -- Persist to eval store ───────────────────────────────────────────────

        if (!string.IsNullOrWhiteSpace(_evalConnString))
        {
            var result = new EvalResult(
                RunId: runId,
                TicketId: ticketId,
                RelevanceScore: relevanceScore,
                HallucinationScore: hallucinationScore,
                SafetyScore: safetyScore,
                HallucinationRisk: hallucinationResult.Risk.ToString(),
                SafetyViolations: string.Join(",", safetyResult.Violations),
                EvaluatedAt: DateTime.UtcNow);

            await PersistAsync(result);
        }
    }

    private async Task PersistAsync(EvalResult result)
    {
        await using var conn = new SqlConnection(_evalConnString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO AgentEvaluations
                (RunId, TicketId, RelevanceScore, HallucinationScore, SafetyScore,HallucinationRisk, SafetyViolations, EvaluatedAt)
            VALUES
                (@RunId, @TicketId, @Relevance, @Hallucination, @Safety,@HallucinationRisk, @SafetyViolations, @EvaluatedAt)
            """;
        cmd.Parameters.AddWithValue("@RunId", result.RunId);
        cmd.Parameters.AddWithValue("@TicketId", result.TicketId);
        cmd.Parameters.AddWithValue("@Relevance", result.RelevanceScore);
        cmd.Parameters.AddWithValue("@Hallucination", result.HallucinationScore);
        cmd.Parameters.AddWithValue("@Safety", result.SafetyScore);
        cmd.Parameters.AddWithValue("@HallucinationRisk", result.HallucinationRisk);
        cmd.Parameters.AddWithValue("@SafetyViolations", result.SafetyViolations);
        cmd.Parameters.AddWithValue("@EvaluatedAt", result.EvaluatedAt);
        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            AgentTelemetry.Source
                .CreateActivity("eval.persistence.failure", System.Diagnostics.ActivityKind.Internal)
                ?.AddTag("ticket.id", result.TicketId)
                .SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Eval] Failed to persist evaluation for Ticket={result.TicketId}: {ex.Message}");
            Console.ResetColor();
        }
    }
}
