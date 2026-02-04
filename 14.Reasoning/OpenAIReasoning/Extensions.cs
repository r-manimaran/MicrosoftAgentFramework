using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAIReasoning;

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

    public static ChatClientAgent CreateAIAgentForOpenAI(
        this ChatClient client,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        string? reasoningeffort = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null) => client.CreateAIAgentForAzureOpenAI(instructions, name, description, tools, reasoningeffort, clientFactory,
            loggerFactory,
            services);


    public static ChatClientAgent CreateAIAgentForAzureOpenAI(
        this ChatClient client,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        string? reasoningeffort = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        ChatOptions options = new();
        if (!string.IsNullOrEmpty(reasoningeffort))
        {
            options.RawRepresentationFactory = _ => new ChatCompletionOptions
            {
#pragma warning disable OPENAI001
                ReasoningEffortLevel = reasoningeffort, // possible values: minimal, low, medium (default), high
#pragma warning restore OPENAI001
            };
        }
        options.Instructions = instructions;
        if (tools?.Count > 0)
        {
            options.Tools = tools;
        }

        IChatClient chatClient = client.AsIChatClient();
        if (clientFactory != null)
        {
            chatClient = clientFactory(chatClient);
        }
        ChatClientAgentOptions clientAgentOptions = new()
        {
            ChatOptions = options,
            Name = name,
            Description = description,
            //Instructions = instructions,
        };

        return new ChatClientAgent(chatClient, clientAgentOptions, loggerFactory, services);
    }
}
