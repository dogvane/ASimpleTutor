using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 知识图谱构建服务接口
/// 负责将知识点转换为知识图谱结构
/// </summary>
public interface IKnowledgeGraphBuilder
{
    /// <summary>
    /// 构建知识图谱
    /// </summary>
    /// <param name="knowledgePoints">知识点列表</param>
    /// <param name="options">知识图谱构建选项</param>
    /// <returns>知识图谱</returns>
    KnowledgeGraph Build(List<KnowledgePoint> knowledgePoints, KnowledgeGraphBuildOptions? options = null);

    /// <summary>
    /// 根据书籍中心 ID 构建知识图谱
    /// </summary>
    /// <param name="bookHubId">书籍中心 ID</param>
    /// <param name="options">知识图谱构建选项</param>
    /// <returns>知识图谱</returns>
    Task<KnowledgeGraph> BuildAsync(string bookHubId, KnowledgeGraphBuildOptions? options = null);

    /// <summary>
    /// 获取知识图谱的子图
    /// </summary>
    /// <param name="graph">原始知识图谱</param>
    /// <param name="rootNodeId">根节点 ID</param>
    /// <param name="depth">子图深度</param>
    /// <returns>子图</returns>
    KnowledgeGraph GetSubgraph(KnowledgeGraph graph, string rootNodeId, int depth = 2);

    /// <summary>
    /// 搜索知识图谱节点
    /// </summary>
    /// <param name="graph">知识图谱</param>
    /// <param name="query">搜索关键词</param>
    /// <param name="maxResults">最大结果数</param>
    /// <returns>查询结果</returns>
    KnowledgeGraphQueryResult SearchNodes(KnowledgeGraph graph, string query, int maxResults = 10);

    /// <summary>
    /// 获取节点的邻居节点
    /// </summary>
    /// <param name="graph">知识图谱</param>
    /// <param name="nodeId">节点 ID</param>
    /// <param name="maxNeighbors">最大邻居数</param>
    /// <returns>邻居节点和关联边</returns>
    KnowledgeGraphQueryResult GetNeighbors(KnowledgeGraph graph, string nodeId, int maxNeighbors = 10);

    /// <summary>
    /// 计算节点之间的相似度（用于推荐相关知识点）
    /// </summary>
    /// <param name="node1">节点 1</param>
    /// <param name="node2">节点 2</param>
    /// <returns>相似度分数（0.0~1.0）</returns>
    float CalculateSimilarity(KnowledgeGraphNode node1, KnowledgeGraphNode node2);
}
