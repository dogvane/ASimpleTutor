namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// LightRAG 简单服务接口（MiniLightRag 兼容）
/// </summary>
public interface ISimpleRagService
{
    /// <summary>
    /// 插入文档并分块
    /// </summary>
    /// <param name="documentId">文档ID</param>
    /// <param name="content">文档内容</param>
    /// <param name="metadata">元数据</param>
    /// <returns>Chunk 列表</returns>
    Task<List<ChunkResult>> InsertAsync(string documentId, string content, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// 向量检索
    /// </summary>
    /// <param name="query">查询文本</param>
    /// <param name="topK">返回结果数量</param>
    /// <returns>检索结果列表</returns>
    Task<List<SearchResult>> SearchAsync(string query, int topK = 5);

    /// <summary>
    /// 清空工作区
    /// </summary>
    Task ClearAsync();
}

/// <summary>
/// Chunk 结果
/// </summary>
public class ChunkResult
{
    public string ChunkId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }
}

/// <summary>
/// 检索结果
/// </summary>
public class SearchResult
{
    public string ChunkId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
