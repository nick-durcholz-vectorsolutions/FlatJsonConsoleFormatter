using System.Collections;
using System.Collections.Frozen;

namespace Benchmarks.Infrastructure;

public class StaticScope : IEnumerable<KeyValuePair<string, object>>
{
    public StaticScope(string? message, Dictionary<string, object>? scopeItems)
    {
        Message = message ?? string.Empty;
        Properties = scopeItems?.ToFrozenDictionary() ?? FrozenDictionary<string, object>.Empty;
    }

    public string Message { get; }
    public FrozenDictionary<string, object> Properties { get; }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Properties.GetEnumerator();


    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => Message;
}
