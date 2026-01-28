using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Api.Endpoints;
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

// 注册 Core 服务
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// 注册 Core 服务
builder.Services.AddSingleton<IScannerService, MarkdownScanner>();
builder.Services.AddSingleton<ISourceTracker, SourceTracker>();
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

// 配置中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

// 注册端点
app.MapBookEndpoints();
app.MapAdminEndpoints();
app.MapKnowledgeEndpoints();
app.MapLearningEndpoints();
app.MapExerciseEndpoints();

// 健康检查
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

// 首页
app.MapGet("/", () => Results.Ok(new
{
    Name = "ASimpleTutor API",
    Version = "1.0.0",
    Endpoints = new[]
    {
        "GET /health - 健康检查",
        "GET /api/books - 获取书籍目录列表",
        "POST /api/books/{id}/activate - 激活书籍目录",
        "POST /api/admin/build - 构建知识体系",
        "GET /api/knowledge-points - 获取知识点列表",
        "GET /api/knowledge-points/search?q=xxx - 搜索知识点",
        "GET /api/knowledge-points/{id} - 获取知识点详情",
        "GET /api/learning/{kpId} - 获取学习内容",
        "POST /api/exercises/generate - 生成习题",
        "GET /api/exercises/{id} - 获取习题",
        "POST /api/exercises/{id}/submit - 提交答案"
    }
}));

app.Run();
