namespace ASimpleTutor.Core.Models;

/// <summary>
/// 文档（主书内容文档）
/// </summary>
public class Document
{
    /// <summary>
    /// 文档唯一标识符
    /// </summary>
    public string DocId { get; set; } = string.Empty;

    /// <summary>
    /// 所属书籍目录 ID
    /// </summary>
    public string BookRootId { get; set; } = string.Empty;

    /// <summary>
    /// 文档的本地文件系统完整路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 文档标题（通常从 H1 标题提取）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 文档章节列表
    /// </summary>
    public List<Section> Sections { get; set; } = new();

    /// <summary>
    /// 文档内容的 SHA256 哈希值，用于检测变更
    /// </summary>
    public string? ContentHash { get; set; }
}

/// <summary>
/// 文档章节
/// </summary>
public class Section
{
    /// <summary>
    /// 章节唯一标识符
    /// </summary>
    public string SectionId { get; set; } = string.Empty;

    /// <summary>
    /// 标题路径（从根标题到当前章节的层级路径）
    /// </summary>
    public List<string> HeadingPath { get; set; } = new();

    /// <summary>
    /// 章节内的段落列表
    /// </summary>
    public List<Paragraph> Paragraphs { get; set; } = new();
}

/// <summary>
/// 段落
/// </summary>
public class Paragraph
{
    /// <summary>
    /// 段落唯一标识符
    /// </summary>
    public string ParagraphId { get; set; } = string.Empty;

    /// <summary>
    /// 段落在文档中的起始行号（从 0 开始）
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// 段落在文档中的结束行号（从 0 开始）
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// 段落内容文本
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 段落类型
    /// </summary>
    public ParagraphType Type { get; set; } = ParagraphType.Text;
}

/// <summary>
/// 段落类型
/// </summary>
public enum ParagraphType
{
    /// <summary>
    /// 普通文本段落
    /// </summary>
    Text,

    /// <summary>
    /// 代码块
    /// </summary>
    Code,

    /// <summary>
    /// 引用块
    /// </summary>
    Quote,

    /// <summary>
    /// 列表项
    /// </summary>
    List
}
