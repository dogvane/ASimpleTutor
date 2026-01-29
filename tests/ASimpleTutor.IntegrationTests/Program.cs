using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ASimpleTutor.IntegrationTests;

/// <summary>
/// 控制台集成测试入口程序
/// 测试文档扫描和学习内容生成模块
/// </summary>
public class Program
{
    private static ILogger<MarkdownScanner> _scannerLogger = null!;
    private static ILogger<SourceTracker> _trackerLogger = null!;
    private static ILogger<LearningGenerator> _generatorLogger = null!;
    private static ILogger<KnowledgeBuilder> _builderLogger = null!;

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("ASimpleTutor 集成测试");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine();

        // 1. 加载配置
        // 确定项目目录（配置文件在项目目录下，不在 bin 目录下）
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        Console.WriteLine($"BasePath: {projectDir}");
        Console.WriteLine($"appsettings.json 路径: {Path.Combine(projectDir, "appsettings.json")}");
        Console.WriteLine($"appsettings.local.json 路径: {Path.Combine(projectDir, "appsettings.local.json")}");
        Console.WriteLine();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(projectDir)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // 读取 LLM 配置
        var llmConfig = configuration.GetSection("Llm").Get<LlmConfig>();
        Console.WriteLine("[配置读取完成]");
        Console.WriteLine($"  Llm:ApiKey = {(string.IsNullOrEmpty(llmConfig?.ApiKey) ? "空" : llmConfig.ApiKey.Substring(0, Math.Min(8, llmConfig.ApiKey.Length)) + "...")}");
        Console.WriteLine($"  Llm:BaseUrl = {llmConfig?.BaseUrl ?? "空"}");
        Console.WriteLine($"  Llm:Model = {llmConfig?.Model ?? "空"}");
        Console.WriteLine($"  环境变量 OPENAI_API_KEY = {(Environment.GetEnvironmentVariable("OPENAI_API_KEY") != null ? "已设置" : "未设置")}");
        Console.WriteLine();

        var config = configuration.GetSection("Test").Get<TestConfig>() ?? new TestConfig();

        // 解析路径（相对于项目根目录）
        // AppContext.BaseDirectory = tests/ASimpleTutor.IntegrationTests/bin/Debug/net10.0/
        // 需要向上 5 级到达项目根目录，然后访问 tests/datas/hello-agents
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var dataPath = Path.Combine(projectRoot, "tests", "datas", "hello-agents");

        // 输出路径改为与数据目录同级别，例如: tests/hello-agents-output
        var dataDirName = Path.GetFileName(dataPath);
        var outputPath = Path.Combine(projectRoot, "tests", $"{dataDirName}-output");

        // 确保输出目录存在
        Directory.CreateDirectory(outputPath);

        Console.WriteLine($"数据路径: {dataPath}");
        Console.WriteLine($"输出路径: {outputPath}");
        Console.WriteLine();

        // 初始化日志
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        _scannerLogger = loggerFactory.CreateLogger<MarkdownScanner>();
        _trackerLogger = loggerFactory.CreateLogger<SourceTracker>();
        _generatorLogger = loggerFactory.CreateLogger<LearningGenerator>();
        _builderLogger = loggerFactory.CreateLogger<KnowledgeBuilder>();

        // 2. 手动创建服务依赖
        var sourceTracker = new SourceTracker(_trackerLogger);
        var scanner = new MarkdownScanner(_scannerLogger);
        var llmService = CreateRealLLMService(configuration);

        // 3. 运行测试
        var results = new IntegrationTestResults();

        await TestDocumentScanner(scanner, dataPath, outputPath, results);
        var knowledgeSystem = await TestKnowledgeBuilder(sourceTracker, scanner, llmService, dataPath, outputPath, results);
        await TestLearningGenerator(sourceTracker, llmService, knowledgeSystem, outputPath, results);

        // 4. 保存测试结果摘要
        var summaryPath = Path.Combine(outputPath, "test_summary.json");
        var summary = new
        {
            Timestamp = DateTime.UtcNow.ToString("o"),
            DataPath = dataPath,
            OutputPath = outputPath,
            Tests = results
        };
        await File.WriteAllTextAsync(summaryPath, JsonConvert.SerializeObject(summary, Formatting.Indented));

