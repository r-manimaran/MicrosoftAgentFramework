using McpGateway.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace McpGateway.Core.Interfaces;

/// <summary>
/// Maps ServiceId strings to MCP Server Registration records.
/// Later we can swap to a database-backend or config-service-backed registry.
/// </summary>
public interface IMcpServerRegistry
{
    McpServerRegistration? Resolve(string serverId);
    IReadOnlyList<McpServerRegistration> All();
}
