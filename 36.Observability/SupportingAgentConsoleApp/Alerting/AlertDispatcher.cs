using SupportingAgentConsoleApp.Telemetry;
using System.Diagnostics;
using System.Net.Http.Json;

namespace SupportingAgentConsoleApp.Alerting;

/// <summary>
/// Evaluates eval scores against configured thresholds and dispatches alerts when a breach is detected.
/// 
/// Supports three alert destinations out of the box:
///   - Console : always active, useful for development and debugging
///   - Seq : posts a structured log event at Fatal/Error level
///   - Webhook : posts a JSON payload to any HTTP endpoint 
///              (Slack, Teams, PagerDuty, custom dashboards, etc.)
///              
/// Alert suppression is built in — a cooldown window prevents the same
/// alert firing repeatedly for every turn during a sustained degradation.
/// </summary>
public class AlertDispatcher
{
    private readonly AlertDispatcherOptions _options;
    private readonly HttpClient _http;

    // Tracks the last time each alert key fired so we can suppress duplicates
    // within the cooldown window. Key = "{ticketId}:{dimension}".
    private readonly Dictionary<string, DateTime> _lastFired = new();
    private readonly Lock _lock = new();

    public AlertDispatcher(AlertDispatcherOptions options, HttpClient? http=null)
    {
        _options = options;
        _http = http ?? new HttpClient();
    }

    // ------------------------------
    // Primary Entry Point
    // Called by EvalPipeline after all scorers complete.
    // Checks every dimension and dispatches if threshold is breached.
    // ------------------------------

    public async Task EvaluateAndAlertAsync(string ticketId, 
                    string runId,
                    double relevanceScore,
                    double hallucinationScore,
                    double safetyScore,
                    string? agentResponse = null)
    {
        var checks = new[]
        {
            (Dimension: "safety",         Score: safetyScore,         Threshold: _options.SafetyThreshold,         Severity: AlertSeverity.Critical),
            (Dimension: "hallucination",  Score: hallucinationScore,  Threshold: _options.HallucinationThreshold,  Severity: AlertSeverity.High),
            (Dimension: "relevance",      Score: relevanceScore,      Threshold: _options.RelevanceThreshold,      Severity: AlertSeverity.Medium),
        };

        foreach(var check in checks)
        {
            if (check.Score < check.Threshold)
            {
                var alert = new AlertEvent(
                    TicketId: ticketId,
                    RunId: runId,
                    Dimension: check.Dimension,
                    Score: check.Score,
                    Threshold: check.Threshold,
                    Severity: check.Severity,
                    Message: BuildMessage(check.Dimension, check.Score, check.Threshold, ticketId),
                    FiredAt: DateTime.UtcNow,
                    ResponseSnippet: agentResponse !=null ? Truncate(agentResponse, 200) : null
                );

                await DispatchAsync(alert);
            }
        }
    }


    //------------------------------
    // Dispatch - routes the alert to all configured destinations
    //------------------------------

    private async Task DispatchAsync(AlertEvent alert)
    {
        // Suppress if this dimension already fired within the cooldown window
        var suppressionKey = $"{alert.TicketId}:{alert.Dimension}";

        lock (_lock)
        {
            if (_lastFired.TryGetValue(suppressionKey, out var last) &&
                DateTime.UtcNow - last < _options.CooldownWindow)
            {
                WriteConsole($"[AlertDispatcher] Suppressed ({alert.Dimension}) — " +
                             $"cooldown active until {last + _options.CooldownWindow:HH:mm:ss}",
                             ConsoleColor.DarkYellow);
                return;
            }
            _lastFired[suppressionKey] = DateTime.UtcNow;
        }

        // OTel span so alerts are traceable alongside the agent run
        using var activity = AgentTelemetry.Source.StartActivity("alert.dispatch");
        activity?.SetTag("alert.dimension", alert.Dimension);
        activity?.SetTag("alert.severity", alert.Severity.ToString());
        activity?.SetTag("alert.score", alert.Score);
        activity?.SetTag("ticket.id", alert.TicketId);

        // Always write to console
        DispatchToConsole(alert);

        // Seq structured log (if configured)
        if (_options.SeqEnabled)
            DispatchToSeq(alert);

        // Webhook — Slack / PagerDuty / Teams (if configured)
        if (!string.IsNullOrWhiteSpace(_options.WebhookUrl))
            await DispatchToWebhookAsync(alert);
    }


    //------------------------------
    // Distination 1: Console
    //------------------------------
    private static void DispatchToConsole(AlertEvent alert)
    {
        var color = alert.Severity switch
        {
            AlertSeverity.Critical => ConsoleColor.Red,
            AlertSeverity.High => ConsoleColor.DarkRed,
            AlertSeverity.Medium => ConsoleColor.Yellow,
            _ => ConsoleColor.White
        };
        WriteConsole("", color);
        WriteConsole($"╔══ ALERT [{alert.Severity}] ══════════════════════════════════", color);
        WriteConsole($"║  Dimension : {alert.Dimension}", color);
        WriteConsole($"║  Score     : {alert.Score:F2}  (threshold: {alert.Threshold:F2})", color);
        WriteConsole($"║  Ticket    : {alert.TicketId}", color);
        WriteConsole($"║  Run       : {alert.RunId}", color);
        WriteConsole($"║  Time      : {alert.FiredAt:yyyy-MM-dd HH:mm:ss} UTC", color);
        WriteConsole($"║  Message   : {alert.Message}", color);
        if (alert.ResponseSnippet != null)
            WriteConsole($"║  Snippet   : {alert.ResponseSnippet}", color);
        WriteConsole($"╚══════════════════════════════════════════════════════", color);
        WriteConsole("", color);
    }

