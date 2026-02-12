using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Api.Controllers;
using ASimpleTutor.Api.Interfaces;
using ASimpleTutor.Api.Logging;
using ASimpleTutor.Api.Middleware;
using ASimpleTutor.Api.Services;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Services;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// 配置自定义日志格式化器
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.FormatterName = "SimpleCustom";
});
builder.Logging.AddConsoleFormatter<SimpleConsoleFormatter, SimpleConsoleFormatterOptions>();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// 加载本地覆盖配置（不提交 git）：
// - appsettings.local.json
// - appsettings.{Environment}.local.json
builder.Configuration
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true);

// 加载配置
var config = new AppConfig();
builder.Configuration.GetSection("App").Bind(config);

// 注册配置为单例
builder.Services.AddSingleton(config);

// 注册 SectioningOptions 配置
builder.Services.Configure<ASimpleTutor.Core.Configuration.SectioningOptions>(
    builder.Configuration.GetSection("App:Sectioning"));

// 注册 Core 服务
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IScannerService, MarkdownScanner>();
builder.Services.AddSingleton<KnowledgeSystemStore>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KnowledgeSystemStore>>();
    // 默认使用项目根目录下的 datas 目录
    var dataDirectory = config.StoragePath ?? "datas";
    return new KnowledgeSystemStore(logger, dataDirectory);
});
builder.Services.AddSingleton<ILLMService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<LLMService>>();
    return new LLMService(
        config.Llm.ApiKey,
        config.Llm.BaseUrl,
        config.Llm.Model,
        config.Llm.Concurrency,
        logger);
});
builder.Services.AddSingleton<ITtsService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<TtsService>>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var ss = sp.GetRequiredService<ISettingsService>();

    return new TtsService(config, logger, env, ss);
});

builder.Services.AddSingleton<IKnowledgeBuilder, KnowledgeBuilder>();
builder.Services.AddSingleton<ILearningGenerator, LearningGenerator>();
builder.Services.AddSingleton<IExerciseGenerator, ExerciseService>();
builder.Services.AddSingleton<IExerciseFeedback, ExerciseService>();

// 添加控制器支持
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "ASimpleTutor API",
        Version = "v1",
        Description = "基于书籍 Markdown 文档的智能学习系统服务端"
    });
});

var app = builder.Build();

// 启动时自动加载已保存的知识系统
async Task TryLoadSavedKnowledgeSystemAsync()
{
    using var scope = app.Services.CreateScope();
    var store = scope.ServiceProvider.GetRequiredService<KnowledgeSystemStore>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // 获取已激活的书籍中心
    var activeBookHubId = config.ActiveBookHubId;
    if (!string.IsNullOrEmpty(activeBookHubId))
    {
        // 检查存储文件的完整性
        if (store.CheckStorageIntegrity(activeBookHubId))
        {
            logger.LogInformation("发现已保存的知识系统，正在加载: {BookHubId}", activeBookHubId);
            var (knowledgeSystem, documents) = await store.LoadAsync(activeBookHubId);
            if (knowledgeSystem != null)
            {
                AdminController.SetKnowledgeSystem(knowledgeSystem);
                KnowledgePointsController.SetKnowledgeSystem(knowledgeSystem);
                ChaptersController.SetKnowledgeSystem(knowledgeSystem);
                ExercisesController.SetKnowledgeSystem(knowledgeSystem);
                ProgressController.SetKnowledgeSystem(knowledgeSystem);
                logger.LogInformation("知识系统加载完成，共 {Count} 个知识点，{DocCount} 个文档", 
                    knowledgeSystem.KnowledgePoints.Count, 
                    documents?.Count ?? 0);
            }
        }
        else
        {
            logger.LogInformation("未找到完整的已保存知识系统，请触发扫描: {BookHubId}", activeBookHubId);
        }
    }
}

