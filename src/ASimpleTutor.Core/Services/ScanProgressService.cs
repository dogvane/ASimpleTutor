using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 扫描进度信息
/// </summary>
public class ScanProgress
{
    /// <summary>
    /// 扫描任务ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// 当前状态：idle, scanning, completed, failed
    /// </summary>
    public string Status { get; set; } = "idle";

    /// <summary>
    /// 当前阶段
    /// </summary>
    public string CurrentStage { get; set; } = string.Empty;

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    /// 已处理的知识点数量
    /// </summary>
    public int ProcessedKpCount { get; set; }

    /// <summary>
    /// 总知识点数量
    /// </summary>
    public int TotalKpCount { get; set; }

    /// <summary>
    /// 消息/日志
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdateTime { get; set; }
}

/// <summary>
/// 扫描进度跟踪服务
/// </summary>
public class ScanProgressService
{
    private readonly ConcurrentDictionary<string, ScanProgress> _progressMap = new();
    private readonly ILogger<ScanProgressService> _logger;

    public ScanProgressService(ILogger<ScanProgressService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 开始新的扫描任务
    /// </summary>
    public ScanProgress StartScan(string bookHubId)
    {
        var taskId = Guid.NewGuid().ToString("N")[..8];
        var progress = new ScanProgress
        {
            TaskId = taskId,
            Status = "scanning",
            CurrentStage = "初始化",
            ProgressPercent = 0,
            StartTime = DateTime.UtcNow,
            LastUpdateTime = DateTime.UtcNow,
            Message = "开始扫描..."
        };

        _progressMap[bookHubId] = progress;
        _logger.LogInformation("扫描任务开始: {TaskId}, BookHub: {BookHubId}", taskId, bookHubId);
        return progress;
    }

    /// <summary>
    /// 更新扫描进度
    /// </summary>
    public void UpdateProgress(string bookHubId, string stage, int percent, string message, int processedCount = 0, int totalCount = 0)
    {
        if (_progressMap.TryGetValue(bookHubId, out var progress))
        {
            progress.CurrentStage = stage;
            progress.ProgressPercent = percent;
            progress.Message = message;
            progress.ProcessedKpCount = processedCount;
            progress.TotalKpCount = totalCount;
            progress.LastUpdateTime = DateTime.UtcNow;
            _logger.LogDebug("扫描进度更新 [{BookHubId}]: {Stage} - {Percent}%", bookHubId, stage, percent);
        }
    }

    /// <summary>
    /// 标记扫描完成
    /// </summary>
    public void CompleteScan(string bookHubId, int totalKpCount)
    {
        if (_progressMap.TryGetValue(bookHubId, out var progress))
        {
            progress.Status = "completed";
            progress.CurrentStage = "完成";
            progress.ProgressPercent = 100;
            progress.TotalKpCount = totalKpCount;
            progress.Message = $"扫描完成，共 {totalKpCount} 个知识点";
            progress.LastUpdateTime = DateTime.UtcNow;
            _logger.LogInformation("扫描任务完成: {BookHubId}, 知识点: {Count}", bookHubId, totalKpCount);
        }
    }

    /// <summary>
    /// 标记扫描失败
    /// </summary>
    public void FailScan(string bookHubId, string error)
    {
        if (_progressMap.TryGetValue(bookHubId, out var progress))
        {
            progress.Status = "failed";
            progress.Error = error;
            progress.Message = $"扫描失败: {error}";
            progress.LastUpdateTime = DateTime.UtcNow;
            _logger.LogError("扫描任务失败: {BookHubId}, 错误: {Error}", bookHubId, error);
        }
    }

    /// <summary>
    /// 获取扫描进度
    /// </summary>
    public ScanProgress? GetProgress(string bookHubId)
    {
        _progressMap.TryGetValue(bookHubId, out var progress);
        return progress;
    }

    /// <summary>
    /// 清理过期任务（超过1小时的）
    /// </summary>
    public void CleanupOldTasks()
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var oldKeys = _progressMap
            .Where(x => x.Value.LastUpdateTime < cutoff)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in oldKeys)
        {
            _progressMap.TryRemove(key, out _);
            _logger.LogDebug("清理过期扫描任务: {BookHubId}", key);
        }
    }
}
