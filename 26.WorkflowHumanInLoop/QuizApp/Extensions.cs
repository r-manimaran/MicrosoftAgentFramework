using Microsoft.Extensions.AI;

namespace QuizApp;

public static class Extensions
{
    public static void OutputAsInformation(this UsageDetails usageDetails)
    {
        Utils.WriteLineInformation("************************************");
        Utils.WriteLineInformation($"- Input Tokens:{usageDetails?.InputTokenCount}");
        Utils.WriteLineInformation($"- Output Tokens:{usageDetails?.OutputTokenCount}" +
            $"({usageDetails?.GetOutputTokensUsedForReasoning()} was used for reasoning)");
        Utils.Separator();
    }


    // private const string ReasonTokenCountKey = "OutputTokenDetails.ReasoningTokenCount";
    private const string ReasonTokenCountKey = "OutputTokenDetails.ReasoningTokenCount";
    public static long? GetOutputTokensUsedForReasoning(this UsageDetails? usageDetails)
    {
        //if (usageDetails?.AdditionalCounts?.TryGetValue(ReasonTokenCountKey, out long reasonTokenCount) ?? false)
        return usageDetails?.ReasoningTokenCount;
    }   
}
