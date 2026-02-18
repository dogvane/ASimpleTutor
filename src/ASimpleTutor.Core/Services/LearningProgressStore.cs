using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 学习进度持久化存储服务
/// </summary>
public class LearningProgressStore
{
    private readonly string _storePath;
    private readonly ILogger<LearningProgressStore> _logger;
    private readonly object _lock = new();

    public LearningProgressStore(ILogger<LearningProgressStore> logger, string baseDataDirectory)
    {
        _logger = logger;

        // 检查参数是否为空
        if (string.IsNullOrEmpty(baseDataDirectory))
        {
            baseDataDirectory = "datas";
            _logger.LogWarning("存储目录参数为空，使用默认目录: {Directory}", baseDataDirectory);
        }

        try
        {
            // 保存目录：datas/../saves/ 即相对于 datas 目录的 saves 文件夹
            var combinedPath = Path.Combine(baseDataDirectory, "..", "saves");
            _storePath = Path.GetFullPath(combinedPath);

            if (!Directory.Exists(_storePath))
            {
                Directory.CreateDirectory(_storePath);
            }

            _logger.LogInformation("学习进度存储目录: {Path}", _storePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "路径处理失败，baseDataDirectory: '{BaseDataDirectory}'", baseDataDirectory);
            _storePath = "saves";
            if (!Directory.Exists(_storePath))
            {
                Directory.CreateDirectory(_storePath);
            }
        }
    }

    /// <summary>
    /// 异步保存学习进度到文件
    /// </summary>
    public async Task SaveAsync(string bookHubId, string userId, List<LearningProgress> progresses, CancellationToken cancellationToken = default)
    {
        if (progresses == null || progresses.Count == 0)
        {
            _logger.LogWarning("尝试保存空学习进度列表");
            return;
        }

        var directory = Path.Combine(_storePath, bookHubId);
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"learning-progress.{userId}.json");

        _logger.LogInformation("开始保存学习进度: {BookHubId}, UserId: {UserId}, 进度数: {Count}",
            bookHubId, userId, progresses.Count);

        try
        {
            var saveModel = new LearningProgressSaveModel
            {
                BookHubId = bookHubId,
                UserId = userId,
                Progresses = progresses,
                SavedAt = DateTime.UtcNow
            };

            var json = JsonConvert.SerializeObject(saveModel, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.LogInformation("学习进度保存完成: {BookHubId}, UserId: {UserId}, 进度数: {Count}",
                bookHubId, userId, progresses.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存学习进度失败: {BookHubId}, UserId: {UserId}", bookHubId, userId);
            throw;
        }
    }

    /// <summary>
    /// 异步从文件加载学习进度
    /// </summary>
    public async Task<List<LearningProgress>?> LoadAsync(string bookHubId, string userId, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(_storePath, bookHubId);
        var filePath = Path.Combine(directory, $"learning-progress.{userId}.json");

        if (!File.Exists(filePath))
        {
            _logger.LogInformation("学习进度文件不存在: {FilePath}", filePath);
            return new List<LearningProgress>();
        }

        _logger.LogInformation("开始加载学习进度: {BookHubId}, UserId: {UserId}", bookHubId, userId);

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var saveModel = JsonConvert.DeserializeObject<LearningProgressSaveModel>(json);

            if (saveModel == null)
            {
                _logger.LogWarning("学习进度反序列化失败: {BookHubId}, UserId: {UserId}", bookHubId, userId);
                return new List<LearningProgress>();
            }

            _logger.LogInformation("学习进度加载完成: {BookHubId}, UserId: {UserId}, 进度数: {Count}",
                bookHubId, userId, saveModel.Progresses?.Count ?? 0);

            return saveModel.Progresses ?? new List<LearningProgress>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载学习进度失败: {BookHubId}, UserId: {UserId}", bookHubId, userId);
            return new List<LearningProgress>();
        }
    }

