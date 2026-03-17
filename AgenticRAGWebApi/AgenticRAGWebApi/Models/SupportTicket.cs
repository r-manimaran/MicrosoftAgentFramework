namespace AgenticRAGWebApi.Models;

public record SupportTicket(int TicketId,
    string UserId,
    string Description,
    DateTime CreatedAt,
    string Status);


