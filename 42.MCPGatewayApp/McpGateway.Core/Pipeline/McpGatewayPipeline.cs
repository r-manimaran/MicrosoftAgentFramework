using McpGateway.Core.Interfaces;
using McpGateway.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace McpGateway.Core.Pipeline;

/// <summary>
/// Builds and executes the ordered middleware chain for every tool call.
///
/// How the chain is built:
///   Given middlewares [A, B, C, Dispatch]:
///     pipeline = Dispatch
///     pipeline = C wrapping pipeline       → C → Dispatch
///     pipeline = B wrapping pipeline       → B → C → Dispatch
///     pipeline = A wrapping pipeline       → A → B → C → Dispatch
///
///   A runs first on the way IN and last on the way OUT.
///   This matches ASP.NET middleware ordering exactly.
///
/// Milestone 3 pipeline (only 2 active middleware):
///   [0] ServerRoutingMiddleware   ← resolves ServerId → McpServerRegistration
///   [1] McpDispatchMiddleware     ← HTTP POST to the resolved MCP server (terminal)
///
/// Later milestones insert middleware at specific positions without changing
/// any existing code — the pipeline engine is stable across all milestones.
/// </summary>
public sealed class McpGatewayPipeline : IMcpGateway
{
    private readonly IReadOnlyList<IMcpMiddleware> _middlewares;
    private readonly ILogger<McpGatewayPipeline> _logger;
    public McpGatewayPipeline(IEnumerable<IMcpMiddleware> middlewares,
        ILogger<McpGatewayPipeline> logger)
    {
        _middlewares = middlewares.ToList();
        _logger = logger;

        _logger.LogDebug("[PIPELINE] Built with {Count} middleware(s): {Names}",
            _middlewares.Count,
            string.Join(" → ", _middlewares.Select(m => m.GetType().Name)));
    }
    public async Task<McpToolCallResponse> ExecuteAsync(McpToolCallRequest request, CancellationToken ct)
    {
        var context = new McpGatewayContext { Request = request };

        // Build the chain from the tail backwards
        Func<McpGatewayContext, Task> pipeline = TerminalAsync;

        foreach (var middleware in _middlewares.Reverse())
        {
            var current = middleware;
            var next = pipeline;
            pipeline = ctx => current.InvokeAsync(ctx, next);
        }

        try
        {
            await pipeline(context);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex,
               "[PIPELINE] Unhandled exception for corr={CorrelationId} agent={AgentId} server={ServerId} tool={ToolName}",
               request.CorrelationId, request.AgentId, request.ServerId, request.ToolName);

            context.Response = new McpToolCallResponse
            {
                CorrelationId = request.CorrelationId,
                Success = false,
                ErrorCode = "PIPELINE_EXCEPTION",
                ErrorMessage = $"An unhandled exception occurred during pipeline execution.{ex.Message}"
            };
        }

        if (context.Response is not null)
            return context.Response;

        // Should only happen if the pipeline has no terminal middleware
        _logger.LogError("[PIPELINE] No response produced for corr={CorrelationId}", request.CorrelationId);
        return new McpToolCallResponse
        {
            CorrelationId = request.CorrelationId,
            Success = false,
            ErrorCode = "NO_RESPONSE",
            ErrorMessage = "Pipeline completed without producing a response — check middleware configuration"
        };
    }

    private static Task TerminalAsync(McpGatewayContext _) => Task.CompletedTask;
}
