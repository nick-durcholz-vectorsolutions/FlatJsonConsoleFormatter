using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Benchmarks.Infrastructure;
using Benchmarks.Scenarios;
using Microsoft.Extensions.Logging;

namespace Benchmarks;

public class SmallLog
{
    private ILogger? _flatJsonLogger;
    private ILogger? _flatJsonLoggerIncludeEventId;
    private ILogger? _flatJsonLoggerMergeDuplicateKeys;
    private ILogger? _flatJsonLoggerTruncateCategory;
    private ILogger? _flatJsonLoggerUnsafeRelaxedJsonEscaping;

    private ILogger? _jsonLogger;
    private ILogger? _jsonLoggerUnsafeRelaxedJsonEscaping;

    [GlobalSetup]
    public void Setup()
    {
        _jsonLogger = Builder.CreateJsonLogger();
        _jsonLoggerUnsafeRelaxedJsonEscaping = Builder.CreateJsonLogger(Builder.UnsafeRelaxedJsonEscaping);
        _flatJsonLogger = Builder.CreateFlatJsonLogger();
        _flatJsonLoggerTruncateCategory = Builder.CreateFlatJsonLogger(Builder.TruncateCategory);
        _flatJsonLoggerIncludeEventId = Builder.CreateFlatJsonLogger(Builder.IncludeEventId);
        _flatJsonLoggerMergeDuplicateKeys = Builder.CreateFlatJsonLogger(Builder.MergeDuplKeys);
        _flatJsonLoggerUnsafeRelaxedJsonEscaping = Builder.CreateFlatJsonLogger(Builder.UnsafeRelaxedJsonEscaping);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run(ILogger logger) => AspNetScenario.Small(logger);
    
    
    [Benchmark(Baseline = true)]
    public void Json() => Run(_jsonLogger!);

    [Benchmark]
    public void Json_UnsafeJson() => Run(_jsonLoggerUnsafeRelaxedJsonEscaping!);

    [Benchmark]
    public void FlatJson() => Run(_flatJsonLogger!);

    [Benchmark]
    public void FlatJson_UnsafeJson() => Run(_flatJsonLoggerUnsafeRelaxedJsonEscaping!);

    [Benchmark]
    public void FlatJson_TruncCat() => Run(_flatJsonLoggerTruncateCategory!);

    [Benchmark]
    public void FlatJson_IncludeEventId() => Run(_flatJsonLoggerIncludeEventId!);

    [Benchmark]
    public void FlatJson_MergeDuplKeys() => Run(_flatJsonLoggerMergeDuplicateKeys!);
}
