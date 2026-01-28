using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Endpoints;

/// <summary>
/// 书籍管理端点
/// </summary>
public static class BookEndpoints
{
    public static void MapBookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/books");

        group.MapGet("/", GetBooks);
        group.MapPost("/{id}/activate", ActivateBook);
    }

    private static IResult GetBooks([FromServices] AppConfig config)
    {
        var books = config.BookRoots
            .Where(b => b.Enabled)
            .OrderBy(b => b.Order)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Path,
                IsActive = b.Id == config.ActiveBookRootId
            });

        return Results.Ok(books);
    }

    private static async Task<IResult> ActivateBook(
        string id,
        [FromServices] AppConfig config,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger logger)
    {
        var book = config.BookRoots.FirstOrDefault(b => b.Id == id);
        if (book == null)
        {
            return Results.NotFound($"书籍目录不存在: {id}");
        }

        if (!Directory.Exists(book.Path))
        {
            return Results.BadRequest($"目录不存在: {book.Path}");
        }

        logger.LogInformation("激活书籍目录: {Id}", id);
        config.ActiveBookRootId = id;

        return Results.Ok(new { Message = $"已激活书籍目录: {book.Name}" });
    }
}
