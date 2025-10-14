using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLib.Extensions;

public static class UsageDetailsExtensions
{
    private const string ReasonTokenCountKey = "OutputTokenDetails.ReasoningTokenCount";
    public static long? GetOutputTokensUsedForReasoning(this UsageDetails? usageDetails)
    {
        if (usageDetails?.AdditionalCounts?.TryGetValue(ReasonTokenCountKey, out long reasonTokenCount) ?? false)
        {
            return reasonTokenCount;
        }
        return null;
    }
}
