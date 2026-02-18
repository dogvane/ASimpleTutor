using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 知识体系协调器
/// 负责协调整个知识体系构建流程
/// </summary>
public class KnowledgeSystemCoordinator : IKnowledgeSystemCoordinator
{
    private readonly IScannerService _scannerService;
    private readonly IKnowledgeExtractor _knowledgeExtractor;
    private readonly ILearningContentGenerator _learningContentGenerator;
    private readonly ITtsGenerator _ttsGenerator;
    private readonly IKnowledgeTreeBuilder _knowledgeTreeBuilder;
    private readonly IKnowledgeGraphBuilder _knowledgeGraphBuilder;
    private readonly KnowledgeSystemStore _store;
    private readonly ScanProgressService _progressService;
    private readonly ILogger<KnowledgeSystemCoordinator> _logger;

    public KnowledgeSystemCoordinator(
        IScannerService scannerService,
        IKnowledgeExtractor knowledgeExtractor,
        ILearningContentGenerator learningContentGenerator,
        ITtsGenerator ttsGenerator,
        IKnowledgeTreeBuilder knowledgeTreeBuilder,
        IKnowledgeGraphBuilder knowledgeGraphBuilder,
        KnowledgeSystemStore store,
        ScanProgressService progressService,
        ILogger<KnowledgeSystemCoordinator> logger)
    {
        _scannerService = scannerService;
        _knowledgeExtractor = knowledgeExtractor;
        _learningContentGenerator = learningContentGenerator;
        _ttsGenerator = ttsGenerator;
        _knowledgeTreeBuilder = knowledgeTreeBuilder;
        _knowledgeGraphBuilder = knowledgeGraphBuilder;
        _store = store;
        _progressService = progressService;
        _logger = logger;
    }

    public async Task<(KnowledgeSystem KnowledgeSystem, List<Document> Documents)> BuildAsync(
        string bookHubId,
        string rootPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始构建知识体系: {BookHubId}", bookHubId);

        // 开始扫描进度跟踪
        _progressService.StartScan(bookHubId);

        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = bookHubId
        };

        List<Document> documents = new();

        try
        {
            // 1. 扫描文档
            _progressService.UpdateProgress(bookHubId, "扫描文档", 5, "正在扫描文档目录...");
            _logger.LogInformation("扫描文档目录: {RootPath}", rootPath);
            documents = await _scannerService.ScanAsync(rootPath, cancellationToken);

            if (documents.Count == 0)
            {
                _logger.LogWarning("未找到任何 Markdown 文档");
                _progressService.CompleteScan(bookHubId, 0);
                return (knowledgeSystem, documents);
            }

            // 过程保存：扫描完成后保存文档信息
            _progressService.UpdateProgress(bookHubId, "扫描文档", 10, $"扫描完成，发现 {documents.Count} 个文档");
            await SaveProgressAsync(knowledgeSystem, documents, "扫描完成", cancellationToken);

            // 2. 调用 LLM 提取知识点
            _progressService.UpdateProgress(bookHubId, "提取知识点", 20, "正在调用 LLM 提取知识点...");
            _logger.LogInformation("调用 LLM 提取知识点");
            var knowledgePoints = await _knowledgeExtractor.ExtractAsync(documents, cancellationToken);
            knowledgeSystem.KnowledgePoints = knowledgePoints;

            // 过程保存：知识点提取完成后保存
            _progressService.UpdateProgress(bookHubId, "提取知识点", 40, $"知识点提取完成，共 {knowledgePoints.Count} 个知识点");
            await SaveProgressAsync(knowledgeSystem, documents, "知识点提取完成", cancellationToken);

            // 3. 构建知识树
            _progressService.UpdateProgress(bookHubId, "构建知识树", 45, "正在构建知识树...");
            _logger.LogInformation("构建知识树");
            knowledgeSystem.Tree = _knowledgeTreeBuilder.Build(knowledgePoints);

            // 4. 构建知识图谱（为学习内容生成提供关联信息）
            _progressService.UpdateProgress(bookHubId, "构建知识图谱", 48, "正在构建知识图谱...");
            _logger.LogInformation("构建知识图谱");
            try
            {
                knowledgeSystem.Graph = await _knowledgeGraphBuilder.BuildAsync(bookHubId);
                _logger.LogInformation("知识图谱构建完成，包含 {NodeCount} 个节点和 {EdgeCount} 条边",
                    knowledgeSystem.Graph?.Nodes.Count ?? 0,
                    knowledgeSystem.Graph?.Edges.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "知识图谱构建失败，将跳过此步骤");
                // 知识图谱构建失败不影响整体流程
            }

            // 过程保存：知识图谱构建完成后保存
            _progressService.UpdateProgress(bookHubId, "构建知识图谱", 50, "知识图谱构建完成");
            await SaveProgressAsync(knowledgeSystem, documents, "知识图谱构建完成", cancellationToken);

            // 5. 为每个知识点预生成学习内容（此时可以使用知识图谱数据）
            _progressService.UpdateProgress(bookHubId, "生成学习内容", 55, "正在为知识点生成学习内容...");
            _logger.LogInformation("为知识点预生成学习内容");
            await GenerateLearningContentForPointsAsync(knowledgePoints, documents, bookHubId, cancellationToken);

            // 过程保存：学习内容生成完成后保存
            _progressService.UpdateProgress(bookHubId, "生成学习内容", 75, "学习内容生成完成");
            await SaveProgressAsync(knowledgeSystem, documents, "学习内容生成完成", cancellationToken);

            // 6. 为幻灯片卡片生成 TTS 音频
            _progressService.UpdateProgress(bookHubId, "生成音频", 80, "正在为幻灯片生成 TTS 音频...");
            _logger.LogInformation("为幻灯片卡片生成 TTS 音频");
            await GenerateTtsForSlideCardsAsync(knowledgePoints, bookHubId, cancellationToken);

            // 过程保存：TTS 生成完成后保存
            _progressService.UpdateProgress(bookHubId, "生成音频", 95, "TTS 音频生成完成");
            await SaveProgressAsync(knowledgeSystem, documents, "TTS 生成完成", cancellationToken);

            // 最终保存：构建完成
            await SaveProgressAsync(knowledgeSystem, documents, "构建完成", cancellationToken);

            // 标记扫描完成
            _progressService.CompleteScan(bookHubId, knowledgePoints.Count);
            _logger.LogInformation("知识体系构建完成，共 {Count} 个知识点", knowledgePoints.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "知识体系构建失败");
            _progressService.FailScan(bookHubId, ex.Message);
            // 尝试保存已完成的进度
            await SaveProgressAsync(knowledgeSystem, documents, "构建失败-保存进度", cancellationToken);
            // 降级：返回按文件/标题的目录树
            knowledgeSystem = CreateFallbackKnowledgeSystem(bookHubId, documents);
        }

