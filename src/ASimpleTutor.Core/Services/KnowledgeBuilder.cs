using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 知识体系构建服务（向后兼容版本）
/// 内部调用 KnowledgeSystemCoordinator 来实现功能
/// </summary>
public class KnowledgeBuilder : IKnowledgeBuilder
{
    private readonly IKnowledgeSystemCoordinator _coordinator;

    public KnowledgeBuilder(IKnowledgeSystemCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public Task<(KnowledgeSystem KnowledgeSystem, List<Document> Documents)> BuildAsync(string bookHubId, string rootPath, CancellationToken cancellationToken = default)
    {
        return _coordinator.BuildAsync(bookHubId, rootPath, cancellationToken);
    }
}