using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Api.Controllers;
using ASimpleTutor.Api.Middleware;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Services;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// 加载本地覆盖配置（不提交 git）：
// - appsettings.local.json
// - appsettings.{Environment}.local.json
builder.Configuration
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true, reloadOnChange: true);

// 加载配置
var config = new AppConfig();
builder.Configuration.GetSection("App").Bind(config);

// 注册配置为单例
builder.Services.AddSingleton(config);

// 注册 SectioningOptions 配置
builder.Services.Configure<ASimpleTutor.Core.Configuration.SectioningOptions>(
    builder.Configuration.GetSection("App:Sectioning"));

// 注册日志
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// 注册 Core 服务
builder.Services.AddSingleton<IScannerService, MarkdownScanner>();
builder.Services.AddSingleton<ISourceTracker, SourceTracker>();
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
        logger);
});
builder.Services.AddSingleton<ISimpleRagService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<InMemorySimpleRagService>>();
    var sourceTracker = sp.GetRequiredService<ISourceTracker>();
    return new InMemorySimpleRagService(sourceTracker, logger);
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

    // 获取已激活的书籍目录
    var activeBookRootId = config.ActiveBookRootId;
    if (!string.IsNullOrEmpty(activeBookRootId) && store.Exists(activeBookRootId))
    {
        logger.LogInformation("发现已保存的知识系统，正在加载: {BookRootId}", activeBookRootId);
        var knowledgeSystem = await store.LoadAsync(activeBookRootId);
        if (knowledgeSystem != null)
        {
            AdminController.SetKnowledgeSystem(knowledgeSystem);
            KnowledgePointsController.SetKnowledgeSystem(knowledgeSystem);
            ChaptersController.SetKnowledgeSystem(knowledgeSystem);
            ExercisesController.SetKnowledgeSystem(knowledgeSystem);
            ProgressController.SetKnowledgeSystem(knowledgeSystem);
            logger.LogInformation("知识系统加载完成，共 {Count} 个知识点", knowledgeSystem.KnowledgePoints.Count);
        }
    }
    else if (!string.IsNullOrEmpty(activeBookRootId))
    {
        logger.LogInformation("未找到已保存的知识系统，请触发扫描: {BookRootId}", activeBookRootId);
    }
}

await TryLoadSavedKnowledgeSystemAsync();

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
        "GET /api/v1/books/roots - 获取书籍目录列表",
        "POST /api/v1/books/activate - 激活书籍目录",
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
        "PUT /api/v1/progress/mistakes/{id}/resolve - 解决错题",
        "GET /api/v1/progress/relations - 获取关联知识点"
    }
}));

app.Run();
