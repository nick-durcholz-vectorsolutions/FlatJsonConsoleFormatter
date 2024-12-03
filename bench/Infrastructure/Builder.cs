using System.Text.Encodings.Web;
using Benchmarks.Scenarios;
using FlatJsonConsoleFormatter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Benchmarks.Infrastructure;

public static class Builder
{
    public const string FlatJsonFormatterName = "flat-json";
    public const string JsonFormatterName = ConsoleFormatterNames.Json;
    public static readonly Action<ConsoleFormatterOptions> TimestampFormatO = o => o.TimestampFormat = "O";
    public static readonly Action<ConsoleFormatterOptions> UseUtcTimestamp = o => o.UseUtcTimestamp = false;
    public static readonly Action<ConsoleFormatterOptions> IncludeScopes = o => o.IncludeScopes = true;
    public static readonly Action<ConsoleFormatterOptions> DontIncludeScopes = o => o.IncludeScopes = false;

    public static readonly Action<JsonConsoleFormatterOptions> UnsafeRelaxedJsonEscaping = o =>
        o.JsonWriterOptions = o.JsonWriterOptions with { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static readonly Action<JsonConsoleFormatterOptions> Indented = o =>
        o.JsonWriterOptions = o.JsonWriterOptions with { Indented = true };

    public static readonly Action<FlatJsonConsoleFormatterOptions> TruncateCategory = o => o.TruncateCategory = true;
    public static readonly Action<FlatJsonConsoleFormatterOptions> IncludeEventId = o => o.IncludeEventId = true;

    public static readonly Action<FlatJsonConsoleFormatterOptions>
        MergeDuplKeys = o => o.MergeDuplicateKeys = true;

    public static readonly Action<JsonConsoleFormatterOptions>[] Defaults =
    {
        DontIncludeScopes, // FlatJson defaults to true
        TimestampFormatO,
#if DEBUG
        Indented,
#endif
    };


    public static ILogger CreateJsonLogger(params Action<JsonConsoleFormatterOptions>[] configures) =>
        CreateLogger(JsonFormatterName, lb =>
        {
            lb.AddJsonConsole(o =>
            {
                foreach (var configure in Defaults.Concat(configures))
                {
                    configure(o);
                }
            });
        });

    public static ILogger CreateFlatJsonLogger(params Action<FlatJsonConsoleFormatterOptions>[] configures) =>
        CreateLogger(FlatJsonFormatterName, lb =>
        {
            lb.AddFlatJsonConsole(o =>
            {
                foreach (var configure in Defaults.Concat(configures))
                {
                    configure(o);
                }
            });
        });

    private static ILogger CreateLogger(string formatterName, Action<ILoggingBuilder> addFormatter)
    {
        var services = new ServiceCollection();
        services.AddLogging(lb =>
        {
            lb.SetMinimumLevel(LogLevel.Trace);
            addFormatter(lb);
            lb.ClearProviders();
        });

        services.AddSingleton<ILoggerProvider, BenchmarkingLoggingProvider>();
        services.AddOptions<BenchmarkingLoggingOptions>().Configure(o => o.FormatterName = formatterName);

        services.RemoveAll<IExternalScopeProvider>();
        services.AddSingleton<IExternalScopeProvider>(AspNetScenario.ScopeProvider);

        return services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
            .CreateLogger("System.Net.Http.HttpClient.SamlAuthHttpClient.LogicalHandler");
    }
}
