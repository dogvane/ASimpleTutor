using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// TTS 音频生成服务接口
/// 负责为幻灯片卡片生成 TTS 音频
/// </summary>
public interface ITtsGenerator
{
    /// <summary>
    /// 为幻灯片卡片生成 TTS 音频
    /// </summary>
    /// <param name="slideCards">幻灯片卡片列表</param>
    /// <param name="bookHubId">书籍中心 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task GenerateAsync(List<SlideCard> slideCards, string bookHubId, CancellationToken cancellationToken = default);
}