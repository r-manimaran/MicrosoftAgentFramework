using Azure.Storage.Blobs;
using System.Text.Json;

namespace SupportingAgentConsoleApp.Middleware;

public class PromptLogger
{
    private readonly BlobContainerClient _container;
    public PromptLogger(string blobConnectionString)
    {
        var blobService = new BlobServiceClient(blobConnectionString);
        _container = blobService.GetBlobContainerClient("agent-prompts-logs");
        _container.CreateIfNotExists();
    }

    public async Task LogAsync(PromptLogEntry entry)
    {
        // Partition by date for cheap lifecycle tiering
        var blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{entry.TicketId}-{entry.RunId}.json";
        var blob = _container.GetBlobClient(blobName);
        var json = JsonSerializer.Serialize(entry);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        await blob.UploadAsync(stream, overwrite: true);
    }
}
public record PromptLogEntry(
    string RunId,
    string TicketId,
    string UserId,
    string MaskedPrompt,
    string MaskedResponse,
    long InputTokens,
    long OutputTokens,
    double LatencyMs,
    string ModelId,
    DateTime TimestampUtc
);