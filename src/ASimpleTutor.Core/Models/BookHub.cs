namespace ASimpleTutor.Core.Models;

/// <summary>
/// 书籍中心配置
/// </summary>
public class BookHub
{
    /// <summary>
    /// 书籍中心唯一标识符
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 书籍中心显示名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 书籍中心的本地文件系统路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 参考书目目录名称列表，扫描时将排除这些目录
    /// </summary>
    public List<string> ReferenceDirNames { get; set; } = new() { "references", "参考书目" };

    /// <summary>
    /// 排除的文件 glob 模式列表
    /// </summary>
    public List<string> ExcludeGlobs { get; set; } = new();

    /// <summary>
    /// 是否启用该书籍中心
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 显示顺序（数值越小越靠前）
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// 当前激活的书籍中心
/// </summary>
public class ActiveBookHub
{
    /// <summary>
    /// 当前激活的书籍中心 ID
    /// </summary>
    public string? ActiveBookHubId { get; set; }
}
