using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 原文追溯服务接口
/// </summary>
public interface ISourceTracker
{
    /// <summary>
    /// 跟踪 chunk 与原文的映射关系
    /// </summary>
    Task TrackAsync(string documentId, string content, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// 根据 chunk ID 获取原文片段
    /// </summary>
    SourceSnippet? GetSource(string chunkId);

    /// <summary>
    /// 根据多个 chunk ID 批量获取原文片段
    /// </summary>
    List<SourceSnippet> GetSources(IEnumerable<string> chunkIds);

    /// <summary>
    /// 清空追踪数据
    /// </summary>
    void Clear();
}
