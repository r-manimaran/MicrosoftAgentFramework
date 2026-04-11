using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace McpAgentApp.Gateway;

public class GatewayTool
{
    private readonly GatewayMcpClient _client;
    private readonly string _serverId;

    public string Name { get; }
    public string Description { get; }
    public JsonElement InputSchema { get; }
    internal GatewayTool(GatewayMcpClient client,
        string serverId,
        string name,
        string description,
        JsonElement inputSchema)
    {
        _client = client;
        _serverId = serverId;
        Name = name;
        Description = description;
        InputSchema = inputSchema;
    }

    /// <summary>
    /// Execute this tool through the gateway.
    /// Called by the MAF agent loop when the LLM selects this tool.
    /// </summary>
    public async Task<string> InvokeAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken ct = default)
    {
        var args = arguments is not null
        ? new Dictionary<string, object?>(arguments)
        : new Dictionary<string, object?>();

        return await _client.CallToolAsync(_serverId, Name, args, ct);
    }

    /// <summary>
    /// Returns an AIFunction compatible with Microsoft.Extensions.AI ChatOptions.Tools.
    /// Identical to how McpClientTool.AsAIFunction() is used in M2.
    /// </summary>
    public Microsoft.Extensions.AI.AIFunction AsAIFunction()
    {
        return Microsoft.Extensions.AI.AIFunctionFactory.Create(
            async (IReadOnlyDictionary<string, object?> args, CancellationToken ct) =>
                await InvokeAsync(args, ct),
            Name,
            Description);
    }
}
