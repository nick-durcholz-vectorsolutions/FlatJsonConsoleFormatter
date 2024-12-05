using System.Collections.Frozen;
using Microsoft.Extensions.Logging;

namespace Benchmarks.Infrastructure;

public class StaticExternalScopeProvider : IExternalScopeProvider
{
    public StaticExternalScopeProvider(IEnumerable<StaticScope>? scopes)
    {
        Scopes = scopes?.ToFrozenSet() ?? FrozenSet<StaticScope>.Empty;
    }

    public FrozenSet<StaticScope> Scopes { get; }

    public void ForEachScope<TState>(Action<object?, TState> callback, TState state)
    {
        foreach (var scope in Scopes)
        {
            callback(scope, state);
        }
    }

    public IDisposable Push(object? state) => throw new NotImplementedException();
}
