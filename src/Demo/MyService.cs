using Microsoft.Extensions.Logging;

namespace Demo;

public class MyService
{
    private readonly ILogger _logger;

    public MyService(ILogger<MyService> logger)
    {
        this._logger = logger;
    }

    public void Error() => throw new Exception("Something went wrong") { HResult = 9001 };

    public void LogMessages()
    {
        _logger.LogInformation("This is a simple log message with no context");

        var scopeDictionary = new Dictionary<string, object?>
        {
            ["ScopeDictionaryItem1"] = 42,
            ["ScopeDictionaryItem2"] = null,
            ["ScopeDictionaryItem3"] = Guid.NewGuid(),
            ["ScopeDictionaryItem4"] = "ScopeDictionary4 value"
        };
        using (_logger.BeginScope(scopeDictionary))
        using (_logger.BeginScope("This is a scope message with an inline {ContextItemFromScope}", "value"))
        {
            _logger.LogWarning(
                "This is a warning message with a {InlineContextItemFromLogMessage}",
                "InlineContextItemFromLogMessage value");

            try
            {
                Error();
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Critical,
                    new EventId(ex.HResult, "Critical Error"),
                    ex,
                    "The operation failed due to an exception. {InlineContextItemFromLogMessage}",
                    "InlineContextItemFromLogMessage value");
            }
        }
    }
}
