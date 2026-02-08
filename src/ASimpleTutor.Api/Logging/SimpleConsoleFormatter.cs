using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace ASimpleTutor.Api.Logging;

/// <summary>
/// 简洁的日志格式化器，输出格式：[时间] 日志级别: 日志内容
/// </summary>
public class SimpleConsoleFormatter : ConsoleFormatter, IDisposable
{
    private readonly IDisposable? _optionsReloadToken;
    private SimpleConsoleFormatterOptions _options;

    public SimpleConsoleFormatter(IOptionsMonitor<SimpleConsoleFormatterOptions> options) : base("SimpleCustom")
    {
        _options = options.CurrentValue;
        _optionsReloadToken = options.OnChange(updatedOptions => _options = updatedOptions);
    }

    public SimpleConsoleFormatter() : base("SimpleCustom")
    {
        _options = new SimpleConsoleFormatterOptions();
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var timestamp = DateTime.Now.ToString(_options.TimestampFormat ?? "HH:mm:ss");
        var level = logEntry.LogLevel.ToString().ToLower();
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        textWriter.WriteLine($"[{timestamp}] {level}: {message}");
    }

    public void Dispose()
    {
        _optionsReloadToken?.Dispose();
    }
}

public class SimpleConsoleFormatterOptions : ConsoleFormatterOptions
{
}