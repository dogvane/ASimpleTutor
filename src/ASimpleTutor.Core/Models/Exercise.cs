using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ASimpleTutor.Core.Models;

/// <summary>
/// 练习题
/// </summary>
public class Exercise
{
    /// <summary>
    /// 习题唯一标识符
    /// </summary>
    public string ExerciseId { get; set; } = string.Empty;

    /// <summary>
    /// 关联的知识点 ID
    /// </summary>
    public string KpId { get; set; } = string.Empty;

    /// <summary>
    /// 习题类型
    /// </summary>
    [JsonConverter(typeof(ExerciseTypeConverter))]
    public ExerciseType Type { get; set; }

    /// <summary>
    /// 难度等级（1-5，1最简单，5最难）
    /// </summary>
    public int Difficulty { get; set; } = 1;

    /// <summary>
    /// 题目内容
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// 选项列表（选择题、判断题使用）
    /// </summary>
    public List<string> Options { get; set; } = new();

    /// <summary>
    /// 正确答案
    /// </summary>
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 答案解释
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// 证据原文片段 ID 列表
    /// </summary>
    public List<string> EvidenceSnippetIds { get; set; } = new();

    /// <summary>
    /// 考查要点列表
    /// </summary>
    public List<string> KeyPoints { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 习题类型
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ExerciseType
{
    /// <summary>
    /// 单选题
    /// </summary>
    SingleChoice,

    /// <summary>
    /// 多选题
    /// </summary>
    MultiChoice,

    /// <summary>
    /// 判断题
    /// </summary>
    TrueFalse,

    /// <summary>
    /// 简答题
    /// </summary>
    ShortAnswer
}

/// <summary>
/// 习题类型转换器 - 处理中文类型名称
/// </summary>
public class ExerciseTypeConverter : JsonConverter
{
    private static readonly Dictionary<string, ExerciseType> Mapping = new(StringComparer.OrdinalIgnoreCase)
    {
        // 单选题
        ["选择题"] = ExerciseType.SingleChoice,
        ["单选"] = ExerciseType.SingleChoice,
        ["singlechoice"] = ExerciseType.SingleChoice,
        ["single"] = ExerciseType.SingleChoice,
        ["choice"] = ExerciseType.SingleChoice,
        // 多选题
        ["多选"] = ExerciseType.MultiChoice,
        ["multichoice"] = ExerciseType.MultiChoice,
        ["multiple"] = ExerciseType.MultiChoice,
        // 判断题
        ["判断题"] = ExerciseType.TrueFalse,
        ["判断"] = ExerciseType.TrueFalse,
        ["truefalse"] = ExerciseType.TrueFalse,
        ["tf"] = ExerciseType.TrueFalse,
        // 简答题
        ["简答题"] = ExerciseType.ShortAnswer,
        ["简答"] = ExerciseType.ShortAnswer,
        ["shortanswer"] = ExerciseType.ShortAnswer,
        ["short"] = ExerciseType.ShortAnswer
    };

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ExerciseType);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.Value == null)
        {
            return ExerciseType.SingleChoice; // 默认值
        }

        var value = reader.Value.ToString()?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return ExerciseType.SingleChoice;
        }

        if (Mapping.TryGetValue(value, out var result))
        {
            return result;
        }

        // 尝试直接解析枚举名
        try
        {
            return Enum.Parse<ExerciseType>(value, ignoreCase: true);
        }
        catch
        {
            // 如果都无法解析，返回默认值
            return ExerciseType.SingleChoice;
        }
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is ExerciseType type)
        {
            writer.WriteValue(type.ToString());
        }
        else
        {
            writer.WriteValue("SingleChoice");
        }
    }
}

/// <summary>
/// 练习反馈
/// </summary>
public class ExerciseFeedback
{
    /// <summary>
    /// 是否正确
    /// </summary>
    public bool? IsCorrect { get; set; }

    /// <summary>
    /// 解释说明
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// 参考答案
    /// </summary>
    public string? ReferenceAnswer { get; set; }
}

/// <summary>
/// 答题请求
/// </summary>
public class ExerciseSubmission
{
    /// <summary>
    /// 习题 ID
    /// </summary>
    public string ExerciseId { get; set; } = string.Empty;

    /// <summary>
    /// 用户答案
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// 花费时间（秒）
    /// </summary>
    public int? TimeSpentSeconds { get; set; }
}

/// <summary>
/// 批量答题请求
/// </summary>
public class BatchSubmission
{
    /// <summary>
    /// 知识点 ID
    /// </summary>
    public string KpId { get; set; } = string.Empty;

    /// <summary>
    /// 答题列表
    /// </summary>
    public List<ExerciseSubmission> Submissions { get; set; } = new();
}
