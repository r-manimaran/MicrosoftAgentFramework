using McpGateway.Core.Interfaces;
using McpGateway.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpGateway.Core.Client;

public class McpHttpServerClient : IMcpServerClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<McpHttpServerClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public McpHttpServerClient(IHttpClientFactory httpClientFactory,
                        ILogger<McpHttpServerClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    public async Task<object?> CallToolAsync(McpServerRegistration server, string toolName, Dictionary<string, object?> arguments, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        // Named client configured per server
        using var http = _httpClientFactory.CreateClient($"mcp-{server.ServerId}");
        var requestBody = new McpToolCallHttpRequest
        {
            Name = toolName,
            Arguments = arguments
        };

        _logger.LogDebug(
            "[CLIENT] -> POST {BaseUrl}/tools/call tool={Tool} args={ArgCount}",
            server.BaseUrl, toolName, arguments.Count);

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await http.PostAsync("tools/call",
                new StringContent(JsonSerializer.Serialize(requestBody, JsonOpts), Encoding.UTF8, "application/json"), ct);
        }
        catch(TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new McpServerException(server.ServerId, 0,
                $"Request to server {server.ServerId} timed out after {sw.ElapsedMilliseconds}ms");            
        }
        sw.Stop();
        _logger.LogDebug(
             "[CLIENT] ← HTTP {Status} from '{ServerId}' in {Elapsed:N1}ms",
             (int)httpResponse.StatusCode, server.ServerId, sw.Elapsed.TotalMilliseconds);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync(ct);
            throw new McpServerException(server.ServerId, (int)httpResponse.StatusCode, errorBody);
        }

        var mcpResponse = await httpResponse.Content
            .ReadFromJsonAsync<McpToolCallHttpResponse>(JsonOpts, ct)
            ?? throw new McpServerException(server.ServerId, 200, "Server returned an empty body");

        if (mcpResponse.IsError)
        {
            var errText = mcpResponse.Content?.FirstOrDefault()?.Text ?? "MCP server reported an error";
            _logger.LogWarning(
                "[CLIENT] MCP server '{ServerId}' returned isError=true: {Error}",
                server.ServerId, errText);
            throw new McpServerException(server.ServerId, 200, errText);
        }

        // Single text block → return the string directly so the agent LLM sees clean text
        if (mcpResponse.Content?.Count == 1 && mcpResponse.Content[0].Type == "text")
            return mcpResponse.Content[0].Text;

        // Multiple content blocks → return the array
        return mcpResponse.Content;
    }
}

public sealed class McpToolCallHttpRequest
{
    public required string Name { get; set; }
    public Dictionary<string, object?> Arguments { get; set; } = new();
}

public sealed class McpToolCallHttpResponse
{
    public List<McpContentBlock>? Content { get; set; }
    public bool IsError { get; set; }
}

public sealed class McpContentBlock
{
    public string Type { get; set; } = "text";
    public string? Text { get; set; }
}

public sealed class McpServerException : Exception
{
    public string ServerId { get; }
    public int HttpStatusCode { get; }
    public McpServerException(string serverId, int statusCode, string message) : base($"[{serverId}] HTTP {statusCode}: {message}")
    {
        ServerId = serverId;
        HttpStatusCode = statusCode;
    }
}
