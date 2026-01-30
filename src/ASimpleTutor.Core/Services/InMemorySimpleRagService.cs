using ASimpleTutor.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 简单的 RAG 服务内存实现
/// </summary>
public class InMemorySimpleRagService : ISimpleRagService
{
    private readonly Dictionary<string, (string DocumentId, string Content, Dictionary<string, object> Metadata)> _chunks = new();
    private readonly Dictionary<string, List<string>> _docChunks = new();
    private readonly ILogger<InMemorySimpleRagService> _logger;
    private readonly ISourceTracker _sourceTracker;

    public InMemorySimpleRagService(ISourceTracker sourceTracker, ILogger<InMemorySimpleRagService> logger)
    {
        _sourceTracker = sourceTracker;
        _logger = logger;
    }

    public Task<List<ChunkResult>> InsertAsync(string documentId, string content, Dictionary<string, object>? metadata = null)
    {
        _logger.LogInformation("插入文档: {DocumentId}", documentId);

        var chunks = SplitIntoChunks(content, 1000); // 简单分块
        var results = new List<ChunkResult>();

        var chunkIndex = 0;
        foreach (var chunk in chunks)
        {
            var chunkId = $"{documentId}_chunk_{chunkIndex}";
            var enrichedMetadata = new Dictionary<string, object>(metadata ?? new Dictionary<string, object>());
            enrichedMetadata["chunkId"] = chunkId;
            enrichedMetadata["chunkIndex"] = chunkIndex;

            _chunks[chunkId] = (documentId, chunk, enrichedMetadata);

            // 跟踪原文位置
            _sourceTracker.TrackAsync(documentId, chunk, enrichedMetadata);

            results.Add(new ChunkResult
            {
                ChunkId = chunkId,
                Content = chunk,
                TokenCount = EstimateTokens(chunk)
            });

            chunkIndex++;
        }

        if (!_docChunks.TryGetValue(documentId, out _))
        {
            _docChunks[documentId] = new List<string>();
        }

        _logger.LogInformation("文档分块完成，共 {Count} 个 chunk", results.Count);
        return Task.FromResult(results);
    }

    public Task<List<SearchResult>> SearchAsync(string query, int topK = 5)
    {
        // 简单实现：基于关键词匹配
        _logger.LogDebug("搜索查询: {Query}, topK: {TopK}", query, topK);

        var queryWords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<(string ChunkId, double Score)>();

        foreach (var (chunkId, (docId, content, metadata)) in _chunks)
        {
            var contentLower = content.ToLower();
            var score = 0.0;

            foreach (var word in queryWords)
            {
                if (contentLower.Contains(word))
                {
                    score += 1.0;
                }
            }

            // 标题匹配权重更高
            if (metadata.TryGetValue("headingPath", out var headingPathObj) && headingPathObj is List<string> headingPath)
            {
                var headingText = string.Join(" ", headingPath).ToLower();
                foreach (var word in queryWords)
                {
                    if (headingText.Contains(word))
                    {
                        score += 2.0;
                    }
                }
            }

            if (score > 0)
            {
                results.Add((chunkId, score));
            }
        }

        // 按分数排序并取 topK
        var topResults = results
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .Select(r =>
            {
                var (_, content, metadata) = _chunks[r.ChunkId];
                return new SearchResult
                {
                    ChunkId = r.ChunkId,
                    Content = content,
                    Score = r.Score,
                    Metadata = metadata
                };
            })
            .ToList();

        _logger.LogDebug("搜索结果: {Count} 条", topResults.Count);
        return Task.FromResult(topResults);
    }

    public Task ClearAsync()
    {
        _chunks.Clear();
        _docChunks.Clear();
        _sourceTracker.Clear();
        _logger.LogInformation("已清空 RAG 数据");
        return Task.CompletedTask;
    }

    private static List<string> SplitIntoChunks(string content, int maxChunkSize)
    {
        var chunks = new List<string>();
        var paragraphs = content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new System.Text.StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length > maxChunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString());
                currentChunk.Clear();
            }

            if (currentChunk.Length > 0)
            {
                currentChunk.Append("\n\n");
            }
            currentChunk.Append(paragraph);
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString());
        }

        return chunks;
    }

    private static int EstimateTokens(string text)
    {
        // 粗略估计：英文约 4 字符/token，中文约 1 字符/token
        return text.Length / 4;
    }
}
