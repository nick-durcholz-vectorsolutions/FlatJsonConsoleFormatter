using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Xunit.Abstractions;

namespace Unit.Infrastructure;

public class FakeLogger : ILogger
{
    private readonly StringWriter _stringWriter = new(new StringBuilder(1024));
    
    public Action<string>? OnLogFormatted;
    public ITestOutputHelper? TestOutputHelper { get; set; }
    
    public FakeLogger(ConsoleFormatter formatter)
    {
        Formatter = formatter;
    }

    public string Name { get; set; } = FakeLoggerBuilder.DefaultLoggerName;
    public ConsoleFormatter Formatter { get; }

    public IExternalScopeProvider? ScopeProvider { get; set; }

    public string? Formatted { get; private set; }


    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var logEntry = new LogEntry<TState>(logLevel, Name, eventId, state, exception, formatter);
        Formatter.Write(in logEntry, ScopeProvider, _stringWriter);
        Formatted = _stringWriter.ToString();
        _stringWriter.GetStringBuilder().Clear();
        TestOutputHelper?.WriteLine(Formatted);
        OnLogFormatted?.Invoke(Formatted);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => ScopeProvider?.Push(state);
}
