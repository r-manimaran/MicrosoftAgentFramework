using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportingAgentConsoleApp.Telemetry;

public class CostTracker
{
    private readonly int _inputCentsPer1k;
    private readonly int _outputCentsPer1k;

    public CostTracker(int inputCentsPer1k, int outputCentsPer1k)
    {
        _inputCentsPer1k = inputCentsPer1k;
        _outputCentsPer1k = outputCentsPer1k;
    }

    public void TrackUsage(long inputTokens, long outputTokens, string ticketId, string productLine)
    {
        AgentTelemetry.TokensUsed.Add(inputTokens,
            new KeyValuePair<string, object?>("token.type", "input"),
            new KeyValuePair<string, object?>("product.line", productLine),
            new KeyValuePair<string, object?>("ticket.id", ticketId));

        AgentTelemetry.TokensUsed.Add(outputTokens,
            new KeyValuePair<string, object?>("token.type", "output"),
            new KeyValuePair<string, object?>("product.line", productLine),
            new KeyValuePair<string, object?>("ticket.id", ticketId));

        // Emit cost as a custom metric (in USD micro-dollars for int precision)
        double costUsd = (inputTokens / 1000.0 * _inputCentsPer1k +
                          outputTokens / 1000.0 * _outputCentsPer1k) / 100.0;

        // Use OTel histogram so App Insights can show P50/P95/P99 cost distribution
        AgentTelemetry.LatencyMs.Record(costUsd * 1_000_000,
            new KeyValuePair<string, object?>("metric.type", "cost_usd"),
            new KeyValuePair<string, object?>("product.line", productLine));
    }
}
