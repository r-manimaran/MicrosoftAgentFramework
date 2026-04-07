using Microsoft.Data.SqlClient;
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
    private readonly string _evalConnString;

    public EvalPipeline(RelevanceScorer relevance,
        HallucinationScorer hallucination,
        SafetyScorer safety,
        string evalConnString)
    {
        _relevanceScorer = relevance;
        _hallucinationScorer = hallucination;
        _safetyScorer = safety;
        _evalConnString = evalConnString;
    }

    public async Task RunAndPersistAsync(
        string runId, string ticketId, string query, string response)
    {
        using var activity = AgentTelemetry.Source.StartActivity("eval.pipeline");
        activity?.SetTag("run.id", runId);

        // All three scorers run in parallel
        var relevanceTask = _relevanceScorer.ScoreAsync(query, response);
        var hallucinationTask = _hallucinationScorer.ScoreAsync(query, response);
        var safetyTask = _safetyScorer.ScoreAsync(response);
        await Task.WhenAll(relevanceTask, hallucinationTask, safetyTask);
        double relevance = relevanceTask.Result;
        double hallucination = hallucinationTask.Result;
        SafetyScorerResult safetyResult = safetyTask.Result;
        double safety = safetyResult;

        var result = new EvalResult(runId, ticketId, relevance, hallucination, safety,
                                    DateTime.UtcNow);

        // Record as OTel metrics (visible in App Insights custom metrics)
        AgentTelemetry.EvalScore.Record(relevance, new KeyValuePair<string, object?>("eval.dimension", "relevance"), new KeyValuePair<string, object?>("ticket.id", ticketId));
        AgentTelemetry.EvalScore.Record(hallucination, new KeyValuePair<string, object?>("eval.dimension", "hallucination"), new KeyValuePair<string, object?>("ticket.id", ticketId));
        AgentTelemetry.EvalScore.Record(safety, new KeyValuePair<string, object?>("eval.dimension", "safety"), new KeyValuePair<string, object?>("ticket.id", ticketId));

        if (safety < 0.5)
            AgentTelemetry.SafetyViolations.Add(1, new KeyValuePair<string, object?>("ticket.id", ticketId));

        await PersistAsync(result);
    }

    private async Task PersistAsync(EvalResult result)
    {
        await using var conn = new SqlConnection(_evalConnString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO AgentEvaluations
                (RunId, TicketId, RelevanceScore, HallucinationScore, SafetyScore, EvaluatedAt)
            VALUES
                (@RunId, @TicketId, @Relevance, @Hallucination, @Safety, @EvaluatedAt)
            """;
        cmd.Parameters.AddWithValue("@RunId", result.RunId);
        cmd.Parameters.AddWithValue("@TicketId", result.TicketId);
        cmd.Parameters.AddWithValue("@Relevance", result.RelevanceScore);
        cmd.Parameters.AddWithValue("@Hallucination", result.HallucinationScore);
        cmd.Parameters.AddWithValue("@Safety", result.SafetyScore);
        cmd.Parameters.AddWithValue("@EvaluatedAt", result.EvaluatedAt);
        await cmd.ExecuteNonQueryAsync();
    }
}
