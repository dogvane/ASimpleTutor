namespace ASimpleTutor.Api.Interfaces;

/// <summary>
/// TTS 服务接口
/// </summary>
public interface ITtsService
{
    /// <summary>
    /// 根据口语文本生成或获取音频文件 URL
    /// </summary>
    /// <param name="speechScript">口语化讲解脚本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>音频文件相对 URL（如 /audios/xxx.mp3），如果生成失败则返回 null</returns>
    Task<string?> GetAudioUrlAsync(string speechScript, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新 TTS 配置
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="baseUrl">API 基础 URL</param>
    /// <param name="voice">语音模型</param>
    void UpdateConfig(string apiKey, string baseUrl, string voice);
}
