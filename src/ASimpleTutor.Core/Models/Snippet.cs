namespace ASimpleTutor.Core.Models;

/// <summary>
/// 原文片段（用于原文对照）
/// </summary>
public class SourceSnippet
{
    /// <summary>
    /// 片段唯一标识符
    /// </summary>
    public string SnippetId { get; set; } = string.Empty;

    /// <summary>
    /// 书籍中心 ID
    /// </summary>
    public string BookHubId { get; set; } = string.Empty;

    /// <summary>
    /// 所属文档 ID
    /// </summary>
    public string DocId { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 标题路径（用于显示原文位置）
    /// </summary>
    public List<string> HeadingPath { get; set; } = new();

    /// <summary>
    /// 片段内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 片段在源文件中的起始行号
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// 片段在源文件中的结束行号
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// 关联的 chunk ID（用于 LightRAG 检索）
    /// </summary>
    public string? ChunkId { get; set; }
}
