//Adapted from 8e9a17b2 of https://github.com/dotnet/runtime.git

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Logging.Console;

namespace FlatJsonConsoleFormatter;

/// <summary>
///     Options for FlatJsonConsoleFormatter. Scopes are included by default.
/// </summary>
public class FlatJsonConsoleFormatterOptions : ConsoleFormatterOptions
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FlatJsonConsoleFormatterOptions" /> class.
    /// </summary>
    public FlatJsonConsoleFormatterOptions()
    {
        IncludeScopes = true;
    }

    /// <summary>
    ///     Gets or sets JsonWriterOptions.
    /// </summary>
    public JsonWriterOptions JsonWriterOptions { get; set; }

    /// <summary>
    ///     If true, the logged category will only include characters after the last '.'. Defaults to false.
    /// </summary>
    public bool TruncateCategory { get; set; }

    /// <summary>
    ///     Whether to include the seldom used EventId field. Defaults to false
    /// </summary>
    public bool IncludeEventId { get; set; }

    /// <summary>
    ///     If a log property is repeated from multiple sources, this controls whether the emitted log message has a single
    ///     property with the last logged value of the state key or whether a number is appended to deduplicate them.
    /// </summary>
    /// <remarks>
    ///     Given the following code:
    ///     <code>
    ///     using (_logger.BeginScope(new Dictionary&lt;string, object&gt; { { "x", 1 } }))
    ///     using (_logger.BeginScope(new Dictionary&lt;string, object&gt; { { "x", 2 } }))
    ///         _logger.LogDebug("x is {x}", 3);
    ///     </code>
    ///
    ///     Set this to true in order to log {x: 3}
    ///     Set this to false in order to log {x: 1, x_1: 2, x_2: 3} 
    /// </remarks>
    public bool MergeDuplicateKeys { get; set; }
}