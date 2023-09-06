//Adapted from 8e9a17b2 of https://github.com/dotnet/runtime.git

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace FlatJsonConsoleFormatter;

public class FlatJsonConsoleFormatter : ConsoleFormatter, IDisposable
{
    public const string FormatterName = "flat-json";
    private readonly IDisposable? _optionsReloadToken;

    public FlatJsonConsoleFormatter(IOptionsMonitor<FlatJsonConsoleFormatterOptions> options)
        : base(FormatterName)
    {
        ReloadLoggerOptions(options.CurrentValue);
        _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (logEntry.Exception == null && message == null)
        {
            return;
        }
        LogLevel logLevel = logEntry.LogLevel;
        string category = logEntry.Category;
        int eventId = logEntry.EventId.Id;
        Exception? exception = logEntry.Exception;
        const int DefaultBufferSize = 1024;
        using (var output = new PooledByteBufferWriter(DefaultBufferSize))
        {
            using (var writer = new Utf8JsonWriter(output, FormatterOptions.JsonWriterOptions))
            {
                writer.WriteStartObject();
                var timestampFormat = FormatterOptions.TimestampFormat;
                if (timestampFormat != null)
                {
                    DateTimeOffset dateTimeOffset = FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
                    writer.WriteString("Timestamp", dateTimeOffset.ToString(timestampFormat));
                }
                if (FormatterOptions.IncludeEventId)
                    writer.WriteNumber(nameof(logEntry.EventId), eventId);
                writer.WriteString(nameof(logEntry.LogLevel), GetLogLevelString(logLevel));
                if (FormatterOptions.TruncateCategory)
                {
                    var cat = logEntry.Category.AsSpan();
                    int i = cat.LastIndexOf('.');
                    if (i > 0)
                        cat = cat.Slice(i + 1);
                    writer.WriteString(nameof(logEntry.Category), cat);
                }
                else
                    writer.WriteString(nameof(logEntry.Category), category);
                writer.WriteString("Message", message);

                if (exception != null)
                {
                    writer.WriteString(nameof(Exception), exception.ToString());
                }

                var messageProperties = new Dictionary<string, object?>(0);
                AddScopeInformation(messageProperties, writer, scopeProvider);
                if (logEntry.State is IReadOnlyCollection<KeyValuePair<string, object>> stateProperties)
                {
                    foreach (KeyValuePair<string, object> item in stateProperties)
                    {
                        if (item.Key != "{OriginalFormat}")
                            messageProperties[item.Key] = item.Value;
                    }
                }

                foreach (var prop in messageProperties) 
                    WriteItem(writer, prop);

                writer.WriteEndObject();
                writer.Flush();
            }
#if NETCOREAPP
            textWriter.Write(Encoding.UTF8.GetString(output.WrittenMemory.Span));
#else
                textWriter.Write(Encoding.UTF8.GetString(output.WrittenMemory.Span.ToArray()));
#endif
        }
        textWriter.Write(Environment.NewLine);
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "Trace",
            LogLevel.Debug => "Debug",
            LogLevel.Information => "Information",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Critical",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }

    private void AddScopeInformation(Dictionary<string, object?> messageProperties, Utf8JsonWriter writer, IExternalScopeProvider? scopeProvider)
    {
        int scopeNum = 0;
        if (FormatterOptions.IncludeScopes && scopeProvider != null)
        {
            scopeProvider.ForEachScope((scope, _) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object>> scopeItems)
                {
                    foreach (KeyValuePair<string, object> item in scopeItems)
                    {
                        messageProperties[item.Key] = item.Value;
                    }
                }
                else
                {
                    messageProperties[$"Scope{scopeNum++}"] = ToInvariantString(scope);
                }
            }, writer);
        }
    }

    private static void WriteItem(Utf8JsonWriter writer, KeyValuePair<string, object?> item)
    {
        var key = item.Key;
        switch (item.Value)
        {
            case bool boolValue:
                writer.WriteBoolean(key, boolValue);
                break;
            case byte byteValue:
                writer.WriteNumber(key, byteValue);
                break;
            case sbyte sbyteValue:
                writer.WriteNumber(key, sbyteValue);
                break;
            case char charValue:
#if NETCOREAPP
                writer.WriteString(key, MemoryMarshal.CreateSpan(ref charValue, 1));
#else
                    writer.WriteString(key, charValue.ToString());
#endif
                break;
            case decimal decimalValue:
                writer.WriteNumber(key, decimalValue);
                break;
            case double doubleValue:
                writer.WriteNumber(key, doubleValue);
                break;
            case float floatValue:
                writer.WriteNumber(key, floatValue);
                break;
            case int intValue:
                writer.WriteNumber(key, intValue);
                break;
            case uint uintValue:
                writer.WriteNumber(key, uintValue);
                break;
            case long longValue:
                writer.WriteNumber(key, longValue);
                break;
            case ulong ulongValue:
                writer.WriteNumber(key, ulongValue);
                break;
            case short shortValue:
                writer.WriteNumber(key, shortValue);
                break;
            case ushort ushortValue:
                writer.WriteNumber(key, ushortValue);
                break;
            case null:
                writer.WriteNull(key);
                break;
            default:
                writer.WriteString(key, ToInvariantString(item.Value));
                break;
        }
    }

    private static string? ToInvariantString(object? obj) => Convert.ToString(obj, CultureInfo.InvariantCulture);

    internal FlatJsonConsoleFormatterOptions FormatterOptions { get; set; }

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(FormatterOptions))]
    private void ReloadLoggerOptions(FlatJsonConsoleFormatterOptions options)
    {
        FormatterOptions = options;
    }

    public void Dispose()
    {
        _optionsReloadToken?.Dispose();
    }
}