using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace RAGAgent;

public class CostCenter
{
    // ── OpenAI GPT-4o pricing (per 1M tokens, as of 2025) ──────────────
    // Update these if you switch models.
    private readonly decimal _inputPricePerMillion;
    private readonly decimal _outputPricePerMillion;
    private readonly string _modelName;

    // ── Cumulative counters ─────────────────────────────────────────────
    private long _totalInputTokens;
    private long _totalOutputTokens;
    private int _totalTurns;

    // ── Known model pricing table (per 1M tokens, USD) ──────────────────
    private static readonly Dictionary<string, (decimal Input, decimal Output)> ModelPricing = new()
    {
        ["gpt-4o"] = (2.50m, 10.00m),
        ["gpt-4o-mini"] = (0.15m, 0.60m),
        ["gpt-4-turbo"] = (10.00m, 30.00m),
        ["gpt-4"] = (30.00m, 60.00m),
        ["gpt-3.5-turbo"] = (0.50m, 1.50m),
        ["o1"] = (15.00m, 60.00m),
        ["o1-mini"] = (3.00m, 12.00m),
    };

    public CostCenter(string modelName)
    {
        _modelName = modelName;

        // Look up pricing; fall back to gpt-4o rates if model unknown
        var key = ModelPricing.Keys
            .FirstOrDefault(k => modelName.Contains(k, StringComparison.OrdinalIgnoreCase))
            ?? "gpt-4o";

        (_inputPricePerMillion, _outputPricePerMillion) = ModelPricing[key];
    }

    // ── Public read-only properties ─────────────────────────────────────
    public long TotalInputTokens => _totalInputTokens;
    public long TotalOutputTokens => _totalOutputTokens;
    public long TotalTokens => _totalInputTokens + _totalOutputTokens;
    public int TotalTurns => _totalTurns;
    public decimal TotalCostUsd => CalculateCost(_totalInputTokens, _totalOutputTokens);

    /// <summary>Records token usage from a single AgentResponse.</summary>
    public void Record(UsageDetails? usage)
    {
        if (usage is null) return;

        _totalInputTokens += usage.InputTokenCount ?? 0;
        _totalOutputTokens += usage.OutputTokenCount ?? 0;
        _totalTurns++;
    }

    /// <summary>Returns cost for arbitrary token counts (useful for per-turn display).</summary>
    public decimal CalculateCost(long inputTokens, long outputTokens)
    {
        var inputCost = (inputTokens / 1_000_000m) * _inputPricePerMillion;
        var outputCost = (outputTokens / 1_000_000m) * _outputPricePerMillion;
        return Math.Round(inputCost + outputCost, 6);
    }

    /// <summary>Prints a formatted cost summary to the console.</summary>
    public void PrintSummary()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("════════════════════════════════════════════");
        Console.WriteLine("             SESSION COST SUMMARY            ");
        Console.WriteLine("════════════════════════════════════════════");
        Console.ResetColor();

        Console.WriteLine($"  Model             : {_modelName}");
        Console.WriteLine($"  Total Turns       : {_totalTurns}");
        Console.WriteLine($"  Input  Tokens     : {_totalInputTokens,10:N0}");
        Console.WriteLine($"  Output Tokens     : {_totalOutputTokens,10:N0}");
        Console.WriteLine($"  Total  Tokens     : {TotalTokens,10:N0}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ──────────────────────────────────────");
        Console.WriteLine($"  Estimated Cost    :    ${TotalCostUsd:F6} USD");
        Console.WriteLine($"  (Input rate: ${_inputPricePerMillion}/1M | Output rate: ${_outputPricePerMillion}/1M)");
        Console.ResetColor();
        Console.WriteLine("════════════════════════════════════════════");
    }
}
