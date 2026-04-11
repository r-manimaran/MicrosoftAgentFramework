using McpAgentApp.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpAgentApp.Gateway;

public class GatewayMcpClient : IAsyncDisposable
{
    private readonly HttpClient _http;
    private readonly string _agentId;
    private readonly string _serverId;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    public GatewayMcpClient(string gatewayBaseUrl, string agentId, string serverId)
    {
        _agentId = agentId;
        _serverId = serverId;
        _http = new HttpClient 
        { 
            BaseAddress = new Uri(gatewayBaseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        };
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<IList<GatewayTool>> ListToolsAsync(CancellationToken ct = default)
    {
        // Ask the gateway which tools this server exposes
        // The gateway reads from appsettings McpServers[].AllowedTools
        // In M3 AllowedTools is empty (open access), so we use a discovery call
        var resp = await _http.GetAsync("/api/gateway/servers", ct);
        resp.EnsureSuccessStatusCode();

        var servers = await resp.Content
            .ReadFromJsonAsync<List<GatewayServerInfo>>(JsonOpts, ct) ?? [];

        var server = servers.FirstOrDefault(s =>
            string.Equals(s.ServerId, _serverId, StringComparison.OrdinalIgnoreCase));

        if (server is null)
            throw new InvalidOperationException(
                $"Server '{_serverId}' is not registered in the gateway. " +
                $"Registered: [{string.Join(", ", servers.Select(s => s.ServerId))}]");

        // If AllowedTools is empty (M3 open-access), probe the MCP server for its tool list.
        // The gateway proxies this call transparently.
        var toolNames = server.AllowedTools.Length > 0
            ? server.AllowedTools
            : await ProbeToolNamesAsync(ct);

        // Build GatewayTool objects — shape mirrors McpClientTool
        return toolNames.Select(name => new GatewayTool(
            this, _serverId, name,
            description: $"Tool '{name}' on server '{_serverId}'",
            inputSchema: default)).ToList();
    }

    public async Task<string> CallToolAsync(string serverId, 
                                string toolName, 
                                Dictionary<string, object?> arguments, 
                                CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        
        var normalizedArgs = NormalizeArguments(arguments);

        var body = new GatewayToolCallRequest(
            AgentId: _agentId,
            ServerId: serverId,
            ToolName: toolName,
            Arguments: normalizedArgs,
            correlationId: correlationId);

        var httpResp = await _http.PostAsJsonAsync(
            "/api/gateway/tools/call", body, JsonOpts, ct);

        var gatewayResp = await httpResp.Content
            .ReadFromJsonAsync<GatewayToolCallResponse>(JsonOpts, ct)
            ?? throw new InvalidOperationException("Empty response from gateway");

        if (!gatewayResp.Success)
        {
            throw new GatewayToolException(
                toolName, gatewayResp.ErrorCode, gatewayResp.ErrorMessage);
        }

        // Return the result as a string — same as MCP wire protocol text content
        return gatewayResp.Result is string s
            ? s
            : JsonSerializer.Serialize(gatewayResp.Result, JsonOpts);
    }

    private async Task<string[]> ProbeToolNamesAsync(CancellationToken ct)
    {
        // Use a dedicated probe call through the gateway
        // The gateway will route this to the MCP server's tool list
        try
        {
            var resp = await CallToolAsync(
                _serverId, "__list_tools__", new Dictionary<string, object?>(), ct);
            // If the MCP server doesn't support __list_tools__, fall back to known tools
        }
        catch { /* swallow — fall back to hardcoded known tools below */ }

        // Fallback: return the known tools for the IT Support MCP server
        // In production, replace with a proper tool discovery mechanism
        return ["get_open_incidents", "get_ticket_detail", "update_ticket_status"];
    }

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await Task.CompletedTask;
    }

    private static Dictionary<string, object?> NormalizeArguments(
    Dictionary<string, object?> args)
    {
        var result = new Dictionary<string, object?>(args.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in args)
        {
            result[key] = value switch
            {
                System.Text.Json.JsonElement je => UnwrapJsonElement(je),
                _ => value
            };
        }

        return result;
    }

    private static object? UnwrapJsonElement(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => element.GetString(),
            System.Text.Json.JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null,
            System.Text.Json.JsonValueKind.Array => element.EnumerateArray()
                                                              .Select(UnwrapJsonElement)
                                                              .ToList(),
            System.Text.Json.JsonValueKind.Object => element.EnumerateObject()
                                                              .ToDictionary(p => p.Name,
                                                                            p => UnwrapJsonElement(p.Value)),
            _ => element.GetRawText()
        };
    }
}

public class GatewayToolException : Exception
{
    public string ToolName { get; }
    public string? ErrorCode { get; }

    public GatewayToolException(string toolName, string? errorCode, string? message)
        : base($"Tool '{toolName}' failed [{errorCode}]: {message}")
    {
        ToolName = toolName;
        ErrorCode = errorCode;
    }
}
