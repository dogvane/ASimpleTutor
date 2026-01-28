using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 练习反馈服务接口
/// </summary>
public interface IExerciseFeedback
{
    /// <summary>
    /// 评判答案
    /// </summary>
    /// <param name="exercise">练习题</param>
    /// <param name="userAnswer">用户答案</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>答题反馈</returns>
    Task<ExerciseFeedback> JudgeAsync(Exercise exercise, string userAnswer, CancellationToken cancellationToken = default);
}
