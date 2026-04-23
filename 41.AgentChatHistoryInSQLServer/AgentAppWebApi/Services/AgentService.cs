using AgentAppWebApi.Config;
using AgentAppWebApi.Models;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using System.ClientModel;
using System.Collections.Concurrent;

namespace AgentAppWebApi.Services;


public interface IAgentService
{
    Task<string> ChatAsync(UserContext user, string message, CancellationToken ct = default);
}
public class AgentService : IAgentService, IAsyncDisposable
{
    private readonly ChatClientAgent _agent;

    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();
    public AgentService(SqlServerVectorStore vectorStore, AzureOpenAIConfig config, 
        ILogger<AgentService> logger)
    {
        var azureClient = new AzureOpenAIClient(
           new Uri(config.Endpoint),
           new ApiKeyCredential(config.ApiKey));

        IChatClient chatClient = azureClient
            .GetChatClient(config.ChatDeployment)
            .AsIChatClient();
    }
    public async Task<string> ChatAsync(UserContext user, string message, CancellationToken ct = default)
    {
        var sessionKey = $"{user.UserId}:{user.SessionId}";

        // Get or create session for this user+session combo
        var session = await GetOrCreateSessionAsync(sessionKey);

        var response = await _agent.RunAsync(message, session);
        return response.ToString();
    }

    private async Task<AgentSession> GetOrCreateSessionAsync(string key)
    {
        if (_sessions.TryGetValue(key, out var existing))
            return existing;

        var session = await _agent.CreateSessionAsync();
        _sessions[key] = session;
        return session;
    }

    /// <summary>
    /// Explicitly end a session — flushes history to SQL Server.
    /// Call this when the user ends a conversation.
    /// </summary>
    public async Task EndSessionAsync(string userId, string sessionId)
    {
        var key = $"{userId}:{sessionId}";
        if (_sessions.TryRemove(key, out var session))
        {
            await session.DisposeAsync(); // ← triggers SQL Server write
        }
    }

    public async ValueTask DisposeAsync()
    {
        // End all active sessions on shutdown — saves all history to SQL
        foreach (var (key, session) in _sessions)
        {
            await session.DisposeAsync();
        }
        _sessions.Clear();
    }
}
