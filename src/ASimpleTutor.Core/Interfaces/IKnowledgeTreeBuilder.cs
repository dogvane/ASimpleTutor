using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 知识树构建服务接口
/// 负责将知识点组织成树状结构
/// </summary>
public interface IKnowledgeTreeBuilder
{
    /// <summary>
    /// 构建知识树
    /// </summary>
    /// <param name="knowledgePoints">知识点列表</param>
    /// <returns>知识树结构</returns>
    KnowledgeTreeNode Build(List<KnowledgePoint> knowledgePoints);
}