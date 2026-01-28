using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 原文追溯服务
/// 维护 chunk → 原始文档的映射关系
/// </summary>
public class SourceTracker : ISourceTracker
{
    private readonly Dictionary<string, SourceSnippet> _chunkToSnippet = new();
    private readonly ILogger<SourceTracker> _logger;

    public SourceTracker(ILogger<SourceTracker> logger)
    {
        _logger = logger;
    }

    public Task TrackAsync(string documentId, string content, Dictionary<string, object>? metadata = null)
    {
        // 简单实现：整个文档作为一个 chunk
        var chunkId = $"{documentId}_chunk_0";

        var snippet = new SourceSnippet
        {
            SnippetId = chunkId,
            DocId = documentId,
            ChunkId = chunkId,
            Content = content.Length > 500 ? content.Substring(0, 500) + "..." : content,
            FilePath = metadata?["filePath"]?.ToString() ?? string.Empty,
            HeadingPath = new List<string>(),
            StartLine = 0,
            EndLine = content.Split('\n').Length - 1
        };

        if (metadata != null)
        {
            if (metadata.TryGetValue("headingPath", out var headingPathObj) && headingPathObj is List<string> headingPath)
            {
                snippet.HeadingPath = headingPath;
            }
            if (metadata.TryGetValue("filePath", out var filePathObj))
            {
                snippet.FilePath = filePathObj.ToString() ?? string.Empty;
            }
        }

        _chunkToSnippet[chunkId] = snippet;
        _logger.LogDebug("跟踪 chunk: {ChunkId}", chunkId);

        return Task.CompletedTask;
    }

    public SourceSnippet? GetSource(string chunkId)
    {
        return _chunkToSnippet.TryGetValue(chunkId, out var snippet) ? snippet : null;
    }

    public List<SourceSnippet> GetSources(IEnumerable<string> chunkIds)
    {
        return chunkIds
            .Select(id => _chunkToSnippet.TryGetValue(id, out var snippet) ? snippet : null)
            .Where(s => s != null)
            .ToList()!;
    }

    public void Clear()
    {
        _chunkToSnippet.Clear();
        _logger.LogInformation("已清空原文追溯数据");
    }
}
