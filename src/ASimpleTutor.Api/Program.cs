using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Api.Controllers;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Services;
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
    var storagePath = config.StoragePath;
    return new KnowledgeSystemStore(logger, storagePath);
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
using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<KnowledgeSystemStore>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // 获取已激活的书籍目录
    var activeBookRootId = config.ActiveBookRootId;
    if (!string.IsNullOrEmpty(activeBookRootId) && store.Exists(activeBookRootId))
    {
        logger.LogInformation("发现已保存的知识系统，正在加载: {BookRootId}", activeBookRootId);
        var knowledgeSystem = store.Load(activeBookRootId);
        if (knowledgeSystem != null)
        {
            AdminController.SetKnowledgeSystem(knowledgeSystem);
            KnowledgePointsController.SetKnowledgeSystem(knowledgeSystem);
            ChaptersController.SetKnowledgeSystem(knowledgeSystem);
            ExercisesController.SetKnowledgeSystem(knowledgeSystem);
            logger.LogInformation("知识系统加载完成，共 {Count} 个知识点", knowledgeSystem.KnowledgePoints.Count);
        }
    }
    else if (!string.IsNullOrEmpty(activeBookRootId))
    {
        logger.LogInformation("未找到已保存的知识系统，请触发扫描: {BookRootId}", activeBookRootId);
    }
}

// 配置中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
        "POST /api/v1/exercises/feedback - 批量提交并获取反馈"
    }
}));

app.Run();
