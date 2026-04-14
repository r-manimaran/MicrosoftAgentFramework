using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Shared.Helpers;

/// <summary>
/// Calculates estimated USD cost for an LLM call based on token counts.
/// Used by the agent pipeline to record rag.query.cost_usd metrics.
///
/// Prices are approximate and should be kept in sync with Azure OpenAI pricing.
/// https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/
/// </summary>
public static class TokenCostCalculator
{
    // Cost per 1000 tokens in USD
    private static readonly Dictionary<string, (double InputPer1K, double OutputPer1K)> Pricing = new(StringComparer.Ordinal)
    {

        ["gpt-4o"] = (0.005, 0.015),
        ["gpt-4o-mini"] = (0.00015, 0.0006),
        ["text-embedding-3-large"] = (0.00013, 0.0),
        ["text-embedding-3-small"] = (0.00002, 0.0),
    };

    /// <summary>
    /// Calculates the estimated cost for a single LLM call.
    /// Returns 0 if the model name is not in the pricing table.
    /// </summary>
    public static double Calculate(string modelName, int promptTokens, int completionTokens)
    {
        if (!Pricing.TryGetValue(modelName, out var price))
            return 0;

        return (promptTokens / 1000.0 * price.InputPer1K) 
            + (completionTokens / 1000.0 * price.OutputPer1K);
    }
}
