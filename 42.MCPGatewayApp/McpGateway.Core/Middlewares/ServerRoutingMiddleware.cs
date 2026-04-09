using McpGateway.Core.Interfaces;
using McpGateway.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace McpGateway.Core.Middlewares;

public class ServerRoutingMiddleware : IMcpMiddleware
{
    private readonly IMcpServerRegistry _registry;
    private readonly ILogger<ServerRoutingMiddleware> _logger;

    public ServerRoutingMiddleware(IMcpServerRegistry registry,
        ILogger<ServerRoutingMiddleware> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public async Task InvokeAsync(McpGatewayContext context, Func<McpGatewayContext, Task> next)
    {
        var serverId = context.Request.ServerId;
        var server = _registry.Resolve(serverId);

        if(server is null)
        {
            var registered = string.Join(", ", _registry.All().Select(s => s.ServerId));
            _logger.LogWarning(
                "[ROUTING] Unknown ServerId='{ServerId}' from AgentId='{AgentId}'. Registered: [{Registered}]",
                serverId, context.Request.AgentId, registered);

            context.Response = new McpToolCallResponse
            {
                CorrelationId = context.Request.CorrelationId,
                Success = false,
                ErrorCode = "SERVER_NOT_FOUND",
                ErrorMessage = $"No MCP server registered with ID '{serverId}'. " +
                               $"Registered servers: [{registered}]"
            };
            return;  // pipeline stops - next is NOT called
        }

        context.ResolvedServer = server;

        _logger.LogInformation(
            "[ROUTING] corr={CorrelationId} → ServerId='{ServerId}' resolved to {BaseUrl} ({Transport})",
            context.Request.CorrelationId, serverId, server.BaseUrl, server.Transport);

        await next(context); // continue to next middleware
    }
}
