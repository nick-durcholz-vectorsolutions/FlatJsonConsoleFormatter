using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks.Config;
using Benchmarks.Infrastructure;

namespace Benchmarks;

internal class Program
{
    private static int Main(string[] args)
    {
        var debuggingConfig = new DebuggingConfig();
        var releaseConfig = new ReleaseConfig();

        IConfig config;

#if DEBUG
        config = debuggingConfig;
#else
        config = releaseConfig;
#endif

        return BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .RunAll(config, args)
            .ToExitCode();
    }
}
