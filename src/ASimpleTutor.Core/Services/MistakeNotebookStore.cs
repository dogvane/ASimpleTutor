using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 错题本持久化存储服务
/// </summary>
public class MistakeNotebookStore
{
    private readonly string _storePath;
    private readonly ILogger<MistakeNotebookStore> _logger;
    private readonly object _lock = new();

    public MistakeNotebookStore(ILogger<MistakeNotebookStore> logger, string baseDataDirectory)
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

            _logger.LogInformation("错题本存储目录: {Path}", _storePath);
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
    /// 异步保存错题本到文件
    /// </summary>
    public async Task SaveAsync(string bookHubId, string userId, List<MistakeRecord> mistakes, CancellationToken cancellationToken = default)
    {
        if (mistakes == null || mistakes.Count == 0)
        {
            _logger.LogWarning("尝试保存空错题本列表");
            return;
        }

        var directory = Path.Combine(_storePath, bookHubId);
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"mistake-notebook.{userId}.json");

        _logger.LogInformation("开始保存错题本: {BookHubId}, UserId: {UserId}, 错题数: {Count}",
            bookHubId, userId, mistakes.Count);

        try
        {
            var saveModel = new MistakeNotebookSaveModel
            {
                BookHubId = bookHubId,
                UserId = userId,
                Mistakes = mistakes,
                SavedAt = DateTime.UtcNow
            };

            var json = JsonConvert.SerializeObject(saveModel, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.LogInformation("错题本保存完成: {BookHubId}, UserId: {UserId}, 错题数: {Count}",
                bookHubId, userId, mistakes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存错题本失败: {BookHubId}, UserId: {UserId}", bookHubId, userId);
            throw;
        }
    }

    /// <summary>
    /// 异步从文件加载错题本
    /// </summary>
    public async Task<List<MistakeRecord>?> LoadAsync(string bookHubId, string userId, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(_storePath, bookHubId);
        var filePath = Path.Combine(directory, $"mistake-notebook.{userId}.json");

        if (!File.Exists(filePath))
        {
            _logger.LogInformation("错题本文件不存在: {FilePath}", filePath);
            return new List<MistakeRecord>();
        }

        _logger.LogInformation("开始加载错题本: {BookHubId}, UserId: {UserId}", bookHubId, userId);

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var saveModel = JsonConvert.DeserializeObject<MistakeNotebookSaveModel>(json);

            if (saveModel == null)
            {
                _logger.LogWarning("错题本反序列化失败: {BookHubId}, UserId: {UserId}", bookHubId, userId);
                return new List<MistakeRecord>();
            }

            _logger.LogInformation("错题本加载完成: {BookHubId}, UserId: {UserId}, 错题数: {Count}",
                bookHubId, userId, saveModel.Mistakes?.Count ?? 0);

            return saveModel.Mistakes ?? new List<MistakeRecord>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载错题本失败: {BookHubId}, UserId: {UserId}", bookHubId, userId);
            return new List<MistakeRecord>();
        }
    }

    /// <summary>
    /// 同步保存错题本
    /// </summary>
    public void Save(string bookHubId, string userId, List<MistakeRecord> mistakes)
    {
        SaveAsync(bookHubId, userId, mistakes).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 同步加载错题本
    /// </summary>
    public List<MistakeRecord>? Load(string bookHubId, string userId)
    {
        return LoadAsync(bookHubId, userId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 添加错题记录
    /// </summary>
    public void AddMistake(string bookHubId, string userId, MistakeRecord mistake)
    {
        lock (_lock)
        {
            var mistakes = Load(bookHubId, userId) ?? new List<MistakeRecord>();

            // 检查是否已有相同习题的未解决错题记录
            var existingRecord = mistakes
                .FirstOrDefault(m => m.ExerciseId == mistake.ExerciseId && m.KpId == mistake.KpId && !m.IsResolved);

            if (existingRecord != null)
            {
                existingRecord.ErrorCount++;
                existingRecord.UserAnswer = mistake.UserAnswer;
                existingRecord.ErrorAnalysis = mistake.ErrorAnalysis;
                _logger.LogInformation("更新现有错题记录: {RecordId}, 错误次数: {ErrorCount}",
                    existingRecord.RecordId, existingRecord.ErrorCount);
            }
            else
            {
                mistakes.Add(mistake);
                _logger.LogInformation("添加新错题记录: {RecordId}", mistake.RecordId);
            }

            Save(bookHubId, userId, mistakes);
        }
    }

    /// <summary>
    /// 解决错题
    /// </summary>
    public bool ResolveMistake(string bookHubId, string userId, string recordId)
    {
        lock (_lock)
        {
            var mistakes = Load(bookHubId, userId);
            if (mistakes == null)
                return false;

            var record = mistakes.FirstOrDefault(m => m.RecordId == recordId);
            if (record == null)
                return false;

            record.IsResolved = true;
            record.ResolvedAt = DateTime.UtcNow;

            Save(bookHubId, userId, mistakes);

            _logger.LogInformation("错题已标记为解决: {RecordId}", recordId);
            return true;
        }
    }

    /// <summary>
    /// 获取未解决的错题
    /// </summary>
    public List<MistakeRecord> GetUnresolvedMistakes(string bookHubId, string userId)
    {
        var mistakes = Load(bookHubId, userId);
        if (mistakes == null)
            return new List<MistakeRecord>();

        return mistakes
            .Where(m => !m.IsResolved)
            .OrderByDescending(m => m.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// 检查是否存在错题本
    /// </summary>
    public bool Exists(string bookHubId, string userId)
    {
        var directory = Path.Combine(_storePath, bookHubId);
        var filePath = Path.Combine(directory, $"mistake-notebook.{userId}.json");
        return File.Exists(filePath);
    }

    /// <summary>
    /// 删除错题本
    /// </summary>
    public bool Delete(string bookHubId, string userId)
    {
        var directory = Path.Combine(_storePath, bookHubId);
        var filePath = Path.Combine(directory, $"mistake-notebook.{userId}.json");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("错题本已删除: {BookHubId}, UserId: {UserId}", bookHubId, userId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取所有有错题记录的用户 ID
    /// </summary>
    public List<string> GetAllUserIds(string bookHubId)
    {
        var directory = Path.Combine(_storePath, bookHubId);

        if (!Directory.Exists(directory))
        {
            return new List<string>();
        }

        return Directory.GetFiles(directory, "mistake-notebook.*.json")
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name =>
            {
                // 从 "mistake-notebook.{userId}.json" 提取 userId
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
/// 错题本保存模型
/// </summary>
internal class MistakeNotebookSaveModel
{
    [JsonProperty("book_hub_id")]
    public string BookHubId { get; set; } = string.Empty;

    [JsonProperty("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonProperty("mistakes")]
    public List<MistakeRecord> Mistakes { get; set; } = new();

    [JsonProperty("saved_at")]
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
