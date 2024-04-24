using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlatJsonConsoleFormatter;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddFlatJsonConsole(this ILoggingBuilder builder,
        Action<FlatJsonConsoleFormatterOptions>? configure = null)
    {
        builder.AddConsole(options => options.FormatterName = FlatJsonConsoleFormatter.FormatterName);
        builder.AddConsoleFormatter<FlatJsonConsoleFormatter, FlatJsonConsoleFormatterOptions>();
        if (configure != null)
            builder.Services.Configure(configure);
        return builder;
    }
}
