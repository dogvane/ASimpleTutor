using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 知识体系协调服务接口
/// 负责协调整个知识体系构建流程
/// </summary>
public interface IKnowledgeSystemCoordinator
{
    /// <summary>
    /// 构建知识体系
    /// </summary>
    /// <param name="bookHubId">书籍中心 ID</param>
    /// <param name="rootPath">文档根路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>知识体系和文档列表</returns>
    Task<(KnowledgeSystem KnowledgeSystem, List<Document> Documents)> BuildAsync(
        string bookHubId,
        string rootPath,
        CancellationToken cancellationToken = default);
}