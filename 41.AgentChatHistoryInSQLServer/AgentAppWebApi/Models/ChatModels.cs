namespace AgentAppWebApi.Models;

public record ChatRequest(
    string Message,
    string? SessionId = null   // null = start a new session
);

public record ChatResponse(
    string Reply,
    string SessionId,
    string UserId,
    DateTime Timestamp
);

public record BookmarkRequest(
    string SessionId,
    string UserMessage,
    string AgentReply,
    string? Note = null
);

public record Bookmark(
    Guid Id,
    string UserId,
    string UserMessage,
    string AgentReply,
    string? Note,
    DateTime CreatedAt
);

public record UserContext(
    string UserId,       // sub claim (stable unique ID)
    string Username,     // preferred_username
    string Email,
    string SessionId
)
{
    /// <summary>Build from HttpContext — throws if unauthenticated.</summary>
    public static UserContext FromHttpContext(HttpContext ctx, string? sessionId = null)
    {
        var user = ctx.User;

        var userId = user.FindFirst("sub")?.Value
                  ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? throw new UnauthorizedAccessException("Missing 'sub' claim");

        var username = user.FindFirst("preferred_username")?.Value
                    ?? user.Identity?.Name
                    ?? userId;

        var email = user.FindFirst("email")?.Value
                 ?? user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                 ?? "";

        return new UserContext(
            UserId: userId,
            Username: username,
            Email: email,
            SessionId: sessionId ?? Guid.NewGuid().ToString()
        );
    }
}