using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportingAgentConsoleApp.Alerting;

public class AlertDispatcherOptions
{
    // Score thresholds — breach triggers an alert
    public double SafetyThreshold { get; init; } = 0.7;
    public double HallucinationThreshold { get; init; } = 0.5;
    public double RelevanceThreshold { get; init; } = 0.6;

    // Cooldown — prevents alert storms during sustained degradation
    // Default: same dimension will not re-alert within 5 minutes
    public TimeSpan CooldownWindow { get; init; } = TimeSpan.FromMinutes(5);

    // Seq — uses OTLP span events (no extra config needed if OTLP is set up)
    public bool SeqEnabled { get; init; } = true;

    // Webhook destination
    public string WebhookUrl { get; init; } = string.Empty;
    public WebhookFormat WebhookFormat { get; init; } = WebhookFormat.Generic;

    // PagerDuty — only needed when WebhookFormat = PagerDuty
    public string PagerDutyRoutingKey { get; init; } = string.Empty;
}

public enum WebhookFormat { Generic, Slack, PagerDuty }

public enum AlertSeverity { Low, Medium, High, Critical }

/// <summary>
/// Immutable snapshot of a single alert event — passed to all dispatch destinations.
/// </summary>
public record AlertEvent(
    string TicketId,
    string RunId,
    string Dimension,
    double Score,
    double Threshold,
    AlertSeverity Severity,
    string Message,
    DateTime FiredAt,
    string? ResponseSnippet);