using MdRag.Ingestion.Models;
using Microsoft.Extensions.Options;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ML.Tokenizers;

namespace MdRag.Ingestion.Services;

/// <summary>
/// Splits ParsedDocument sections into token-bounded ChunkRecords.
/// 
/// Algorithm:
///   For each DocumentSection:
///     a) If section fits within MaxTokensPerChunk → emit as a single chunk.
///     b) Otherwise, split the section text by sentences/paragraphs into
///        sub-chunks of MaxTokensPerChunk tokens with ChunkOverlapTokens overlap.
///
/// Overlap is implemented by carrying the last N tokens of the previous
/// sub-chunk forward into the next one, preserving context at boundaries.
///
/// Chunk IDs are deterministic:
///   ChunkId = GuidV5(FileId + ChunkIndex)
/// so re-ingesting the same file always produces the same IDs — Qdrant
/// upsert is idempotent and old vectors are overwritten, not duplicated.
/// 
/// </summary>
public sealed class ChunkingService
{
    private static readonly Tokenizer Tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");

    private readonly IngestionOptions _options;
    private readonly ILogger<ChunkingService> _logger;

    public ChunkingService(IOptions<IngestionOptions> options,
        ILogger<ChunkingService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IReadOnlyList<ChunkRecord> Chunk(ParsedDocument doc)
    {
        var chunks = new List<ChunkRecord>();
        int chunkIndex = 0;

        foreach(var section in doc.Sections)
        {
            if(section.TokenCount <= _options.MaxTokensPerChunk)
            {
                // Section fits in a single chunk.
                chunks.Add(MakeChunk(doc, section.Content,section.HeadingBreadcrumb, chunkIndex++));
            }
            else
            {
                // section must be split.
                var subChunks = SplitSection(
                    section.Content,section.HeadingBreadcrumb, doc, ref chunkIndex);
                chunks.AddRange(subChunks);
            }
        }

        _logger.LogDebug(
           "Chunked {File}: {Sections} sections → {Chunks} chunks",
           doc.FilePath, doc.Sections.Count, chunks.Count);

        return chunks;
    }
    //-------------------------
    // Section splitting
    //------------------------

    private IReadOnlyList<ChunkRecord> SplitSection(string content,
        string breadcrumb,
        ParsedDocument doc,
        ref int chunkIndex)
    {
        var result = new List<ChunkRecord>();

        // Split by paragraph (double newline) as the natural unit.
        // If a single paragraph exceeds MaxTokens it is further split by sentence.
        var paragraphs = content.Split( new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        var buffer = new List<string>();
        int bufferTokens = 0;


        foreach (var para in paragraphs)
        {
            var paraTokens = Tokenizer.CountTokens(para);

            if(bufferTokens + paraTokens > _options.MaxTokensPerChunk && buffer.Count > 0)
            {
                // Flush current buffer as a chunk
                result.Add(MakeChunk(doc, string.Join("\n\n", buffer), breadcrumb, chunkIndex++));

                // Keep overlap: retain last N tokens worth of content
                RetainOverlap(buffer, ref bufferTokens);
            }

            if(paraTokens > _options.MaxTokensPerChunk)
            {
                // Paragraph is too long - split by sentence
                var sentenceChunks = SplitBySentence(para, breadcrumb, doc, ref chunkIndex);
                result.AddRange(sentenceChunks);
            }
            else
            {
                buffer.Add(para);
                bufferTokens += paraTokens;
            }
        }

        // Flush remaining buffer
        if (buffer.Count > 0)
            result.Add(MakeChunk(doc, string.Join("\n\n", buffer), breadcrumb, chunkIndex++));
        return result;
    }

    private IReadOnlyList<ChunkRecord> SplitBySentence(string text, string breadcrumb, ParsedDocument doc, ref int chunkIndex)
    {
        var result = new List<ChunkRecord>();
        // Simple sentence split on '.',',! ','? '
        var sentences = text.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries);

        var buffer = new List<string>();
        int bufferTokens = 0;
        
        foreach(var sentence in sentences)
        {
            var sentTokens = Tokenizer.CountTokens(sentence);

            if(bufferTokens + sentTokens > _options.MaxTokensPerChunk && 
                buffer.Count > 0)
            {
                result.Add(MakeChunk(doc, string.Join(" ", buffer), breadcrumb, chunkIndex++));

                RetainOverlap(buffer, ref bufferTokens);
            }

            buffer.Add(sentence);
            bufferTokens += sentTokens;
        }

        if(buffer.Count > 0)
        {
            result.Add(MakeChunk(doc, string.Join(" ", buffer), breadcrumb, chunkIndex++));
        }

        return result;
        
    }

    //-----------------
    // Overlap helper - remove items from the front of the buffer until
    // remaining token count is within ChunkoverlapTokens
    // -----------------

    private void RetainOverlap(List<string> buffer, ref int bufferTokens)
    {
        while(bufferTokens > _options.ChunkOverlapTokens &&  buffer.Count > 1)
        {
            bufferTokens -= Tokenizer.CountTokens(buffer[0]);
            buffer.RemoveAt(0);
        }
    }

    //------------------------
    // Chunk record factory
    // ------------------------
    private static ChunkRecord MakeChunk(ParsedDocument doc, string content, string breadcrumb, int index)
    {
        return new ChunkRecord
        {
            ChunkId = DeterministicGuid(doc.FileId, index),
            ChunkIndex = index,
            FileId = doc.FileId,
            FilePath = doc.FilePath,
            HeadingBreadcrumb = breadcrumb,
            LastModifiedUtc = doc.LastModifiedUtc,
            Content = content.Trim(),
            TokenCount = Tokenizer.CountTokens(content),
        };
    }

    private static Guid DeterministicGuid(Guid namespaceId, int index)
    {
        var nameBytes = Encoding.UTF8.GetBytes($"{namespaceId}:{index}");
        var hash = System.Security.Cryptography.SHA1.HashData(nameBytes);

        // Fit the first 16 bytes of the SHA-1 hash into a Guid
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50); // version 5
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // variant RFC 4122
        return new Guid(hash[..16]);
    }
}
