using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace ASimpleTutor.Api.Middleware;

/// <summary>
/// 统一异常处理中间件
/// </summary>
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // 记录异常信息
        _logger.LogError(ex, "未处理的异常: {Message}", ex.Message);

        // 设置响应状态码
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        // 构建错误响应
            var errorResponse = new ErrorResponse
            {
                Success = false,
                Error = new ErrorDetails
                {
                    Code = "INTERNAL_SERVER_ERROR",
                    Message = "服务器内部错误，请稍后重试",
                    // 生产环境不返回详细错误信息
                    Details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                        ? ex.Message ?? string.Empty
                        : string.Empty
                },
                Timestamp = DateTime.UtcNow
            };

        // 根据异常类型设置不同的错误码和消息
        if (ex is ArgumentNullException || ex is ArgumentException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            errorResponse.Error.Code = "BAD_REQUEST";
            errorResponse.Error.Message = "请求参数错误";
        }
        else if (ex is UnauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            errorResponse.Error.Code = "UNAUTHORIZED";
            errorResponse.Error.Message = "未授权访问";
        }
        else if (ex is FileNotFoundException || ex is DirectoryNotFoundException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            errorResponse.Error.Code = "NOT_FOUND";
            errorResponse.Error.Message = "资源不存在";
        }

        // 序列化错误响应
        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// 错误响应模型
/// </summary>
public class ErrorResponse
{
    public bool Success { get; set; }
    public ErrorDetails Error { get; set; }
    public DateTime Timestamp { get; set; }
    
    public ErrorResponse()
    {
        Error = new ErrorDetails();
    }
}

/// <summary>
/// 错误详情模型
/// </summary>
public class ErrorDetails
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// 异常处理中间件扩展
/// </summary>
public static class ExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlerMiddleware>();
    }
}
