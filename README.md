# FlatJsonConsoleFormatter

This project emits log messages as json, but constructs the log message differently than the
default JsonConsoleFormatter. The default JSON formatter creates an object that is deeply nested,
includes unnecessary information, and repeats log messages. This log message formatter gives you a
simple collection of key/value pairs formatted as a json object that includes state and scopes,
while avoiding unnecessary information.

## Breaking Changes in v2.0

In version 2.0, default options were introduced to make the most common use cases produce more
succinct log messages.

* Category names are truncated by default to include only the text after the lat period. Typically,
  this means that the log category will be the class name without the namespace.
* Duplicate scope keys are merged instead of numbered.

To use old behavior, set explicit options in startup:

    var services = new ServiceCollection();
    services.AddLogging(builder =>
    {
        builder.AddConfiguration(configuration.GetSection("Logging"));
        builder.AddFlatJsonConsole(options => {
            options.TruncateCategory = false;
            options.MergeDuplicateKeys = false;
        });
    });

## Usage

Add the nuget reference

    dotnet add package FlatJsonConsoleFormatter

Setup the library in your project

    var services = new ServiceCollection();
    services.AddLogging(builder =>
    {
        builder.AddConfiguration(configuration.GetSection("Logging"));
        builder.AddFlatJsonConsole();
    });

## Why another formatter?

Here is an example of a message logged using JsonConsoleFormatter that includes scopes, where the scope object is
Dictionary<string, object>.

    using (_log.BeginScope(new Dictionary<string, object> { { "MessageId", messageId } }))
    using (_log.BeginScope(new Dictionary<string, object> { { "BaseUrl", url }, { "CustomerId", customerId } }))
    {
        _log.LogDebug("GET {Endpoint}", endpoint);
    }

The above code results in the following log message:

    {
        "EventId": 0,
        "LogLevel": "Debug",
        "Category": "ACME.Project.ApiInvoker",
        "Message": "GET https://example.com/api/endpoint/32120",
        "State": {
            "Message": "GET https://example.com/api/endpoint/32120",
            "Endpoint": "https://example.com/api/endpoint/32120",
            "{OriginalFormat}": "GET {Endpoint}"rstem.Object]",
                "MessageId": "a38cb57d-4719-4d39-a36c-19f75b289bb4"
            },
            {
                "Message": "System.Collections.Generic.Dictionary\u00602[System.String,System.Object]",
                "BaseUrl": "https://example.com/api",
                "CustomerId": 32120
            }
        ]
    }

### What is wrong with that log message?

* The message `GET https://example.com/api` is repeated multiple times
* I don't care what the original format string was. I care about having the format values and the result.
* The state and scope information is contained as nested objects, which makes it harder than necessary to parse using
  tools such as AWS CloudWatch Log Insights
* I don't care about the type of the individual scope objects. The scope object is just a collection of values I want
  logged.
* Not shown above, but if you want your json formatted on a single line and log an exception, then newlines will be
  stripped from the exception message making the stack trace unreadable. JSON has an escape sequence for newline
  characters. This is unnecessary and weird, and seems to be removed in .NET 8.

### How does this project address those shortcomings?

FlatJsonConsoleFormatter will log the following message instead:

    {
        "EventId": 0,
        "LogLevel": "Debug",
        "Category": "ACME.Project.ApiInvoker",
        "Message": "GET https://example.com/api/endpoint/32120",
        "Endpoint": "https://example.com/api/endpoint/32120",
        "MessageId": "a38cb57d-4719-4d39-a36c-19f75b289bb4"
        "BaseUrl": "https://example.com/api",
        "CustomerId": 32120
    }

It does this by merging the state and scope keys into a dictionary and writing them as top-level properties. When there
is a conflict between the names of keys from different sources, the default strategy is "last one wins", but you can set
an option in startup to keep all scope items and differentiate them by appending a number to the key.
