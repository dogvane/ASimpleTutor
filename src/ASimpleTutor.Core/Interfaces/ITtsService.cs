namespace ASimpleTutor.Core.Interfaces;

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
}
