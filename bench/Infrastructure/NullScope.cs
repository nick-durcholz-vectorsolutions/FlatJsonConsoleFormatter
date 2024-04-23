namespace Benchmarks.Infrastructure;

internal sealed class NullScope : IDisposable
{
    private NullScope()
    {
    }

    public static NullScope Instance { get; } = new();

    public void Dispose()
    {
    }
}
