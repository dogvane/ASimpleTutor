using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 文档扫描服务接口
/// </summary>
public interface IScannerService
{
    /// <summary>
    /// 扫描指定目录的 Markdown 文档
    /// </summary>
    Task<List<Document>> ScanAsync(string rootPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 扫描指定目录的 Markdown 文档（带排除规则）
    /// </summary>
    Task<List<Document>> ScanAsync(string rootPath, List<string> excludeDirNames, CancellationToken cancellationToken = default);
}
