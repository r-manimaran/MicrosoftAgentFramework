using System;
using System.Collections.Generic;
using System.Text;

namespace McpAgentApp.Models;

public record GatewayToolCallResponse(string CorrelationId,
    bool Success,
    object? Result,
    string? ErrorCode,
    string? ErrorMessage,
    TimeSpan Elapsed);

