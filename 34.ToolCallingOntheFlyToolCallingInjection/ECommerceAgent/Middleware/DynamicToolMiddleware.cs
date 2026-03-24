using ECommerceAgent.Models;
using ECommerceAgent.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceAgent.Middleware;

public static class DynamicToolMiddleware
{
    // 1. Intercept the agent run -----
    public static async Task<AgentResponse> InjectToolsAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        CancellationToken cancellationToken)
    {
        var enrichedOptions = BuildEnrichedOptions(messages, options);

        return await innerAgent.RunAsync(messages, session, enrichedOptions, cancellationToken);
    }

    // Streaming (required by Use())
    public static async IAsyncEnumerable<AgentResponseUpdate> InjectToolsStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var enrichedOptions = BuildEnrichedOptions(messages, options);
        await foreach (var update in innerAgent.RunStreamingAsync(messages, session, enrichedOptions, cancellationToken))
        {
            yield return update;
        }
    }

    // Shared logic — call from both delegates
    private static ChatClientAgentRunOptions BuildEnrichedOptions(
        IEnumerable<ChatMessage> messages,
        AgentRunOptions? options)
    {
        var userMessage = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;
        var domains = ClassifyIntent(userMessage);
        var resolvedTools = ResolveTools(domains);

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"\n[Middleware] Detected domains: {domains}");
        Console.WriteLine($"[Middleware] Injecting {resolvedTools.Count} tool(s): " +
            $"{string.Join(", ", resolvedTools.Select(t => t.Name))}");
        Console.ResetColor();

        var existingChatOptions = (options as ChatClientAgentRunOptions)?.ChatOptions;
        var mergedChatOptions = new ChatOptions
        {
            Tools = resolvedTools,
            Temperature = existingChatOptions?.Temperature,
            MaxOutputTokens = existingChatOptions?.MaxOutputTokens,
            TopP = existingChatOptions?.TopP,
            ResponseFormat = existingChatOptions?.ResponseFormat,
            ModelId = existingChatOptions?.ModelId,
        };

        return new ChatClientAgentRunOptions(mergedChatOptions);
    }

    // 2. Classify the user message into tool domains
    private static ToolDomain ClassifyIntent(string message)
    {
        var lower = message.ToLowerInvariant();
        var domain = ToolDomain.None;

        // order keywords
        if (lower.ContainsAny("order", "purchase", "bought", "item", "product"))
            domain |= ToolDomain.Order;

        // Return / refund keywords
        if (lower.ContainsAny("return", "refund", "exchange", "cancel", "send back"))
            domain |= ToolDomain.Return;

        // Payment / billing keywords
        if (lower.ContainsAny("charge", "charged", "payment", "invoice", "billing", "double"))
            domain |= ToolDomain.Payment;

        // Shipping / delivery keywords
        if (lower.ContainsAny("ship", "deliver", "track", "tracking", "arrive", "where is"))
            domain |= ToolDomain.Shipping;

        // Default: inject order tools as a fallback
        return domain == ToolDomain.None ? ToolDomain.Order : domain;
    }

    // 3- Map domains --> tool functions
    private static List<AITool> ResolveTools(ToolDomain domains)
    {
        var tools= new List<AITool>();
        if (domains.HasFlag(ToolDomain.Order)) tools.AddRange(OrderTools.GetAll());
        if (domains.HasFlag(ToolDomain.Return)) tools.AddRange(ReturnTools.GetAll());
        if (domains.HasFlag(ToolDomain.Payment)) tools.AddRange(PaymentTools.GetAll());
        if (domains.HasFlag(ToolDomain.Shipping)) tools.AddRange(ShippingTools.GetAll());

        return tools;
    }    
}

// - Helper: multi-keyword string check
file static class StringExtensions
{
    public static bool ContainsAny(this string source, params string[] keywords) => keywords.Any(source.Contains);
}
