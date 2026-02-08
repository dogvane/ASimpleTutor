using ASimpleTutor.Core.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ASimpleTutor.IntegrationTests;

/// <summary>
/// 控制台集成测试入口程序
/// 测试文档扫描和学习内容生成模块
/// </summary>
public class Program
{
    private static ILogger<MarkdownScanner> _scannerLogger = null!;
    private static ILogger<LearningGenerator> _generatorLogger = null!;
    private static ILogger<KnowledgeBuilder> _builderLogger = null!;
    private static ILogger<ExerciseService> _exerciseLogger = null!;

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
        _generatorLogger = loggerFactory.CreateLogger<LearningGenerator>();
        _builderLogger = loggerFactory.CreateLogger<KnowledgeBuilder>();
        _exerciseLogger = loggerFactory.CreateLogger<ExerciseService>();

        // 2. 手动创建服务依赖
        // 读取 SectioningOptions 配置
        var sectioningOptions = new SectioningOptions();
        configuration.GetSection("Sectioning").Bind(sectioningOptions);
        var sectioningOptionsWrapper = Options.Create(sectioningOptions);
        
        var scanner = new MarkdownScanner(_scannerLogger, sectioningOptionsWrapper);
        var llmService = CreateRealLLMService(configuration);

        // 3. 运行测试
        var results = new IntegrationTestResults();

        await TestDocumentScanner(scanner, dataPath, outputPath, results);
        var (knowledgeSystem, documents) = await TestKnowledgeBuilder(scanner, llmService, dataPath, outputPath, results);
        await TestLearningGenerator(llmService, knowledgeSystem, documents, outputPath, results);
        await TestExerciseGenerator(llmService, knowledgeSystem, outputPath, results);

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

            // 输出扫描总结信息
            var allSections = new List<Section>();
            foreach (var doc in documents)
            {
                CollectAllSections(doc.Sections, allSections);
            }
            
            var leafSections = allSections.Where(s => s.SubSections.Count == 0).ToList();
            var totalSections = allSections.Count;
            var leafSectionCount = leafSections.Count;
            
            // 原始字符统计
            var totalOriginalCharacters = allSections.Sum(s => s.OriginalLength);
            var avgOriginalCharactersPerSection = totalSections > 0 ? totalOriginalCharacters / totalSections : 0;
            
            // 有效字符统计
            var totalEffectiveCharacters = allSections.Sum(s => s.EffectiveLength);
            var avgEffectiveCharactersPerSection = totalSections > 0 ? totalEffectiveCharacters / totalSections : 0;
            
            // 过滤字符统计
            var totalFilteredCharacters = allSections.Sum(s => s.FilteredLength);
            var avgFilteredCharactersPerSection = totalSections > 0 ? totalFilteredCharacters / totalSections : 0;
            
            // 最大最小值统计
            var maxOriginalCharacters = allSections.Count > 0 ? allSections.Max(s => s.OriginalLength) : 0;
            var minOriginalCharacters = allSections.Count > 0 ? allSections.Min(s => s.OriginalLength) : 0;
            var maxEffectiveCharacters = allSections.Count > 0 ? allSections.Max(s => s.EffectiveLength) : 0;
            var minEffectiveCharacters = allSections.Count > 0 ? allSections.Min(s => s.EffectiveLength) : 0;
            
