using System.Text.Encodings.Web;
using System.Text.Json;
using FlatJsonConsoleFormatter;
using FlatFormatter = FlatJsonConsoleFormatter.FlatJsonConsoleFormatter;
using FluentAssertions;
using FluentAssertions.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Unit.Infrastructure;
using Xunit.Abstractions;

namespace Unit;

public class FlatJsonFormatterTests : FormatterTestsBase<FlatFormatter, FlatJsonConsoleFormatterOptions>
{
    public FlatJsonFormatterTests(ITestOutputHelper testOutputHelper) : base(FakeLoggerBuilder.FlatJson(),
        testOutputHelper)
    {
    }

    [Theory(Skip = "Test fails, not clear why")]
    [MemberData(nameof(LogLevels))]
    public void Log_LogsCorrectTimestamp(LogLevel logLevel)
    {
        // Arrange
        var logger = LoggerBuilder
            .With(o =>
            {
                o.TimestampFormat = "yyyy-MM-ddTHH:mm:sszz ";
                o.UseUtcTimestamp = false;
                o.JsonWriterOptions = new JsonWriterOptions()
                {
                    // otherwise escapes for timezone formatting from + to \u002b
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, Indented = true
                };
            })
            .Build();
        var ex = new Exception("Exception message" + Environment.NewLine + "with a second line");

        // Act
        logger.Log(logLevel, 0, _state, ex, _defaultFormatter);

        // Assert
        logger.Formatted.Should().BeValidJson() //
            .Subject.Should().HaveElement("Timestamp").Which. //
            Should().MatchRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}");
    }
}

public abstract class FormatterTestsBase<TFormatter, TFormatterOptions>
    where TFormatter : ConsoleFormatter
    where TFormatterOptions : JsonConsoleFormatterOptions, new()
{
    public ITestOutputHelper TestOutputHelper { get; }
    public FakeLoggerBuilder<TFormatterOptions> LoggerBuilder { get; }


    protected const string _loggerName = FakeLoggerBuilder.DefaultLoggerName;
    protected const string _state = "This is a test, and {curly braces} are just fine!";
    protected readonly Func<object, Exception, string> _defaultFormatter = (state, exception) => state.ToString();

    public FormatterTestsBase(FakeLoggerBuilder<TFormatterOptions> fakeLoggerBuilder,
        ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        LoggerBuilder = fakeLoggerBuilder.WithTestOutputHelper(testOutputHelper);
    }

    [Fact]
    public void HelloWorld()
    {
        // Arrange
        var logger = LoggerBuilder.Build();

        // Act
        logger.LogInformation("Hello, world!");

        // Assert
        logger.Formatted.Should().Contain("Hello, world!");
        logger.Formatted.Should().BeValidJson();
    }

    [Theory]
    [InlineData("Hello, world!")]
    [InlineData("{Message}")]
    public void SimpleStringMessage_ValidJson(string message)
    {
        // Arrange
        var logger = LoggerBuilder.Build();

        // Act
        logger.LogInformation(message);

        // Assert
        logger.Formatted.Should().BeValidJson();
    }

    #region ScopeTests

    [Fact]
    public void EnabledScope_ContainsScopeProperties()
    {
        // Arrange
        var logger = LoggerBuilder
            .With(o => o.IncludeScopes = true)
            .AddStaticScope(null, "Key", "Value") //
            .Build();

        // Act
        logger.LogInformation("Hello, world!");

        // Assert
        logger.Formatted.Should().BeValidJson() //
            .Subject.Should().HaveElement("Key").Which.Should().HaveValue("Value");
    }

    [Fact]
    public void ScopeNotEnabled_DoesNotContainScopeProperties()
    {
        // Arrange
        var logger = LoggerBuilder
            .AddStaticScope(null, "Key", "Value")
            .Build();

        // Act
        logger.LogInformation("Hello, world!");

        // Assert
        logger.Formatted.Should().BeValidJson() //
            .Subject.Should().NotHaveElement("Key");
    }

    #endregion
    
    #region runtime/ConsoleFormatterTests

    [Theory]
    [MemberData(nameof(LogLevels))]
    public void NoMessageOrException_Noop(LogLevel level)
    {
        // Arrange
        var logger = LoggerBuilder.Build();

        // Act
        Func<object, Exception, string> formatter = (state, exception) => null;
        logger.Log(level, 0, _state, null, formatter);

        // Assert
        logger.Formatted.Should().BeNullOrEmpty();
    }

    [Fact]
    public void InvalidLogLevel_Throws()
    {
        // Arrange
        var logger = LoggerBuilder.Build();

        // Act
        Action act = () => logger.Log((LogLevel)8, 0, _state, null, _defaultFormatter);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region runtime/JsonConsoleFormatterTests

    // TODO: remove this test
    [Fact(Skip = "Test name is a misnomer, as there is scope")]
    public void NoLogScope_DoesNotWriteAnyScopeContentToOutput_Json()
    {
        // Arrange
        var logger = LoggerBuilder 
            .With(o =>
            {
                o.IncludeScopes = true;
                o.JsonWriterOptions = new JsonWriterOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            })
            .Build();

        // Act
        using (logger.BeginScope("Scope with named parameter {namedParameter}", 123))
        using (logger.BeginScope("SimpleScope"))
            logger.Log(LogLevel.Warning, 0, "Message with {args}", 73);

        // Assert
        Assert.Contains("Message with {args}", logger.Formatted);
        Assert.Contains("73", logger.Formatted);
        Assert.Contains("{OriginalFormat}", logger.Formatted);
        Assert.Contains("namedParameter", logger.Formatted);
        Assert.Contains("123", logger.Formatted);
        Assert.Contains("SimpleScope", logger.Formatted);
    }

    [Fact]
    public void Log_TimestampFormatSet_ContainsTimestamp()
    {
        // Arrange
        var logger = LoggerBuilder
            .With(o => o.TimestampFormat = "hh:mm:ss ")
            .Build();
        
        // Act
        logger.LogCritical(eventId: 0, message: null);
        
        // Assert
        logger.Formatted.Should().BeValidJson() //
            .Subject.Should().HaveElement("Timestamp").Which.Should().MatchRegex(@"\d{2}:\d{2}:\d{2}");

    }

    #endregion
    
    public static TheoryData<LogLevel> LogLevels
    {
        get
        {
            var data = new TheoryData<LogLevel>();
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                if (level == LogLevel.None)
                {
                    continue;
                }

                data.Add(level);
            }

            return data;
        }
    }
}
