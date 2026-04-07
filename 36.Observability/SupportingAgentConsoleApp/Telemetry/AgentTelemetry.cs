using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportingAgentConsoleApp.Telemetry;

public static class AgentTelemetry
{
    public const string SourceName = "SupportAgent";

    // ActivitySource drives OTel traces
    public static readonly ActivitySource Source = new ActivitySource(SourceName, "1.0.0");

    // Meter drives OTel metrics
    public static readonly Meter Meter = new Meter(SourceName, "1.0.0");
    public static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>("agent.requests.total");
    public static readonly Histogram<double> LatencyMs = Meter.CreateHistogram<double>("agent.latency_ms");
    public static readonly Counter<long> TokensUsed = Meter.CreateCounter<long>("agent.tokens.used");
    public static readonly Histogram<double> EvalScore = Meter.CreateHistogram<double>("agent.eval.score");
    public static readonly Counter<long> SafetyViolations = Meter.CreateCounter<long>("agent.safety.violations");

    public static Activity? StartAgentRun(string ticketId, string agentName, string userId)
    {
        var activity = Source.StartActivity("agent.run", ActivityKind.Server);
        activity?.SetTag("ticket.id", ticketId);
        activity?.SetTag("agent.name", agentName);
        activity?.SetTag("user.id", userId);
        activity?.SetTag("service.name", "support-agent");
        return activity;
    }

}
