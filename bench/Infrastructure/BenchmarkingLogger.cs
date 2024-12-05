using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Benchmarks.Infrastructure;

public class BenchmarkingLogger : ILogger
{
    private const int Capacity = 1024;

    [ThreadStatic] private static StringWriter? t_stringWriter;

    public BenchmarkingLogger(string name, ConsoleFormatter formatter, IExternalScopeProvider? scopeProvider)
    {
        Name = name;
        Formatter = formatter;
        ScopeProvider = scopeProvider;
    }

    public string Name { get; }

    public ConsoleFormatter Formatter { get; }
    public IExternalScopeProvider? ScopeProvider { get; set; }


    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // Adapted from https://github.com/dotnet/runtime/blob/3cea1eeb6c04ebb7e81d47393b96ad51b66b8d1f/src/libraries/Microsoft.Extensions.Logging.Console/src/ConsoleLogger.cs
        t_stringWriter ??= new StringWriter(new StringBuilder(Capacity));

        var logEntry = new LogEntry<TState>(logLevel, Name, eventId, state, exception, formatter);
        Formatter.Write(in logEntry, ScopeProvider, t_stringWriter);

        var sb = t_stringWriter.GetStringBuilder();

#if DEBUG
        Console.Error.WriteLine(sb);
        Console.Error.Flush();
#endif

        // reset string builder
        sb.Clear();
        if (sb.Capacity > Capacity) sb.Capacity = Capacity;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
        ScopeProvider?.Push(state) ?? NullScope.Instance;
}
