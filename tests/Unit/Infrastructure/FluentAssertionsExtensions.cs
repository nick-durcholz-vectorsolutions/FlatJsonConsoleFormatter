using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Json;
using Newtonsoft.Json.Linq;

namespace Unit.Infrastructure;

public static class FluentAssertionsExtensions
{
    /// <summary>
    ///     Asserts that the current <see cref="JToken" /> has exactly one direct or indirect child element with the specified
    ///     <paramref name="expected" /> name.
    /// </summary>
    /// <param name="assertion"></param>
    /// <param name="expected">The name of the expected child element</param>
    /// <param name="because">
    ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
    /// </param>
    [CustomAssertion]
    public static AndWhichConstraint<JTokenAssertions,JToken> HaveElementSomewhere(this JTokenAssertions assertion,
        string expected, string because = "",
        params object[] becauseArgs)
    {
        var jsonPathResult = SearchWithJsonPath(assertion, expected);
        Execute.Assertion.BecauseOf(because, becauseArgs)
            .ForCondition(jsonPathResult != null)
            .FailWith("Expected JSON document {0} to have element \"" + expected.EscapePlaceholders() + "\"{reason}" +
                      ", but no such element was found.", assertion.Subject);
        return new AndWhichConstraint<JTokenAssertions,JToken>(assertion, jsonPathResult!);
    }

    private static JToken? SearchWithJsonPath(JTokenAssertions assertion, string expected)
    {
        var jsonPath = $"$..['{expected}']";
        var jsonPathResult = assertion.Subject.SelectToken(jsonPath);
        return jsonPathResult;
    }

    /// <summary>
    ///     Asserts that the current <see cref="JToken" /> does not have a direct or indirect child element with the specified
    ///     <paramref name="unexpected" /> name.
    /// </summary>
    /// <param name="unexpected">The name of the not expected child element</param>
    /// <param name="because">
    ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
    /// </param>
    public static AndConstraint<JTokenAssertions> NotHaveElementSomewhere(this JTokenAssertions assertion, string unexpected, string because = "",
        params object[] becauseArgs)
    {
       
        var jsonPathResult = SearchWithJsonPath(assertion, unexpected);
        Execute.Assertion
            .ForCondition(jsonPathResult == null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Did not expect JSON document {0} to have element \"" + unexpected.EscapePlaceholders() + "\"{reason}.", assertion.Subject);

        return new AndConstraint<JTokenAssertions>(assertion);
    }
    
    /// <summary>
    /// Replaces all characters that might conflict with formatting placeholders with their escaped counterparts.
    /// </summary>
    //
    // from https://github.com/fluentassertions/fluentassertions.json/blob/5c95025ba1459fbdfb30e7e62aa652003eeb21cc/Src/FluentAssertions.Json/Common/StringExtensions.cs
    static string EscapePlaceholders(this string value) =>
        value.Replace("{", "{{").Replace("}", "}}");
}
