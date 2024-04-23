using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace Benchmarks.Config;

public class ReleaseConfig : ManualConfig
{
    public ReleaseConfig()
    {
        AddDefaults();
        AddJob(Job.Default
                .WithWarmupCount(1) // 1 warmup is enough for our purpose
                .WithMinIterationCount(15)
                .WithMaxIterationCount(100) // we don't want to run more that 100 iterations
                .DontEnforcePowerPlan() // make sure BDN does not try to enforce High Performance power plan on Windows
                .WithGcServer(true)
                .WithUnrollFactor(
                    1024 * 8) // we need to reach 100ms of execution time, unrolling is the easiest way to do it
        );

        AddColumn(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max,
            CategoriesColumn.Default);

        AddDiagnoser(MemoryDiagnoser.Default);

        AddExporter(MarkdownExporter.GitHub, JsonExporter.Full, HtmlExporter.Default);

        AddLogger(ConsoleLogger.Unicode);

        WithOption(ConfigOptions.StopOnFirstError, true);

        SummaryStyle =
            SummaryStyle.Default
                .WithMaxParameterColumnWidth(36); // the default is 20 and trims some benchmark results too aggressively
    }

    private void AddDefaults()
    {
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray());
        AddValidator(DefaultConfig.Instance.GetValidators().ToArray());
    }
}
