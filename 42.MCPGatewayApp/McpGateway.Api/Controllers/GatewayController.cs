using McpGateway.Core.Interfaces;
using McpGateway.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace McpGateway.Api.Controllers
{
    [Route("api/gateway")]
    [ApiController]
    public sealed class GatewayController : ControllerBase
    {
        private readonly IMcpGateway _gateway;
        private readonly ILogger<GatewayController> _logger;
        public GatewayController(IMcpGateway gateway, ILogger<GatewayController> logger)
        {
            _gateway = gateway;
            _logger = logger;
        }

        /// <summary>Execute a tool call through the MCP gateway.</summary>
        /// <remarks>
        /// The bearer token must contain an 'agent_id' claim matching the AgentId field.
        /// </remarks>
        [HttpPost("tools/call")]
        [ProducesResponseType(typeof(McpToolCallResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(429)]
        public async Task<IActionResult> CallTool(
            [FromBody] GatewayToolCallRequest req,
            CancellationToken ct)
        {
            var correlationId = req.CorrelationId ?? Guid.NewGuid().ToString("N");

            var mcpRequest = new McpToolCallRequest
            {
                CorrelationId = correlationId,
                AgentId = req.AgentId,
                ServerId = req.ServerId,
                ToolName = req.ToolName,
                Arguments = req.Arguments ?? []
            };

            var response = await _gateway.ExecuteAsync(mcpRequest, ct);

            return response.ErrorCode switch
            {
                "UNAUTHORIZED" => Unauthorized(response),
                "FORBIDDEN" => Forbid(),
                "RATE_LIMITED" => StatusCode(429, response),
                "CIRCUIT_OPEN" => StatusCode(503, response),
                null when response.Success => Ok(response),
                _ => BadRequest(response)
            };

        }


        /// <summary>List all registered MCP servers visible to the gateway.</summary>
        [HttpGet("servers")]
        [ProducesResponseType(typeof(IEnumerable<ServerSummary>), 200)]
        public IActionResult ListServers([FromServices] IMcpServerRegistry registry)
        {
            var summaries = registry.All().Select(s => new ServerSummary(
                s.ServerId, s.BaseUrl, s.Transport.ToString(),
                s.AllowedTools, s.TimeoutSeconds));
            return Ok(summaries);
        }
    }
}

public record GatewayToolCallRequest(
    string? CorrelationId,
    string AgentId,
    string ServerId,
    string ToolName,
    Dictionary<string, object?>? Arguments);

public record ServerSummary(
    string ServerId,
    string BaseUrl,
    string Transport,
    string[] AllowedTools,
    int TimeoutSeconds);