        return (knowledgeSystem, documents);
    }

    /// <summary>
    /// 保存构建进度
    /// </summary>
    private async Task SaveProgressAsync(KnowledgeSystem knowledgeSystem, List<Document> documents, string stage, CancellationToken cancellationToken)
    {
        try
        {
            await _store.SaveAsync(knowledgeSystem, documents, cancellationToken);
            _logger.LogInformation("过程保存完成 [{Stage}]: {BookHubId}, 知识点: {KpCount}",
                stage, knowledgeSystem.BookHubId, knowledgeSystem.KnowledgePoints.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "过程保存失败 [{Stage}]: {BookHubId}", stage, knowledgeSystem.BookHubId);
            // 过程保存失败不影响主流程
        }
    }

    private async Task GenerateLearningContentForPointsAsync(
        List<KnowledgePoint> knowledgePoints,
        List<Document> documents,
        string bookHubId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始为 {Count} 个知识点生成学习内容", knowledgePoints.Count);

        var processedCount = 0;
        var totalCount = knowledgePoints.Count;

        // 使用 Parallel.ForEachAsync 并发处理所有知识点
        await Parallel.ForEachAsync(knowledgePoints, new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = 3 },
            async (kp, cancellationToken) =>
            {
                try
                {
                    _logger.LogDebug("生成学习内容: {KpId}", kp.KpId);

                    var learningPack = await _learningContentGenerator.GenerateAsync(kp, documents, cancellationToken);

                    if (learningPack != null)
                    {
                        kp.Summary = learningPack.Summary;
                        kp.Levels = learningPack.Levels;
                        kp.SlideCards = learningPack.SlideCards;
                    }

                    // 更新进度
                    var current = Interlocked.Increment(ref processedCount);
                    if (current % 5 == 0 || current == totalCount) // 每5个更新一次进度
                    {
                        var percent = 50 + (current * 20 / totalCount); // 50-70% 区间
                        _progressService.UpdateProgress(bookHubId, "生成学习内容", percent, $"正在生成学习内容 ({current}/{totalCount})...", current, totalCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "知识点 {KpId} 学习内容生成失败，使用降级内容", kp.KpId);
                }
            });

        _logger.LogInformation("已完成 {CompletedCount}/{TotalCount} 个知识点的学习内容生成", knowledgePoints.Count(kp => kp.Summary != null), knowledgePoints.Count);
        _logger.LogInformation("学习内容生成完成");
    }

    private async Task GenerateTtsForSlideCardsAsync(
        List<KnowledgePoint> knowledgePoints,
        string bookHubId,
        CancellationToken cancellationToken)
    {
        var allSlideCards = knowledgePoints
            .SelectMany(kp => kp.SlideCards)
            .ToList();

        await _ttsGenerator.GenerateAsync(allSlideCards, bookHubId, cancellationToken);
    }

    private KnowledgeSystem CreateFallbackKnowledgeSystem(string bookHubId, List<Document> documents)
    {
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = bookHubId
        };

        // 从文档标题创建临时知识点
        var id = 0;
        foreach (var doc in documents)
        {
            var kp = new KnowledgePoint
            {
                KpId = $"kp_{id++:D4}",
                BookHubId = bookHubId,
                Title = doc.Title,
                Type = KpType.Chapter,
                ChapterPath = new List<string> { doc.Title },
                Importance = 0.5f,
                SectionId = "",
                DocId = doc.DocId
            };
            knowledgeSystem.KnowledgePoints.Add(kp);
        }

        knowledgeSystem.Tree = _knowledgeTreeBuilder.Build(knowledgeSystem.KnowledgePoints);

        return knowledgeSystem;
    }
}