using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using SupportingAgentConsoleApp.Evaluation;
using SupportingAgentConsoleApp.Middleware;
using SupportingAgentConsoleApp.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportingAgentConsoleApp;

public class SupportAgentService
{
    private readonly AIAgent _agent;
    private readonly PromptLogger _promptLogger;
    private readonly EvalPipeline _evalPipeline;
    private readonly CostTracker _costTracker;
    private readonly ILogger<SupportAgentService> _logger;

    public SupportAgentService(
        AIAgent agent,
        PromptLogger promptLogger,
        EvalPipeline evalPipeline,
        CostTracker costTracker,
        ILogger<SupportAgentService> logger)
    {
        _agent = agent;
        _promptLogger = promptLogger;
        _evalPipeline = evalPipeline;
        _costTracker = costTracker;
        _logger = logger;
    }

    public async Task<string> HandleTicketAsync(
        string ticketId, string userId, string message,
        string productLine, AgentSession session,
        CancellationToken ct = default)
    {
        var runId = Guid.NewGuid().ToString("N");

        // ── Span wraps the entire ticket interaction ──────────────────────────
        using var activity = AgentTelemetry.StartAgentRun(ticketId, "SupportAgent", userId);
        activity?.SetTag("run.id", runId);
        activity?.SetTag("product.line", productLine);

        var sw = Stopwatch.StartNew();
        try
        {
            var maskedInput = PiiMaskingMiddleware.Mask(message);

            AgentTelemetry.RequestCounter.Add(1,
                new("product.line", productLine),
                new("ticket.id", ticketId));

            // ── Call the MAF agent ────────────────────────────────────────────
            var response = await _agent.RunAsync(maskedInput, session);
            sw.Stop();

            var maskedResponse = PiiMaskingMiddleware.Mask(response.Text);

            // Pull token usage from the underlying response
            var inputTokens = response.Usage?.InputTokenCount ?? 0;
            var outputTokens = response.Usage?.OutputTokenCount ?? 0;

            // ── Telemetry ─────────────────────────────────────────────────────
            AgentTelemetry.LatencyMs.Record(sw.Elapsed.TotalMilliseconds,
                new("product.line", productLine),
                new("ticket.id", ticketId));

            activity?.SetTag("tokens.input", inputTokens);
            activity?.SetTag("tokens.output", outputTokens);
            activity?.SetTag("latency_ms", sw.Elapsed.TotalMilliseconds);

            _costTracker.TrackUsage(inputTokens, outputTokens, ticketId, productLine);

            // ── Prompt log (async, non-blocking) ─────────────────────────────
            _ = _promptLogger.LogAsync(new PromptLogEntry(
                                    runId, ticketId, userId,
                                    maskedInput, maskedResponse,
                                    inputTokens, outputTokens,
                                    sw.Elapsed.TotalMilliseconds,
                                    "gpt-4o", DateTime.UtcNow));

            // ── Eval pipeline (fire-and-forget, doesn't delay user) ───────────
            _ = _evalPipeline.RunAndPersistAsync(runId, ticketId, maskedInput, maskedResponse);

            _logger.LogInformation(
                "Ticket {TicketId} resolved in {LatencyMs:F0}ms | tokens: {In}+{Out}",
                ticketId, sw.Elapsed.TotalMilliseconds, inputTokens, outputTokens);

            return response.Text;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            _logger.LogError(ex, "Agent run failed for ticket {TicketId}", ticketId);
            throw;
        }
    }
}
