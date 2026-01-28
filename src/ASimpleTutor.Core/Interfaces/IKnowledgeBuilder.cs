using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 知识体系构建服务接口
/// </summary>
public interface IKnowledgeBuilder
{
    /// <summary>
    /// 构建知识体系
    /// </summary>
    /// <param name="bookRootId">书籍目录ID</param>
    /// <param name="rootPath">书籍目录路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>知识系统</returns>
    Task<KnowledgeSystem> BuildAsync(string bookRootId, string rootPath, CancellationToken cancellationToken = default);
}