    /// <summary>
    /// 同步保存学习进度
    /// </summary>
    public void Save(string bookHubId, string userId, List<LearningProgress> progresses)
    {
        SaveAsync(bookHubId, userId, progresses).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 同步加载学习进度
    /// </summary>
    public List<LearningProgress>? Load(string bookHubId, string userId)
    {
        return LoadAsync(bookHubId, userId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 获取单个知识点的学习进度
    /// </summary>
    public LearningProgress? GetProgress(string bookHubId, string userId, string kpId)
    {
        var progresses = Load(bookHubId, userId);
        if (progresses == null)
            return null;

        return progresses.FirstOrDefault(p => p.KpId == kpId);
    }

    /// <summary>
    /// 更新单个知识点的学习进度
    /// </summary>
    public void UpdateProgress(string bookHubId, string userId, LearningProgress progress)
    {
        lock (_lock)
        {
            var progresses = Load(bookHubId, userId) ?? new List<LearningProgress>();

            var existing = progresses.FirstOrDefault(p => p.KpId == progress.KpId);
            if (existing != null)
            {
                // 更新现有记录
                existing.Status = progress.Status;
                existing.MasteryLevel = progress.MasteryLevel;
                existing.ReviewCount = progress.ReviewCount;
                existing.LastReviewTime = progress.LastReviewTime;
                existing.CompletedSlideIds = progress.CompletedSlideIds;
            }
            else
            {
                // 添加新记录
                progresses.Add(progress);
            }

            Save(bookHubId, userId, progresses);

            _logger.LogInformation("学习进度已更新: {BookHubId}, UserId: {UserId}, KpId: {KpId}, Status: {Status}, Mastery: {Mastery}",
                bookHubId, userId, progress.KpId, progress.Status, progress.MasteryLevel);
        }
    }

    /// <summary>
    /// 增量添加已完成的幻灯片 ID
    /// </summary>
    public void AddCompletedSlideId(string bookHubId, string userId, string kpId, string slideId)
    {
        lock (_lock)
        {
            var progresses = Load(bookHubId, userId) ?? new List<LearningProgress>();

            var progress = progresses.FirstOrDefault(p => p.KpId == kpId);
            if (progress == null)
            {
                progress = new LearningProgress
                {
                    UserId = userId,
                    KpId = kpId,
                    Status = LearningStatus.Todo,
                    MasteryLevel = 0,
                    ReviewCount = 0,
                    CompletedSlideIds = new List<string> { slideId }
                };
                progresses.Add(progress);
            }
            else
            {
                if (progress.CompletedSlideIds == null)
                {
                    progress.CompletedSlideIds = new List<string>();
                }

                if (!progress.CompletedSlideIds.Contains(slideId))
                {
                    progress.CompletedSlideIds.Add(slideId);
                }
            }

            Save(bookHubId, userId, progresses);
        }
    }

    /// <summary>
    /// 检查是否存在学习进度
    /// </summary>
    public bool Exists(string bookHubId, string userId)
    {
        var directory = Path.Combine(_storePath, bookHubId);
        var filePath = Path.Combine(directory, $"learning-progress.{userId}.json");
        return File.Exists(filePath);
    }

    /// <summary>
    /// 删除学习进度
    /// </summary>
    public bool Delete(string bookHubId, string userId)
    {
        var directory = Path.Combine(_storePath, bookHubId);
        var filePath = Path.Combine(directory, $"learning-progress.{userId}.json");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("学习进度已删除: {BookHubId}, UserId: {UserId}", bookHubId, userId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取所有有学习进度的用户 ID
    /// </summary>
    public List<string> GetAllUserIds(string bookHubId)
    {
        var directory = Path.Combine(_storePath, bookHubId);

        if (!Directory.Exists(directory))
        {
            return new List<string>();
        }

        return Directory.GetFiles(directory, "learning-progress.*.json")
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name =>
            {
                // 从 "learning-progress.{userId}.json" 提取 userId
                var parts = name?.Split('.');
                if (parts != null && parts.Length >= 2)
                {
                    return parts[1]; // userId 在第二个位置
                }
                return string.Empty;
            })
            .Where(userId => !string.IsNullOrEmpty(userId))
            .ToList();
    }
}

/// <summary>
/// 学习进度保存模型
/// </summary>
internal class LearningProgressSaveModel
{
    [JsonProperty("book_hub_id")]
    public string BookHubId { get; set; } = string.Empty;

    [JsonProperty("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonProperty("progresses")]
    public List<LearningProgress> Progresses { get; set; } = new();

    [JsonProperty("saved_at")]
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
