//Adapted from 5fa2080e2fff7bb31a4235250ce2f7a4bb1b64cb of https://github.com/dotnet/runtime.git src/libraries/Microsoft.Extensions.Logging.Console/src/JsonConsoleFormatter.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    internal FlatJsonConsoleFormatterOptions FormatterOptions { get; set; }

    public void Dispose() => _optionsReloadToken?.Dispose();

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (logEntry.Exception == null && message == null)
        {
            return;
        }

        var logLevel = logEntry.LogLevel;
        var category = logEntry.Category;
        var eventId = logEntry.EventId.Id;
        var exception = logEntry.Exception;
        const int DefaultBufferSize = 1024;
        using (var output = new PooledByteBufferWriter(DefaultBufferSize))
        {
            using (var writer = new Utf8JsonWriter(output, FormatterOptions.JsonWriterOptions))
            {
                var messageProperties = new Dictionary<string, object?>();
                var timestampFormat = FormatterOptions.TimestampFormat;
                if (timestampFormat != null)
                {
                    var dateTimeOffset = FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
                    AddMessageProperty(messageProperties, "Timestamp", dateTimeOffset.ToString(timestampFormat));
                }

                if (FormatterOptions.IncludeEventId)
                    AddMessageProperty(messageProperties, nameof(logEntry.EventId), eventId);
                AddMessageProperty(messageProperties, nameof(logEntry.LogLevel), GetLogLevelString(logLevel));
                if (FormatterOptions.TruncateCategory)
                {
                    var cat = logEntry.Category;
                    var i = cat.LastIndexOf('.');
                    if (i > 0)
                        cat = cat.Substring(i + 1);
                    AddMessageProperty(messageProperties, nameof(logEntry.Category), cat);
                }
                else
                    AddMessageProperty(messageProperties, nameof(logEntry.Category), category);

                AddMessageProperty(messageProperties, "Message", message);

                if (exception != null)
                    AddMessageProperty(messageProperties, nameof(Exception), exception.ToString());

                AddScopeInformation(messageProperties, writer, scopeProvider);
                if (logEntry.State is IReadOnlyCollection<KeyValuePair<string, object>> stateProperties)
                {
                    foreach (var item in stateProperties)
                    {
                        if (item.Key != "{OriginalFormat}")
                            AddMessageProperty(messageProperties, item.Key, item.Value);
                    }
                }

                writer.WriteStartObject();
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

    private void AddMessageProperty(Dictionary<string, object?> messageProperties, string key, object value)
    {
        if (FormatterOptions.MergeDuplicateKeys)
        {
            messageProperties[key] = value;
        }
        else
        {
            var k = key;
            var n = 1;
            while (messageProperties.ContainsKey(k))
                k = $"{key}_{n++}";
            messageProperties.Add(k, value);
        }
    }

    private static string GetLogLevelString(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => "Trace",
            LogLevel.Debug => "Debug",
            LogLevel.Information => "Information",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Critical",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };

    private void AddScopeInformation(Dictionary<string, object?> messageProperties, Utf8JsonWriter writer,
        IExternalScopeProvider? scopeProvider)
    {
        var scopeNum = 0;
        if (FormatterOptions.IncludeScopes && scopeProvider != null)
        {
            scopeProvider.ForEachScope((scope, _) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object>> scopeItems)
                {
                    foreach (var item in scopeItems)
                    {
                        AddMessageProperty(messageProperties, item.Key, item.Value);
                    }
                }
                else
                {
                    AddMessageProperty(messageProperties, $"Scope{scopeNum++}", ToInvariantString(scope));
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

    [MemberNotNull(nameof(FormatterOptions))]
    private void ReloadLoggerOptions(FlatJsonConsoleFormatterOptions options) => FormatterOptions = options;
}
