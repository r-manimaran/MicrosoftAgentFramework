using MdRag.Contracts.Ingestion;
using MdRag.Infrastructure.Data;
using MdRag.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Infrastructure.Repositories;

public interface IFileIndexRepository
{
    Task<FileIndexEntry?> GetByPathAsync(string filePath, CancellationToken ct = default);
    Task<FileIndexEntry?> GetByIdAsync(Guid fileId, CancellationToken ct = default);
    Task<IReadOnlyList<FileIndexEntry>> GetAllAsync(CancellationToken ct= default);
    Task<IReadOnlyList<FileIndexEntry>> GetByStatusAsync(IngestionStage status, CancellationToken ct = default);
    Task UpsertAsync(FileIndexEntry entry, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid fileId, IngestionStage status, string? errorMessage=null, CancellationToken ct = default);
    Task UpdateIndexedAsync(Guid fileId, int chunkCount, CancellationToken ct = default);


}

public sealed class FileIndexRepository : IFileIndexRepository
{
    private readonly RagDbContext _db;
    public FileIndexRepository(RagDbContext db) => _db = db;


    public Task<IReadOnlyList<FileIndexEntry>> GetAllAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<FileIndexEntry?> GetByIdAsync(Guid fileId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<FileIndexEntry?> GetByPathAsync(string filePath, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<FileIndexEntry>> GetByStatusAsync(IngestionStage status, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateIndexedAsync(Guid fileId, int chunkCount, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateStatusAsync(Guid fileId, IngestionStage status, string? errorMessage = null, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UpsertAsync(FileIndexEntry entry, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
