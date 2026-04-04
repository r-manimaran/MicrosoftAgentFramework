using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentAppMcpConnect.Extensions;

public static class UsageDetailsExtensions
{
    public static void OutputAsInformation(this UsageDetails? usageDetails)
    {
        if (usageDetails == null)
        {
            return;
        }
        Utils.Gray($"- Input Tokens: {usageDetails.InputTokenCount}");
        var output = $"- Output Tokens: {usageDetails.OutputTokenCount} " + $"({usageDetails.ReasoningTokenCount ?? 0} was used for reasoning)";
        Utils.Gray(output);
    }
}
