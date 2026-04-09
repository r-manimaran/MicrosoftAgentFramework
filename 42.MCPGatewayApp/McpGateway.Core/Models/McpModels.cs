using System;
using System.Collections.Generic;
using System.Text;

namespace McpGateway.Core.Models;

public record McpServerRegistration
{
    // <summary>Logical ID used by agents to address this server (e.g. "it-support-mcp").</summary>
    public required string ServerId { get; init; }
    // <summary> Base Url of the MCP server (e.g. "http://localhost:5100").</summary>
    public required string BaseUrl { get; init; }

    public required McpTransport Transport { get; init; }
    //<summary>Agents Allowed to call this server. Empty = open to all (dev mode).</summary>
    public string[] AllowedAgents { get; init; } = [];
    //<summary>Tools agents may invoke. Empty = all tools allowed (dev mode).</summary>
    public string[] AllowedTools { get; init; } = [];
    public int TimeoutSeconds { get; init; } = 30;

}
public enum McpTransport
{
    Stdio,
    Sse,
    StreamableHttp
}

/// <summary>
/// Carries the request, the resolved server, and any metadata accumulated
/// by middleware. Each middleware reads/writes this as it passes through the chain.
/// </summary>
public class McpGatewayContext
{
    public required McpToolCallRequest Request { get; init; }

    /// <summary>Set by ServerRoutingMiddleware once the target server is resolved.</summary>
    public McpServerRegistration? ResolvedServer { get; set; }

    /// <summary>Set by the terminal McpDispatchMiddleware (or any middleware that short-circuits).</summary>
    public McpToolCallResponse? Response { get; set; }
    /// <summary>
    /// Shared metadata bag for cross-middleware communication.
    /// e.g. PiiRedactionMiddleware writes "sanitised_arguments" here for Dispatch to read.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();
}