    //------------------------------
    // Distination 2: Seq structured log
    // Writes a Fatal/Error level event directly to Seq's raw event ingestion
    // endpoint. Uses the Clef (Compact Log Event Format) line format so Seq
    // indexes every property for filtering.
    //
    // Seq endpoint: POST http://localhost:5341/api/events/raw?clef
    //------------------------------
    private void DispatchToSeq(AlertEvent alert)
    {
        // Emit as OTel span event — Seq picks this up via the OTLP exporter
        // already configured in Program.cs, so no extra HTTP call is needed.
        // The span event appears as a structured log line inside the trace.
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.AddEvent(new ActivityEvent(
                name: "alert.fired",
                tags: new ActivityTagsCollection
                {
                    ["alert.dimension"] = alert.Dimension,
                    ["alert.severity"] = alert.Severity.ToString(),
                    ["alert.score"] = alert.Score,
                    ["alert.threshold"] = alert.Threshold,
                    ["alert.message"] = alert.Message,
                    ["ticket.id"] = alert.TicketId,
                    ["run.id"] = alert.RunId,
                    ["alert.response_snippet"] = alert.ResponseSnippet ?? "",
                }));
        }

        // Fallback: also log to console with structured format
        // so local Seq ingestion via OTLP captures it as a span event
        WriteConsole($"[Seq] Alert event attached to trace span: {alert.Dimension} score={alert.Score:F2}", ConsoleColor.DarkCyan);
    }

    //------------------------------
    // Distination 3: Webhook (Slack / Teams / PagerDuty / Custom)
    // The payload shape auto-adapts based on the WebhookFormat option.
    // ─────────────────────────────────────────────────────────────────────────

    private async Task DispatchToWebhookAsync(AlertEvent alert)
    {
        try
        {
            object payload = _options.WebhookFormat switch
            {
                WebhookFormat.Slack => BuildSlackPayload(alert),
                WebhookFormat.PagerDuty => BuildPagerDutyPayload(alert),
                _ => BuildGenericPayload(alert)
            };

            var response = await _http.PostAsJsonAsync(_options.WebhookUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                WriteConsole($"[AlertDispatcher] Webhook failed: {response.StatusCode}", ConsoleColor.DarkRed);
            }
            else
            {
                WriteConsole($"[AlertDispatcher] Webhook delivered ({_options.WebhookFormat})", ConsoleColor.DarkGreen);
            }
        }
        catch (Exception ex)
        {
            // Webhook failure must never crash the eval pipeline
            WriteConsole($"[AlertDispatcher] Webhook exception: {ex.Message}", ConsoleColor.DarkRed);
        }
    }

    // ── Slack incoming webhook payload ────────────────────────────────────────
    private static object BuildSlackPayload(AlertEvent alert)
    {
        var emoji = alert.Severity switch
        {
            AlertSeverity.Critical => ":red_circle:",
            AlertSeverity.High => ":large_orange_circle:",
            AlertSeverity.Medium => ":large_yellow_circle:",
            _ => ":white_circle:"
        };

        return new
        {
            text = $"{emoji} *AI Agent Alert — {alert.Severity}*",
            attachments = new[]
            {
                new
                {
                    color  = alert.Severity == AlertSeverity.Critical ? "#FF0000" : "#FFA500",
                    fields = new[]
                    {
                        new { title = "Dimension", value = alert.Dimension,           @short = true },
                        new { title = "Score",     value = $"{alert.Score:F2} / 1.00", @short = true },
                        new { title = "Threshold", value = $"{alert.Threshold:F2}",   @short = true },
                        new { title = "Ticket ID", value = alert.TicketId,            @short = true },
                        new { title = "Message",   value = alert.Message,             @short = false },
                    },
                    footer = $"Run: {alert.RunId} | {alert.FiredAt:yyyy-MM-dd HH:mm:ss} UTC"
                }
            }
        };
    }

    // ── PagerDuty Events API v2 payload ───────────────────────────────────────
    private object BuildPagerDutyPayload(AlertEvent alert)
    {
        return new
        {
            routing_key = _options.PagerDutyRoutingKey,
            event_action = "trigger",
            dedup_key = $"{alert.TicketId}:{alert.Dimension}",   // prevents duplicate incidents
            payload = new
            {
                summary = alert.Message,
                severity = alert.Severity switch
                {
                    AlertSeverity.Critical => "critical",
                    AlertSeverity.High => "error",
                    AlertSeverity.Medium => "warning",
                    _ => "info"
                },
                source = "support-agent-console",
                timestamp = alert.FiredAt.ToString("o"),
                custom_details = new
                {
                    dimension = alert.Dimension,
                    score = alert.Score,
                    threshold = alert.Threshold,
                    ticket_id = alert.TicketId,
                    run_id = alert.RunId,
                    response_snippet = alert.ResponseSnippet
                }
            }
        };
    }

    // ── Generic JSON payload (Teams adaptive card / any custom endpoint) ───────
    private static object BuildGenericPayload(AlertEvent alert) => new
    {
        alert_type = "ai_agent_eval",
        severity = alert.Severity.ToString(),
        dimension = alert.Dimension,
        score = alert.Score,
        threshold = alert.Threshold,
        ticket_id = alert.TicketId,
        run_id = alert.RunId,
        message = alert.Message,
        fired_at_utc = alert.FiredAt.ToString("o"),
        response_snippet = alert.ResponseSnippet
    };

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private static string BuildMessage(string dimension, double score, double threshold, string ticketId) =>
        $"{char.ToUpper(dimension[0])}{dimension[1..]} score {score:F2} is below threshold " +
        $"{threshold:F2} for ticket {ticketId}.";

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";

    private static void WriteConsole(string message, ConsoleColor color = ConsoleColor.White)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

}