        Console.WriteLine();
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine($"测试完成! 结果已保存到: {outputPath}");
        Console.WriteLine("=".PadRight(60, '='));
    }

    private static ILLMService CreateRealLLMService(IConfiguration configuration)
    {
        var llmConfig = configuration.GetSection("Llm").Get<LlmConfig>();
        if (string.IsNullOrEmpty(llmConfig?.ApiKey) || llmConfig.ApiKey.StartsWith("${env:"))
        {
            throw new InvalidOperationException("请设置 OPENAI_API_KEY 环境变量或配置 appsettings.json");
        }
        var llmLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<LLMService>();
        return new LLMService(llmConfig.ApiKey, llmConfig.BaseUrl, llmConfig.Model, llmLogger);
    }

    private static async Task TestDocumentScanner(
        IScannerService scanner,
        string dataPath,
        string outputPath,
        IntegrationTestResults results)
    {
        Console.WriteLine("[测试 1] 文档扫描服务");
        Console.WriteLine("-".PadRight(40, '-'));

        try
        {
            var documents = await scanner.ScanAsync(dataPath);
            results.ScannerTest = new ScannerTestResult
            {
                Success = true,
                DocumentCount = documents.Count,
                Documents = documents.Select(d => new DocumentInfo
                {
                    DocId = d.DocId,
                    Title = d.Title,
                    Path = d.Path,
                    SectionCount = d.Sections.Count,
                    ContentHash = d.ContentHash
                }).ToList()
            };

            Console.WriteLine($"  扫描到 {documents.Count} 个文档");
            foreach (var doc in documents)
            {
                Console.WriteLine($"    - {doc.Title}");
                Console.WriteLine($"      章节数: {doc.Sections.Count}");
                Console.WriteLine($"      路径: {doc.Path}");
            }

            // 保存详细结果
            var scannerResultPath = Path.Combine(outputPath, "01_scanner_result.json");
            await File.WriteAllTextAsync(scannerResultPath, JsonConvert.SerializeObject(documents, Formatting.Indented));
            Console.WriteLine($"  详细结果: {scannerResultPath}");
        }
        catch (Exception ex)
        {
            results.ScannerTest = new ScannerTestResult { Success = false, ErrorMessage = ex.Message };
            Console.WriteLine($"  失败: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static async Task<KnowledgeSystem?> TestKnowledgeBuilder(
        ISourceTracker sourceTracker,
        IScannerService scanner,
        ILLMService llmService,
        string dataPath,
        string outputPath,
        IntegrationTestResults results)
    {
        Console.WriteLine("[测试 2] 知识体系构建服务");
        Console.WriteLine("-".PadRight(40, '-'));

        KnowledgeSystem? knowledgeSystem = null;

        try
        {
            // 创建 RAG 服务（使用内存存储）
            var ragLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<InMemorySimpleRagService>();
            var ragService = new InMemorySimpleRagService(sourceTracker, ragLogger);

            var builder = new KnowledgeBuilder(scanner, ragService, sourceTracker, llmService, _builderLogger);
            knowledgeSystem = await builder.BuildAsync("test-book", dataPath);

            results.KnowledgeBuilderTest = new KnowledgeBuilderTestResult
            {
                Success = true,
                KnowledgePointCount = knowledgeSystem.KnowledgePoints.Count,
                SnippetCount = knowledgeSystem.Snippets.Count,
                KnowledgePoints = knowledgeSystem.KnowledgePoints.Select(kp => new KnowledgePointInfo
                {
                    KpId = kp.KpId,
                    Title = kp.Title,
                    ChapterPath = kp.ChapterPath,
                    Importance = kp.Importance,
                    SnippetCount = kp.SnippetIds.Count
                }).ToList()
            };

            Console.WriteLine($"  构建了 {knowledgeSystem.KnowledgePoints.Count} 个知识点");
            Console.WriteLine($"  收集了 {knowledgeSystem.Snippets.Count} 个原文片段");
            Console.WriteLine();
            Console.WriteLine("  知识点列表:");
            foreach (var kp in knowledgeSystem.KnowledgePoints.Take(10))
            {
                Console.WriteLine($"    - [{kp.Importance:P0}] {kp.Title}");
            }
            if (knowledgeSystem.KnowledgePoints.Count > 10)
            {
                Console.WriteLine($"    ... 共 {knowledgeSystem.KnowledgePoints.Count} 个");
            }

            // 保存详细结果
            var kbResultPath = Path.Combine(outputPath, "02_knowledge_builder_result.json");
            await File.WriteAllTextAsync(kbResultPath, JsonConvert.SerializeObject(knowledgeSystem, Formatting.Indented));
            Console.WriteLine($"  详细结果: {kbResultPath}");
        }
        catch (Exception ex)
        {
            results.KnowledgeBuilderTest = new KnowledgeBuilderTestResult { Success = false, ErrorMessage = ex.Message };
            Console.WriteLine($"  失败: {ex.Message}");
            return null;
        }

        Console.WriteLine();
        return knowledgeSystem;
    }

    private static async Task TestLearningGenerator(
        ISourceTracker sourceTracker,
        ILLMService llmService,
        KnowledgeSystem? knowledgeSystem,
        string outputPath,
        IntegrationTestResults results)
    {
        Console.WriteLine("[测试 3] 学习内容生成服务");
        Console.WriteLine("-".PadRight(40, '-'));

        try
        {
            // 从知识体系中获取原文片段
            var snippetIds = knowledgeSystem?.Snippets?.Keys.ToList() ?? new List<string>();
            Console.WriteLine($"  调试: 知识体系中有 {snippetIds.Count} 个原文片段");
            if (!snippetIds.Any())
            {
                results.LearningGeneratorTest = new LearningGeneratorTestResult
                {
                    Success = false,
                    ErrorMessage = "没有可用的原文片段，请先运行知识体系构建测试"
                };
                Console.WriteLine("  跳过: 没有原文片段");
                Console.WriteLine();
                return;
            }

            // 选择第一个知识点进行测试
            var testKp = knowledgeSystem!.KnowledgePoints.First();
            testKp.SnippetIds = new List<string> { snippetIds.First() };

            var ragLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<InMemorySimpleRagService>();
            var ragService = new InMemorySimpleRagService(sourceTracker, ragLogger);

            var generator = new LearningGenerator(ragService, sourceTracker, llmService, _generatorLogger);
            var learningPack = await generator.GenerateAsync(testKp);

            results.LearningGeneratorTest = new LearningGeneratorTestResult
            {
                Success = true,
                HasSummary = !string.IsNullOrEmpty(learningPack.Summary?.Definition),
                LevelCount = learningPack.Levels?.Count ?? 0,
                SnippetCount = learningPack.SnippetIds?.Count ?? 0
            };

            Console.WriteLine($"  生成学习内容:");
            Console.WriteLine($"    - 概要定义: {(string.IsNullOrEmpty(learningPack.Summary?.Definition) ? "无" : "有")}");
            Console.WriteLine($"    - 层次内容: {learningPack.Levels?.Count ?? 0} 层");
            Console.WriteLine($"    - 原文片段: {learningPack.SnippetIds?.Count ?? 0} 个");
            Console.WriteLine();

            if (learningPack.Summary != null)
            {
                Console.WriteLine("  概要定义:");
                Console.WriteLine($"    {learningPack.Summary.Definition}");
                Console.WriteLine();
                Console.WriteLine("  核心要点:");
                foreach (var point in learningPack.Summary.KeyPoints)
                {
                    Console.WriteLine($"    - {point}");
                }
            }

            // 保存详细结果
            var lgResultPath = Path.Combine(outputPath, "03_learning_generator_result.json");
            await File.WriteAllTextAsync(lgResultPath, JsonConvert.SerializeObject(learningPack, Formatting.Indented));
            Console.WriteLine($"  详细结果: {lgResultPath}");
        }
        catch (Exception ex)
        {
            results.LearningGeneratorTest = new LearningGeneratorTestResult { Success = false, ErrorMessage = ex.Message };
            Console.WriteLine($"  失败: {ex.Message}");
        }

        Console.WriteLine();
    }
}

// ==================== 配置类 ====================

public class TestConfig
{
    public string DataPath { get; set; } = "tests/datas/hello-agents";
    public string OutputPath { get; set; } = "tests/results";
}

public class LlmConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4";
}

