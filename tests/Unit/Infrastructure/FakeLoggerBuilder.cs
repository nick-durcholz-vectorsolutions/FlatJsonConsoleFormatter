using Benchmarks.Infrastructure;
using FlatJsonConsoleFormatter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit.Abstractions;

namespace Unit.Infrastructure;

public class FakeLoggerBuilder
{
    public const string DefaultLoggerName = "test";
    
    public static FakeLoggerBuilder<FlatJsonConsoleFormatterOptions> FlatJson() => new(optionsMonitor =>
        new FlatJsonConsoleFormatter.FlatJsonConsoleFormatter(optionsMonitor), new []{Builder.DontIncludeScopes});
}

public class FakeLoggerBuilder<TOptions> : FakeLoggerBuilder where TOptions : JsonConsoleFormatterOptions, new()
{
    private readonly List<Action<TOptions>> _configures = new();
    private readonly Func<IOptionsMonitor<TOptions>, ConsoleFormatter> _factory;
    private List<StaticScope>? _scopes;
    private ITestOutputHelper? _testOutputHelper;
    
    public FakeLoggerBuilder(Func<IOptionsMonitor<TOptions>, ConsoleFormatter> factory, IEnumerable<Action<TOptions>> preConfigures)
    {
        _factory = factory;
        _configures.AddRange(preConfigures);
    }

    public FakeLoggerBuilder<TOptions> With(Action<TOptions> configure)
    {
        _configures.Add(configure);
        return this;
    }
    
    public FakeLoggerBuilder<TOptions> WithTestOutputHelper(ITestOutputHelper? testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        return this;
    }

    public FakeLoggerBuilder<TOptions> WithStaticScopes(params StaticScope[] scopes)
    {
        _scopes = scopes.ToList();
        return this;
    }

    public FakeLoggerBuilder<TOptions> AddStaticScope(string? message, string key, object value)
    {
        _scopes ??= new();
        _scopes.Add(new StaticScope(message, new Dictionary<string, object> { [key] = value }));
        return this;
    }

    public FakeLogger Build(out TOptions options, out IOptionsMonitor<TOptions> optionsMonitor)
    {
        options = new TOptions();
        foreach (var configure in _configures)
        {
            configure(options);
        }

        optionsMonitor = Substitute.For<IOptionsMonitor<TOptions>>();
        optionsMonitor.CurrentValue.Returns(options);

        var logger = new FakeLogger(_factory(optionsMonitor));
       
        logger.TestOutputHelper = _testOutputHelper;

        logger.ScopeProvider = _scopes != null ? new StaticExternalScopeProvider(_scopes) : new LoggerExternalScopeProvider();
        if (_scopes != null)
        {
            logger.ScopeProvider = new StaticExternalScopeProvider(_scopes);
        }

        return logger;
    }

    public FakeLogger Build() => Build(out _, out _);
}
