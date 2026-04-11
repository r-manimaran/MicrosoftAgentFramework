using System;
using System.Collections.Generic;
using System.Text;

namespace McpAgentApp.Models;

public record GatewayToolCallRequest(string AgentId, string ServerId, string ToolName,
    Dictionary<string, object?> Arguments,
    string? correlationId = null);