            Console.WriteLine();
            Console.WriteLine("  [扫描总结]");
            Console.WriteLine($"    总章节数: {totalSections}");
            Console.WriteLine($"    叶子章节数: {leafSectionCount}");
            Console.WriteLine();
            Console.WriteLine("    [字符统计]");
            Console.WriteLine($"      原始字符总数: {totalOriginalCharacters}");
            Console.WriteLine($"      有效字符总数: {totalEffectiveCharacters}");
            Console.WriteLine($"      过滤字符总数: {totalFilteredCharacters}");
            Console.WriteLine($"      过滤比例: {(totalOriginalCharacters > 0 ? (totalFilteredCharacters * 100.0 / totalOriginalCharacters).ToString("F2") : "0.00")}%");
            Console.WriteLine();
            Console.WriteLine("    [每章平均字符数]");
            Console.WriteLine($"      原始字符: {avgOriginalCharactersPerSection}");
            Console.WriteLine($"      有效字符: {avgEffectiveCharactersPerSection}");
            Console.WriteLine($"      过滤字符: {avgFilteredCharactersPerSection}");
            Console.WriteLine();
            Console.WriteLine("    [章节字符范围]");
            Console.WriteLine($"      原始字符: {minOriginalCharacters} - {maxOriginalCharacters}");
            Console.WriteLine($"      有效字符: {minEffectiveCharacters} - {maxEffectiveCharacters}");

