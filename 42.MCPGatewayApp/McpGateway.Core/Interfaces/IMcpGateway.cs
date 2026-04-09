using McpGateway.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace McpGateway.Core.Interfaces;

/// <summary>
/// Public API consumed by agents and the ASP.NET controller.
/// Hides the pipeline internals entirely.
/// </summary>
public interface IMcpGateway
{
    Task<McpToolCallResponse> ExecuteAsync(McpToolCallRequest request,
                             CancellationToken ct);
}
