using FluentAssertions;
using FluentAssertions.Json;
using Xunit.Sdk;

namespace Unit.Infrastructure;

public class FluentAssertionsExtensionsTests
{
    [Fact]
    public void HaveElementSomewhere_ElementExists_DoesNotThrowAndReturnsWhich()
    {
        var json = """
                   {
                        "a": {
                            "b": {
                                "key": "value",
                                "x": "y"
                            },
                        }
                   }
                   """;
        
        json
            .Should().BeValidJson().Which //
            .Should().HaveElementSomewhere("key").Which //
            .Should().HaveValue("value");
    }
    
    [Fact]
    public void HaveElementSomewhere_ElementDoesNotExist_Throws()
    {
        var json = """
                   {
                        "a": {
                            "b": {
                                "x": "y"
                            },
                        }
                   }
                   """;
        
        Action act = () => json
            .Should().BeValidJson().Which //
            .Should().HaveElementSomewhere("key").Which //
            .Should().HaveValue("value");

        act.Should().Throw<XunitException>();
    }
    
    [Fact]
    public void NotHaveElementSomewhere_ElementDoesNotExist_ReturnsAnd()
    {
        var json = """
                   {
                        "a": {
                            "b": {
                                "x": "y"
                            },
                        }
                   }
                   """;

        json
            .Should().BeValidJson().Which //
            .Should().NotHaveElementSomewhere("key") //
            .Should().NotBeNull();
    }
    
    [Fact]
    public void NotHaveElementSomewhere_ElementExists_Throws()
    {
        var json = """
                   {
                        "a": {
                            "b": {
                                "key": "value",
                                "x": "y"
                            },
                        }
                   }
                   """;

        Action act = () => json
            .Should().BeValidJson().Which //
            .Should().NotHaveElementSomewhere("key");

        act.Should().Throw<XunitException>();
    }
}
