using AgentAppWebApi.Models;

namespace AgentAppWebApi.Services;


public interface IBookmarkService
{
    Task<Bookmark> AddAsync(string userId, BookmarkRequest req);
    Task<IEnumerable<Bookmark>> GetByUserAsync(string userId);
    Task<bool> DeleteAsync(string userId, Guid bookmarkId);
}
public class BookmarkService : IBookmarkService
{
    public Task<Bookmark> AddAsync(string userId, BookmarkRequest req)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(string userId, Guid bookmarkId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Bookmark>> GetByUserAsync(string userId)
    {
        throw new NotImplementedException();
    }
}
