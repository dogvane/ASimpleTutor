using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Api.Controllers;

/// <summary>
/// 设置管理控制器
/// </summary>
[ApiController]
[Route("api/v1/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ISettingsService settingsService,
        ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// 获取 LLM 配置
    /// </summary>
    [HttpGet("llm")]
    public async Task<IActionResult> GetLlmSettings()
    {
        try
        {
            var settings = await _settingsService.GetLlmSettingsAsync();
            return Ok(new { items = new[] { settings } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 LLM 配置失败");
            return BadRequest(new { error = new { code = "GET_SETTINGS_FAILED", message = "获取配置失败" } });
        }
    }

    /// <summary>
    /// 更新 LLM 配置
    /// </summary>
    [HttpPut("llm")]
    public async Task<IActionResult> UpdateLlmSettings([FromBody] LlmSettingsRequest request)
    {
        try
        {
            var settings = await _settingsService.UpdateLlmSettingsAsync(request);
            return Ok(new { success = true, message = "配置已保存并实时生效", items = new[] { settings } });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "配置参数无效");
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = ex.Message } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 LLM 配置失败");
            return BadRequest(new { error = new { code = "UPDATE_SETTINGS_FAILED", message = "更新配置失败" } });
        }
    }

    /// <summary>
    /// 测试 LLM 连接
    /// </summary>
    [HttpPost("llm/test")]
    public async Task<IActionResult> TestLlmConnection([FromBody] TestLlmConnectionRequest request)
    {
        try
        {
            var result = await _settingsService.TestLlmConnectionAsync(request);

            if (result.Success)
            {
                return Ok(new { success = true, message = result.Message, items = new[] { result } });
            }
            else
            {
                return BadRequest(new { error = new { code = "CONNECTION_FAILED", message = result.Message } });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试 LLM 连接失败");
            return BadRequest(new { error = new { code = "TEST_FAILED", message = "连接测试失败" } });
        }
    }

    /// <summary>
    /// 获取 TTS 配置
    /// </summary>
    [HttpGet("tts")]
    public async Task<IActionResult> GetTtsSettings()
    {
        try
        {
            var settings = await _settingsService.GetTtsSettingsAsync();
            return Ok(new { items = new[] { settings } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 TTS 配置失败");
            return BadRequest(new { error = new { code = "GET_SETTINGS_FAILED", message = "获取配置失败" } });
        }
    }

    /// <summary>
    /// 更新 TTS 配置
    /// </summary>
    [HttpPut("tts")]
    public async Task<IActionResult> UpdateTtsSettings([FromBody] TtsSettingsRequest request)
    {
        try
        {
            var settings = await _settingsService.UpdateTtsSettingsAsync(request);
            return Ok(new { success = true, message = "配置已保存并实时生效", items = new[] { settings } });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "配置参数无效");
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = ex.Message } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 TTS 配置失败");
            return BadRequest(new { error = new { code = "UPDATE_SETTINGS_FAILED", message = "更新配置失败" } });
        }
    }
}
