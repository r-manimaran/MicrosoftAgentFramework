using AgentAppWebApi.Models;
using AgentAppWebApi.Services;

namespace AgentAppWebApi.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/chat").RequireAuthorization();
        // POST /chat/message — send a message, get a reply
        group.MapPost("/message", async (
            ChatRequest req,
            HttpContext ctx,
            IAgentService agentService) =>
        {
        })
        .WithName("SendMessage")
        .WithSummary("Send a message to the Tamil literature assistant")
        .Produces<ChatResponse>()
        .Produces(401);

        // POST /chat/end — end session (flushes history to SQL Server)
        group.MapPost("/end", async (
            string sessionId,
            HttpContext ctx,
            AgentService agentService) =>
        {


        }).WithName("EndSession")
        .WithSummary("End a session and persist history to SQL Server");

        return app;
    }
}