// ==================== 测试结果类 ====================

public class IntegrationTestResults
{
    public ScannerTestResult? ScannerTest { get; set; }
    public KnowledgeBuilderTestResult? KnowledgeBuilderTest { get; set; }
    public LearningGeneratorTestResult? LearningGeneratorTest { get; set; }
}

public class ScannerTestResult
{
    public bool Success { get; set; }
    public int DocumentCount { get; set; }
    public string? ErrorMessage { get; set; }
    public List<DocumentInfo>? Documents { get; set; }
}

public class DocumentInfo
{
    public string DocId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int SectionCount { get; set; }
    public string? ContentHash { get; set; }
}

public class KnowledgeBuilderTestResult
{
    public bool Success { get; set; }
    public int KnowledgePointCount { get; set; }
    public int SnippetCount { get; set; }
    public string? ErrorMessage { get; set; }
    public List<KnowledgePointInfo>? KnowledgePoints { get; set; }
}

public class KnowledgePointInfo
{
    public string KpId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> ChapterPath { get; set; } = new();
    public float Importance { get; set; }
    public int SnippetCount { get; set; }
}

public class LearningGeneratorTestResult
{
    public bool Success { get; set; }
    public bool HasSummary { get; set; }
    public int LevelCount { get; set; }
    public int SnippetCount { get; set; }
    public string? ErrorMessage { get; set; }
}
