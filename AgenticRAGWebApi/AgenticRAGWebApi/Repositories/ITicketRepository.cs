using AgenticRAGWebApi.Models;

namespace AgenticRAGWebApi.Repositories;

public interface ITicketRepository
{
    // Core CRUD
    Task<SupportTicket> CreateAsync(SupportTicket ticket,
        string priority="Medium",
        string? category=null,
        string? runbookRef=null,
        CancellationToken ct = default(CancellationToken));

    Task<SupportTicket> GetByIdAsync(int ticketId, CancellationToken ct = default);

    Task<SupportTicket> UpdateStatusAsync(int ticketId, string status, string? resolutionNotes=null, 
        CancellationToken ct= default(CancellationToken));

    // Agent tool queries
    Task<IReadOnlyList<SupportTicket>> GetRecentAsync(string userId, int count = 5, CancellationToken ct = default);

    Task<IReadOnlyList<SupportTicket>> GetByStatusAsync(string status, int pageSize = 20, CancellationToken ct = default);

    Task<IReadOnlyList<SupportTicket>> SearchByKeywordAsync(string keyword, int topK=10, CancellationToken ct = default);


    // Analytics used by reflection / reporting
    Task<int> CountOpenByUserAsync(string userId, CancellationToken ct = default);

    Task<bool> HasRecurringIssueAsync(string userId, string category, int withinDays =30,
                                      int threshold = 2, CancellationToken ct = default(CancellationToken));


}
