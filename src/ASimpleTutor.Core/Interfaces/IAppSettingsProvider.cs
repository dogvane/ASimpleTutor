namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 应用配置提供者接口
/// </summary>
public interface IAppSettingsProvider
{
    /// <summary>
    /// 获取当前 LLM 配置
    /// </summary>
    ASimpleTutor.Core.Models.Dto.LlmSettingsResponse GetLlmSettings();

    /// <summary>
    /// 获取当前 TTS 配置
    /// </summary>
    ASimpleTutor.Core.Models.Dto.TtsSettingsResponse GetTtsSettings();

    /// <summary>
    /// 是否启用 TTS 功能
    /// </summary>
    bool GetTtsEnabled();
}
