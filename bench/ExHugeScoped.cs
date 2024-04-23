using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Benchmarks.Infrastructure;
using Benchmarks.Scenarios;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Benchmarks;

[BenchmarkCategory(Categories.WithScope)]
public class ExHugeScoped
{
    private ILogger? _flatJsonLogger;
    private ILogger? _flatJsonLoggerMergeDuplicateKeys;

    private ILogger? _jsonLogger;

    [GlobalSetup]
    public void Setup()
    {
        Action<JsonConsoleFormatterOptions>[] defaults = { Builder.IncludeScopes, Builder.UnsafeRelaxedJsonEscaping };
        _jsonLogger = Builder.CreateJsonLogger(defaults);
        _flatJsonLogger = Builder.CreateFlatJsonLogger(defaults);
        _flatJsonLoggerMergeDuplicateKeys = Builder.CreateFlatJsonLogger(
            defaults.Append(Builder.MergeDuplKeys).ToArray());
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run(ILogger logger) => AspNetScenario.ExHuge(logger);

    [Benchmark(Baseline = true)]
    public void Json() => Run(_jsonLogger!);

    [Benchmark]
    public void FlatJson() => Run(_flatJsonLogger!);

    [Benchmark]
    public void FlatJson_MergeDuplKeys() => Run(_flatJsonLoggerMergeDuplicateKeys!);
}
