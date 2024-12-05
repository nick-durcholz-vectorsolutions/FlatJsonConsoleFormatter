using System.Diagnostics.CodeAnalysis;

namespace Benchmarks.Scenarios;

public static class Exceptions
{
    public static Exception HugeAggregateException = CreateHugeAggregateException();

    private static Exception CreateHugeAggregateException()
    {
        Exception? socketException = null;
        Exception? uriException = null;
        try
        {
            ThrowSocketException();
        }
        catch (Exception e)
        {
            socketException = e;
        }

        try
        {
            ThrowWrappedUriException();
        }
        catch (Exception e)
        {
            uriException = e;
        }

        return new AggregateException(uriException, socketException);
    }


    [DoesNotReturn]
    private static void ThrowWrappedUriException()
    {
        try
        {
            _ = new Uri($"http://some.server.that.is.example.local:{int.MaxValue}");
            throw new Exception("This exception is unexpected, the lines above should not thrown");
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Some text {e.Message}", e);
        }
    }

    [DoesNotReturn]
    private static void ThrowSocketException()
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:9975"), Timeout = TimeSpan.FromSeconds(1)
        };
        _ = httpClient.GetAsync("/should-not-exist").GetAwaiter().GetResult();
        throw new Exception("This exception is unexpected, the lines above should not thrown");
    }
}