            // 输出章节树状结构
            Console.WriteLine();
            Console.WriteLine("  [章节结构]");
            foreach (var doc in documents)
            {
                Console.WriteLine($"  └─ [{doc.Title}] ({doc.Sections.Count} 章节总览)");
                PrintSectionTree(doc.Sections, "    ");
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

    private static async Task<(KnowledgeSystem? KnowledgeSystem, List<Document>? Documents)> TestKnowledgeBuilder(
        IScannerService scanner,
        ILLMService llmService,
        string dataPath,
        string outputPath,
        IntegrationTestResults results)
    {
        Console.WriteLine("[测试 2] 知识体系构建服务");
        Console.WriteLine("-".PadRight(40, '-'));

        KnowledgeSystem? knowledgeSystem = null;
        List<Document>? documents = null;

        try
        {
            var builder = new KnowledgeBuilder(scanner, llmService, _builderLogger);
            var (ks, docs) = await builder.BuildAsync("test-book", dataPath);
            knowledgeSystem = ks;
            documents = docs;

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
            return (null, null);
        }

        Console.WriteLine();
        return (knowledgeSystem, documents);
    }

    private static async Task TestLearningGenerator(
        ILLMService llmService,
        KnowledgeSystem? knowledgeSystem,
        List<Document>? documents,
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

            // 创建知识体系存储服务
            var storeLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<KnowledgeSystemStore>();
            var store = new KnowledgeSystemStore(storeLogger, outputPath);
            await store.SaveAsync(knowledgeSystem!, documents, CancellationToken.None);

            // 创建学习内容生成服务
            var generator = new LearningGenerator(store, llmService, _generatorLogger);
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

    private static async Task TestExerciseGenerator(
        ILLMService llmService,
        KnowledgeSystem? knowledgeSystem,
        string outputPath,
        IntegrationTestResults results)
    {
        Console.WriteLine("[测试 4] 习题生成服务");
        Console.WriteLine("-".PadRight(40, '-'));

        try
        {
            // 检查知识体系是否有效
            if (knowledgeSystem == null || !knowledgeSystem.KnowledgePoints.Any())
            {
                results.ExerciseGeneratorTest = new ExerciseGeneratorTestResult
                {
                    Success = false,
                    ErrorMessage = "没有可用的知识点，请先运行知识体系构建测试"
                };
                Console.WriteLine("  跳过: 没有知识点");
                Console.WriteLine();
                return;
            }

            // 检查原文片段
            var snippetIds = knowledgeSystem.Snippets?.Keys.ToList() ?? new List<string>();
            if (!snippetIds.Any())
            {
                results.ExerciseGeneratorTest = new ExerciseGeneratorTestResult
                {
                    Success = false,
                    ErrorMessage = "没有可用的原文片段"
                };
                Console.WriteLine("  跳过: 没有原文片段");
                Console.WriteLine();
                return;
            }

            // 创建知识体系存储服务
            var storeLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<KnowledgeSystemStore>();
            var store = new KnowledgeSystemStore(storeLogger, outputPath);
            await store.SaveAsync(knowledgeSystem!, null, CancellationToken.None);

            // 创建习题服务
            var exerciseService = new ExerciseService(llmService, store, _exerciseLogger);

            // 选择一个知识点进行测试
            var testKp = knowledgeSystem.KnowledgePoints
                .OrderByDescending(kp => kp.Importance)
                .First();

            // 确保知识点有原文片段
            if (!testKp.SnippetIds.Any())
            {
                testKp.SnippetIds = new List<string> { snippetIds.First() };
            }

            Console.WriteLine($"  测试知识点: {testKp.Title}");
            Console.WriteLine($"  原文片段数: {testKp.SnippetIds.Count}");

            // 生成 3 道习题
            var exercises = await exerciseService.GenerateAsync(testKp, count: 3);

            results.ExerciseGeneratorTest = new ExerciseGeneratorTestResult
            {
                Success = true,
                ExerciseCount = exercises.Count,
                ChoiceCount = exercises.Count(e => e.Type == ExerciseType.SingleChoice),
                MultiChoiceCount = exercises.Count(e => e.Type == ExerciseType.MultiChoice),
                TrueFalseCount = exercises.Count(e => e.Type == ExerciseType.TrueFalse),
                ShortAnswerCount = exercises.Count(e => e.Type == ExerciseType.ShortAnswer),
                Exercises = exercises.Select(e => new ExerciseInfo
                {
                    ExerciseId = e.ExerciseId,
                    Type = e.Type.ToString(),
                    Question = e.Question.Length > 50 ? e.Question.Substring(0, 50) + "..." : e.Question,
                    CorrectAnswer = e.CorrectAnswer.Length > 30 ? e.CorrectAnswer.Substring(0, 30) + "..." : e.CorrectAnswer,
                    OptionCount = e.Options.Count,
                    KeyPointCount = e.KeyPoints.Count,
                    EvidenceSnippetCount = e.EvidenceSnippetIds.Count
                }).ToList()
            };

            Console.WriteLine($"  生成 {exercises.Count} 道习题:");
            Console.WriteLine($"    - 单选题: {results.ExerciseGeneratorTest.ChoiceCount} 题");
            Console.WriteLine($"    - 多选题: {results.ExerciseGeneratorTest.MultiChoiceCount} 题");
            Console.WriteLine($"    - 判断题: {results.ExerciseGeneratorTest.TrueFalseCount} 题");
            Console.WriteLine($"    - 简答题: {results.ExerciseGeneratorTest.ShortAnswerCount} 题");
            Console.WriteLine();

            // 显示每道习题
            foreach (var (exercise, index) in exercises.Select((e, i) => (e, i + 1)))
            {
                Console.WriteLine($"  习题 {index}: [{exercise.Type}]");
                Console.WriteLine($"    问题: {exercise.Question}");
                Console.WriteLine($"    答案: {exercise.CorrectAnswer}");

                if (exercise.Type == ExerciseType.SingleChoice && exercise.Options.Any())
                {
                    Console.WriteLine($"    选项: {string.Join(", ", exercise.Options)}");
                }

                if (exercise.KeyPoints.Any())
                {
                    Console.WriteLine($"    考查要点: {string.Join(", ", exercise.KeyPoints.Take(3))}");
                }
                Console.WriteLine();
            }

            // 如果有习题，进行题测试判
            if (exercises.Any())
            {
                Console.WriteLine("  [判题测试]");
                Console.WriteLine("  -".PadRight(30, '-'));

                var choiceExercise = exercises.FirstOrDefault(e => e.Type == ExerciseType.SingleChoice);
                if (choiceExercise != null)
                {
                    // 测试正确答案
                    var correctFeedback = await exerciseService.JudgeAsync(choiceExercise, choiceExercise.CorrectAnswer);
                    Console.WriteLine($"  选择题 - 正确答案: {(correctFeedback.IsCorrect == true ? "正确" : "错误")}");

                    // 测试错误答案
                    var wrongOption = choiceExercise.Options.First(o => o != choiceExercise.CorrectAnswer);
                    var wrongFeedback = await exerciseService.JudgeAsync(choiceExercise, wrongOption);
                    Console.WriteLine($"  选择题 - 错误答案: {(wrongFeedback.IsCorrect == false ? "正确判定" : "判定错误")}");
                    Console.WriteLine($"    参考答案: {wrongFeedback.ReferenceAnswer}");
                }
                else
                {
                    // 测试填空题或简答题
                    var otherExercise = exercises.First();
                    var feedback = await exerciseService.JudgeAsync(otherExercise, "测试答案");
                    Console.WriteLine($"  {otherExercise.Type} - 判题测试完成");
                    Console.WriteLine($"    参考答案: {feedback.ReferenceAnswer}");
                }
                Console.WriteLine();
            }

            // 保存详细结果
            var exResultPath = Path.Combine(outputPath, "04_exercise_generator_result.json");
            await File.WriteAllTextAsync(exResultPath, JsonConvert.SerializeObject(exercises, Formatting.Indented));
            Console.WriteLine($"  详细结果: {exResultPath}");
        }
        catch (Exception ex)
        {
            results.ExerciseGeneratorTest = new ExerciseGeneratorTestResult { Success = false, ErrorMessage = ex.Message };
            Console.WriteLine($"  失败: {ex.Message}");
            Console.WriteLine();
        }
    }

    // ==================== 辅助方法 ====================

    private static void CollectAllSections(List<Section> sections, List<Section> allSections)
    {
        foreach (var section in sections)
        {
            allSections.Add(section);
            CollectAllSections(section.SubSections, allSections);
        }
    }

    private static int CountAllSections(List<Section> sections)
    {
        var count = sections.Count;
        foreach (var section in sections)
        {
            count += CountAllSections(section.SubSections);
        }
        return count;
    }

    // CountAllCharacters 方法已不再使用，因为 Paragraphs 属性已被删除
    // private static int CountAllCharacters(List<Section> sections)
    // {
    //     var count = 0;
    //     foreach (var section in sections)
    //     {
    //         count += section.Paragraphs.Sum(p => p.Content.Length);
    //         count += CountAllCharacters(section.SubSections);
    //     }
    //     return count;
    // }

    private static void PrintSectionTree(List<Section> sections, string prefix = "")
    {
        for (int i = 0; i < sections.Count; i++)
        {
            var section = sections[i];
            var isLast = i == sections.Count - 1;
            var connector = isLast ? "└─ " : "├─ ";
            var childPrefix = isLast ? "    " : "│  ";
            
            var originalChars = section.OriginalLength;
            var effectiveChars = section.EffectiveLength;
            var filteredChars = section.FilteredLength;
            var subSectionCount = section.SubSections.Count;
            var title = section.HeadingPath.LastOrDefault() ?? "未知章节";
            
            Console.WriteLine($"{prefix}{connector}[{title}]");
            Console.WriteLine($"{prefix}    原始: {originalChars} | 有效: {effectiveChars} | 过滤: {filteredChars} | 子章节: {subSectionCount}");
            
            if (section.SubSections.Count > 0)
            {
                PrintSectionTree(section.SubSections, prefix + childPrefix);
            }
        }
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
    public ExerciseGeneratorTestResult? ExerciseGeneratorTest { get; set; }
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

// ==================== 习题生成测试结果类 ====================

public class ExerciseGeneratorTestResult
{
    public bool Success { get; set; }
    public int ExerciseCount { get; set; }
    public int ChoiceCount { get; set; }
    public int MultiChoiceCount { get; set; }
    public int TrueFalseCount { get; set; }
    public int ShortAnswerCount { get; set; }
    public List<ExerciseInfo>? Exercises { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ExerciseInfo
{
    public string ExerciseId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public int OptionCount { get; set; }
    public int KeyPointCount { get; set; }
    public int EvidenceSnippetCount { get; set; }
}

// ==================== 辅助方法 ====================
// 这些方法需要在 Program 类内部

