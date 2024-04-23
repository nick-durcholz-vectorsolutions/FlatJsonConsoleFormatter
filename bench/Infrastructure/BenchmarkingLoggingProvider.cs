using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Benchmarks.Infrastructure;

public class BenchmarkingLoggingProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConsoleFormatter _formatter;

    private readonly ConcurrentDictionary<string, BenchmarkingLogger> _loggers
        = new();

    private IExternalScopeProvider _scopeProvider = NullExternalScopeProvider.Instance;

    public BenchmarkingLoggingProvider(IOptions<BenchmarkingLoggingOptions> options,
        IEnumerable<ConsoleFormatter> formatters)
    {
        var formatter = formatters.FirstOrDefault(f => f.Name == options.Value.FormatterName);
        _formatter = formatter ??
                     throw new InvalidOperationException(
                         $"No formatter found with name '{options.Value.FormatterName}'");
    }

    public ILogger CreateLogger(string name) =>
        _loggers.TryGetValue(name, out var logger)
            ? logger
            : _loggers.GetOrAdd(name, new BenchmarkingLogger(name, _formatter, _scopeProvider));

    public void Dispose()
    {
        // no-op
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        Debug.WriteLine($"SetScopeProvider {scopeProvider}");

        _scopeProvider = scopeProvider;

        foreach (var logger in _loggers)
        {
            logger.Value.ScopeProvider = _scopeProvider;
        }
    }
}
