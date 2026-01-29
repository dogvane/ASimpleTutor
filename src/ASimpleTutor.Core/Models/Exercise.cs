using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ASimpleTutor.Core.Models;

/// <summary>
/// 练习题
/// </summary>
public class Exercise
{
    public string ExerciseId { get; set; } = string.Empty;
    public string KpId { get; set; } = string.Empty;

    [JsonConverter(typeof(ExerciseTypeConverter))]
    public ExerciseType Type { get; set; }

    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public List<string> EvidenceSnippetIds { get; set; } = new();
    public List<string> KeyPoints { get; set; } = new();
}

/// <summary>
/// 习题类型
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ExerciseType
{
    SingleChoice,   // 选择题
    FillBlank,      // 填空题
    ShortAnswer     // 简答题
}

/// <summary>
/// 习题类型转换器 - 处理中文类型名称
/// </summary>
public class ExerciseTypeConverter : JsonConverter
{
    private static readonly Dictionary<string, ExerciseType> Mapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["选择题"] = ExerciseType.SingleChoice,
        ["单选"] = ExerciseType.SingleChoice,
        ["singlechoice"] = ExerciseType.SingleChoice,
        ["single"] = ExerciseType.SingleChoice,
        ["choice"] = ExerciseType.SingleChoice,
        ["填空题"] = ExerciseType.FillBlank,
        ["填空"] = ExerciseType.FillBlank,
        ["fillblank"] = ExerciseType.FillBlank,
        ["fill"] = ExerciseType.FillBlank,
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
    /// 是否正确（填空/简答可能为 null 表示不确定）
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

    /// <summary>
    /// 覆盖的要点
    /// </summary>
    public List<string> CoveredPoints { get; set; } = new();

    /// <summary>
    /// 遗漏的要点
    /// </summary>
    public List<string> MissingPoints { get; set; } = new();
}
