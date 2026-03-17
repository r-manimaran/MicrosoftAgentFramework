namespace AgenticRAGWebApi.Models;

public record HybridSearchResult(string Title,
    string Content,
    float Score,
    string Source);
