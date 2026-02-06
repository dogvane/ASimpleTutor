using Newtonsoft.Json;

namespace ASimpleTutor.Core.Models.Dto;

/// <summary>
/// LLM 响应数据结构（用于接收习题 JSON）
/// </summary>
public class ExercisesResponse
{
    [JsonProperty("exercises")]
    public List<ExerciseDto> Exercises { get; set; } = new();
}

/// <summary>
/// 习题 DTO（从 LLM 接收）
/// </summary>
public class ExerciseDto
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("difficulty")]
    public int Difficulty { get; set; } = 1;

    [JsonProperty("question")]
    public string? Question { get; set; }

    [JsonProperty("options")]
    public List<string>? Options { get; set; }

    [JsonProperty("correct_answer")]
    public string? CorrectAnswer { get; set; }

    [JsonProperty("key_points")]
    public List<string>? KeyPoints { get; set; }

    [JsonProperty("explanation")]
    public string? Explanation { get; set; }

    public Exercise ToExercise()
    {
        return new Exercise
        {
            Type = ParseExerciseType(Type),
            Difficulty = Math.Clamp(Difficulty, 1, 5),
            Question = Question ?? string.Empty,
            Options = Options ?? new List<string>(),
            CorrectAnswer = CorrectAnswer ?? string.Empty,
            KeyPoints = KeyPoints ?? new List<string>(),
            Explanation = Explanation
        };
    }

    private static ExerciseType ParseExerciseType(string? type)
    {
        if (string.IsNullOrEmpty(type))
            return ExerciseType.ShortAnswer;

        return type.ToLowerInvariant() switch
        {
            "singlechoice" or "single" or "choice" or "单选" or "选择题" => ExerciseType.SingleChoice,
            "multichoice" or "multi" or "multiple" or "多选" or "多选题" => ExerciseType.MultiChoice,
            "truefalse" or "tf" or "判断" or "判断题" => ExerciseType.TrueFalse,
            "shortanswer" or "short" or "简答" or "简答题" => ExerciseType.ShortAnswer,
            _ => ExerciseType.ShortAnswer
        };
    }
}

/// <summary>
/// 填空题/简答题反馈响应 DTO
/// </summary>
public class ShortAnswerFeedbackDto
{
    [JsonProperty("is_correct")]
    public bool? IsCorrect { get; set; }

    [JsonProperty("explanation")]
    public string Explanation { get; set; } = string.Empty;

    [JsonProperty("covered_points")]
    public List<string> CoveredPoints { get; set; } = new();

    [JsonProperty("missing_points")]
    public List<string> MissingPoints { get; set; } = new();
}
