using McpGateway.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace McpGateway.Core.Interfaces;

/// <summary>
/// Makes the actual Http Calls to the resolved MCP server.
/// 
/// </summary>
public interface IMcpServerClient
{
    Task<object?> CallToolAsync(McpServerRegistration server,
        string toolName,
        Dictionary<string, object?> arguments,
        CancellationToken ct);
}
