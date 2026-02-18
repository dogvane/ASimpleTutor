using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 知识点提取服务接口
/// 负责从解析后的文档中提取知识点
/// </summary>
public interface IKnowledgeExtractor
{
    /// <summary>
    /// 从文档中提取知识点
    /// </summary>
    /// <param name="documents">解析后的文档列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>知识点列表</returns>
    Task<List<KnowledgePoint>> ExtractAsync(List<Document> documents, CancellationToken cancellationToken = default);
}