using AgentAppWebApi.Models;
using AgentAppWebApi.Services;
using Microsoft.OpenApi;

namespace AgentAppWebApi.Endpoints;

public static class BookmarkEndpoints
{
    public static IEndpointRouteBuilder MapBookmarkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bookmarks").RequireAuthorization();

        group.MapGet("/", async (HttpContext ctx, IBookmarkService bookmarkService) =>
        {
            var user = UserContext.FromHttpContext(ctx);
            var bookmarks = await bookmarkService.GetByUserAsync(user.UserId);
            return Results.Ok(bookmarks);
        }).WithName("GetBookmarks")
        .WithSummary("Get all bookmarks for the current user")
        .Produces<IEnumerable<Bookmark>>();

        group.MapPost("/", async (BookmarkRequest req, HttpContext ctx, IBookmarkService bookmarkService) =>
        {
            var user = UserContext.FromHttpContext(ctx);
            var bookmark = await bookmarkService.AddAsync(user.UserId, req);
            return Results.Created($"/bookmarks/{bookmark.Id}", bookmark);

        }).WithName("AddBookmark")
        .WithSummary("Bookmark a Q&A exchange for later reference")
        .Produces<Bookmark>(201);
        return app;
    }
}
