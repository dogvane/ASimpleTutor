using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 学习内容生成服务接口
/// </summary>
public interface ILearningGenerator
{
    /// <summary>
    /// 生成学习内容
    /// </summary>
    /// <param name="kp">知识点</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>学习内容包</returns>
    Task<LearningPack> GenerateAsync(KnowledgePoint kp, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新幻灯片卡片的语音脚本（只生成缺失的脚本）
    /// </summary>
    /// <param name="slideCards">幻灯片卡片列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateSpeechScriptsAsync(List<SlideCard> slideCards, CancellationToken cancellationToken = default);
}
