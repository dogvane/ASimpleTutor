using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 学习内容生成服务接口
/// </summary>
public interface ILearningContentGenerator
{
    /// <summary>
    /// 为指定知识点生成学习内容
    /// </summary>
    /// <param name="kp">知识点</param>
    /// <param name="documents">文档列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>学习内容包</returns>
    Task<LearningPack?> GenerateAsync(KnowledgePoint kp, List<Document> documents, CancellationToken cancellationToken = default);
}