// 检查所有已保存的知识点扫描结果状态
async Task CheckAllSavedKnowledgeSystemsAsync()
{
    using var scope = app.Services.CreateScope();
    var store = scope.ServiceProvider.GetRequiredService<KnowledgeSystemStore>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("开始检查所有已保存的知识点扫描结果状态...");

    // 获取所有已保存的书籍中心 ID
    var savedBookHubIds = store.GetAllSavedBookHubIds();

    if (savedBookHubIds.Count == 0)
    {
        logger.LogInformation("未发现任何已保存的知识点扫描结果");
        Console.WriteLine("[启动检查] 未发现任何已保存的知识点扫描结果");
        return;
    }

    logger.LogInformation("发现 {Count} 个已保存的知识点扫描结果", savedBookHubIds.Count);
    Console.WriteLine($"[启动检查] 发现 {savedBookHubIds.Count} 个已保存的知识点扫描结果");

    foreach (var bookHubId in savedBookHubIds)
    {
        // 检查存储文件的完整性
        if (store.CheckStorageIntegrity(bookHubId))
        {
            // 加载知识系统以获取详细信息
            var (knowledgeSystem, documents) = await store.LoadAsync(bookHubId);
            if (knowledgeSystem != null)
            {
                var kpCount = knowledgeSystem.KnowledgePoints.Count;
                var docCount = documents?.Count ?? 0;

                logger.LogInformation("知识点扫描结果状态: 书籍中心={BookHubId}, 知识点数量={KpCount}, 文档数量={DocCount}, 状态=完整", 
                    bookHubId, kpCount, docCount);
                Console.WriteLine($"[启动检查] 书籍中心={bookHubId}, 知识点数量={kpCount}, 文档数量={docCount}, 状态=完整");
            }
            else
            {
                logger.LogWarning("知识点扫描结果状态: 书籍中心={BookHubId}, 状态=损坏或无法加载", bookHubId);
                Console.WriteLine($"[启动检查] 书籍中心={bookHubId}, 状态=损坏或无法加载");
            }
        }
        else
        {
            logger.LogWarning("知识点扫描结果状态: 书籍中心={BookHubId}, 状态=不完整", bookHubId);
            Console.WriteLine($"[启动检查] 书籍中心={bookHubId}, 状态=不完整");
        }
    }

    logger.LogInformation("所有知识点扫描结果状态检查完成");
    Console.WriteLine("[启动检查] 所有知识点扫描结果状态检查完成");
}

await TryLoadSavedKnowledgeSystemAsync();
await CheckAllSavedKnowledgeSystemsAsync();

// 启动时检查 LLM 模型配置状态
async Task CheckLlmModelStatusAsync()
{
    using var scope = app.Services.CreateScope();
    var llmService = scope.ServiceProvider.GetRequiredService<ILLMService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var config = scope.ServiceProvider.GetRequiredService<AppConfig>();

    try
    {
        logger.LogInformation("开始检查 LLM 模型配置状态...");
        
        // 发送检查请求
        var systemPrompt = "你是一个AI助手，请简要回答用户的问题。";
        var userMessage = "你好，你是那个厂商的模型？";
        
        var response = await llmService.ChatAsync(systemPrompt, userMessage);
        
        logger.LogInformation("LLM 模型配置状态检查成功");
        logger.LogInformation("LLM 响应: {Response}", response);
        logger.LogInformation("当前配置的 LLM 模型: {Model}, 端点: {BaseUrl}", 
            config.Llm.Model, config.Llm.BaseUrl);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "LLM 模型配置状态检查失败，请检查配置");
        logger.LogInformation("当前配置的 LLM 模型: {Model}, 端点: {BaseUrl}", 
            config.Llm.Model, config.Llm.BaseUrl);
    }
}

// 异步执行 LLM 模型状态检查
_ = CheckLlmModelStatusAsync();

// 配置中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 使用异常处理中间件
app.UseExceptionHandlerMiddleware();

// 启用静态文件服务，用于提供生成的音频文件等
app.UseStaticFiles();

app.UseRouting();
app.MapControllers();

// 健康检查
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

// 首页
app.MapGet("/", () => Results.Ok(new
{
    name = "ASimpleTutor API",
    version = "1.0.0",
    endpoints = new[]
    {
        "GET /health - 健康检查",
        "GET /api/v1/settings/llm - 获取 LLM 配置",
        "PUT /api/v1/settings/llm - 更新 LLM 配置",
        "POST /api/v1/settings/llm/test - 测试 LLM 连接",
        "GET /api/v1/books/hubs - 获取书籍中心列表",
        "POST /api/v1/books/activate - 激活书籍中心",
        "POST /api/v1/books/scan - 触发扫描",
        "GET /api/v1/chapters - 获取章节树",
        "GET /api/v1/chapters/search - 搜索章节",
        "GET /api/v1/chapters/knowledge-points - 获取章节知识点",
        "GET /api/v1/knowledge-points/overview - 获取精要速览",
        "GET /api/v1/knowledge-points/source-content - 获取原文对照",
        "GET /api/v1/knowledge-points/detailed-content - 获取层次展开内容",
        "GET /api/v1/knowledge-points/exercises/status - 检查习题状态",
        "GET /api/v1/knowledge-points/exercises - 获取习题列表",
        "POST /api/v1/exercises/submit - 提交答案",
        "POST /api/v1/exercises/feedback - 批量提交并获取反馈",
        "GET /api/v1/progress - 获取知识点学习进度",
        "GET /api/v1/progress/overview - 获取进度概览",
        "PUT /api/v1/progress - 更新学习进度",
        "GET /api/v1/progress/mistakes - 获取错题本",
                "PUT /api/v1/progress/mistakes/{id}/resolve - 解决错题"
    }
}));

app.Run();
