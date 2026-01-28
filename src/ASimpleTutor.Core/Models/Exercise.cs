namespace ASimpleTutor.Core.Models;

/// <summary>
/// 练习题
/// </summary>
public class Exercise
{
    public string ExerciseId { get; set; } = string.Empty;
    public string KpId { get; set; } = string.Empty;
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
public enum ExerciseType
{
    SingleChoice,   // 选择题
    FillBlank,      // 填空题
    ShortAnswer     // 简答题
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
