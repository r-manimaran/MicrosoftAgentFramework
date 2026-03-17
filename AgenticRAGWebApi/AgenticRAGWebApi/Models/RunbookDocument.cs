namespace AgenticRAGWebApi.Models;

public record RunbookDocument(Guid Id,
    string Title,
    string Content,
    string Category,
    string Source);
