using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 习题生成服务接口
/// </summary>
public interface IExerciseGenerator
{
    /// <summary>
    /// 生成练习题
    /// </summary>
    /// <param name="kp">知识点</param>
    /// <param name="count">题目数量（1~3）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>练习题列表</returns>
    Task<List<Exercise>> GenerateAsync(KnowledgePoint kp, int count = 1, CancellationToken cancellationToken = default);
}
