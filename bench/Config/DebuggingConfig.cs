using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Benchmarks.Config;

public class DebuggingConfig : ManualConfig
{
    public DebuggingConfig()
    {
        AddLogger(ConsoleLogger.Unicode);
        AddJob(Job.Dry
            .WithCustomBuildConfiguration("Debug") // will do `-c Debug everywhere`
            .WithToolchain(
                new InProcessEmitToolchain(
                    TimeSpan.FromHours(1), // 1h should be enough to debug the benchmark
                    true))
        );

        AddColumnProvider(DefaultColumnProviders.Instance);
        AddColumn(LogicalGroupColumn.Default);

        WithOption(ConfigOptions.JoinSummary, true);
    }
}
