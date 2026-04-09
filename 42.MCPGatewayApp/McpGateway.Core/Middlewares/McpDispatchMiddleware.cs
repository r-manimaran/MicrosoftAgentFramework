using McpGateway.Core.Client;
using McpGateway.Core.Interfaces;
using McpGateway.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace McpGateway.Core.Middlewares;

public class McpDispatchMiddleware : IMcpMiddleware
{
    private readonly IMcpServerClient _client;
    private readonly ILogger<McpDispatchMiddleware> _logger;

    public McpDispatchMiddleware(IMcpServerClient client,
        ILogger<McpDispatchMiddleware> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task InvokeAsync(McpGatewayContext context, 
                        Func<McpGatewayContext, Task> next)
    {
        // Guard: routing middleware must run before dispatch
        if (context.ResolvedServer is null)
        {
            _logger.LogError(
                "[DISPATCH] ResolvedServer is null for corr={CorrelationId} — ServerRoutingMiddleware must run first",
                context.Request.CorrelationId);

            context.Response = new McpToolCallResponse
            {
                CorrelationId = context.Request.CorrelationId,
                Success = false,
                ErrorCode = "MISCONFIGURED_PIPELINE",
                ErrorMessage = "Dispatch reached without a resolved server — check middleware order"
            };
            return;
        }
        //  Use sanitised arguments if PiiRedactionMiddleware has already run
        var arguments = context.Metadata.TryGetValue("sanitised_arguments", out var sanitised)
            ? sanitised as Dictionary<string, object?> ?? context.Request.Arguments
            : context.Request.Arguments;

        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "[DISPATCH] corr={CorrelationId} → {ServerId}/{ToolName}",
            context.Request.CorrelationId,
            context.ResolvedServer.ServerId,
            context.Request.ToolName);

        try
        {
            var result = await _client.CallToolAsync(
                context.ResolvedServer,
                context.Request.ToolName,
                arguments,
                CancellationToken.None);

            context.Response = new McpToolCallResponse
            {
                CorrelationId = context.Request.CorrelationId,
                Success = true,
                Result = result,
                Elapsed = sw.Elapsed
            };
            _logger.LogInformation(
                "[DISPATCH] corr={CorrelationId} ← success from {ServerId} in {Elapsed:N1}ms",
                context.Request.CorrelationId,
                context.ResolvedServer.ServerId,
                sw.Elapsed.TotalMilliseconds);

        }
        catch(McpServerException ex) when (ex.HttpStatusCode == 503)
        {
            context.Response = ErrorResponse(context, sw.Elapsed, "SERVICE_UNAVAILABLE", ex.Message);
        }
        catch (McpServerException ex)
        {
            context.Response = ErrorResponse(context, sw.Elapsed, "MCP_SERVER_ERROR", ex.Message);
        }
        catch (TimeoutException ex)
        {
            context.Response = ErrorResponse(context, sw.Elapsed, "TIMEOUT", ex.Message);
        }
        catch (HttpRequestException ex)
        {
            // Network-level failure (connection refused, DNS failure, etc.)
            _logger.LogWarning(ex,
                "[DISPATCH] Network error calling '{ServerId}'", context.ResolvedServer.ServerId);
            context.Response = ErrorResponse(context, sw.Elapsed, "NETWORK_ERROR",
                $"Could not reach '{context.ResolvedServer.ServerId}': {ex.Message}");
        }

        // Terminal - do not call next() since this is the end of the pipeline and we have a response ready to return
    }

    private static McpToolCallResponse ErrorResponse(
        McpGatewayContext ctx,
        TimeSpan elapsed,
        string code,
        string message) =>
        new()
        {
            CorrelationId = ctx.Request.CorrelationId,
            Success = false,
            ErrorCode = code,
            ErrorMessage = message,
            Elapsed = elapsed
        };
}
