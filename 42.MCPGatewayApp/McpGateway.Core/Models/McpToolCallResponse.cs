namespace McpGateway.Core.Models;

/// <summary>
/// Response returned from the MCP server after invoking the tool.
/// This reponse will be returned to the agent
/// </summary>
public record McpToolCallResponse
{
    public required string CorrelationId { get; init; }
    public required bool Success { get; init; }
    /// <summary>
    /// The tool result - a JSON string, object or array
    /// </summary>
    public object? Result { get; init; }
    /// <summary>
    /// Machine readable error code (UNAUTHORIZED, FORBIDDEN, SERVER_NOT_FOUND, INTERNAL_ERROR, etc).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Human readable error message, useful for debugging and display in agent UI. Should be concise and avoid technical jargon.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public TimeSpan Elapsed { get; init; }

}
