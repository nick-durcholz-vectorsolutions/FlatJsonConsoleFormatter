//Adapted from commit 9d5a6a9aa463d6d10b0b0ba6d5982cc82f363dc3 of https://github.com/dotnet/runtime
//https://github.com/dotnet/runtime/blob/9d5a6a9aa463d6d10b0b0ba6d5982cc82f363dc3/src/libraries/Microsoft.Extensions.Logging.Console/src/JsonConsoleFormatter.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
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
        if (logEntry.State is BufferedLogRecord bufferedRecord)
        {
            string message = bufferedRecord.FormattedMessage ?? string.Empty;
            WriteInternal(null, textWriter, message, bufferedRecord.LogLevel, logEntry.Category, bufferedRecord.EventId.Id, bufferedRecord.Exception,
                          bufferedRecord.Attributes.Count > 0, null, bufferedRecord.Attributes, bufferedRecord.Timestamp);
        }
        else
        {
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            if (logEntry.Exception == null && message == null)
            {
                return;
            }

            DateTimeOffset stamp = FormatterOptions.TimestampFormat != null
                ? (FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now)
                : DateTimeOffset.MinValue;

            // We extract most of the work into a non-generic method to save code size. If this was left in the generic
            // method, we'd get generic specialization for all TState parameters, but that's unnecessary.
            WriteInternal(scopeProvider, textWriter, message, logEntry.LogLevel, logEntry.Category, logEntry.EventId.Id, logEntry.Exception?.ToString(),
                          logEntry.State != null, logEntry.State?.ToString(), logEntry.State as IReadOnlyList<KeyValuePair<string, object?>>, stamp);
        }
    }

    private void WriteInternal(IExternalScopeProvider? scopeProvider, TextWriter textWriter, string? message, LogLevel logLevel,
        string category, int eventId, string? exception, bool hasState, string? stateMessage, IReadOnlyList<KeyValuePair<string, object?>>? stateProperties,
        DateTimeOffset stamp)
    {
        const int DefaultBufferSize = 1024;
        using (var output = new PooledByteBufferWriter(DefaultBufferSize))
        {
            using (var writer = new Utf8JsonWriter(output, FormatterOptions.JsonWriterOptions))
            {
                var messageProperties = new Dictionary<string, object?>();
                var timestampFormat = FormatterOptions.TimestampFormat;
                if (timestampFormat != null)
                {
                    AddMessageProperty(messageProperties, "Timestamp", stamp.ToString(timestampFormat));
                }
                if (FormatterOptions.IncludeEventId)
                    AddMessageProperty(messageProperties, nameof(LogEntry<object>.EventId), eventId);
                AddMessageProperty(messageProperties, nameof(LogEntry<object>.LogLevel), GetLogLevelString(logLevel));
                if (FormatterOptions.TruncateCategory)
                {
                    var cat = category;
                    int i = cat.LastIndexOf('.');
                    if (i > 0)
                        cat = cat.Substring(i + 1);
                    AddMessageProperty(messageProperties, nameof(LogEntry<object>.Category), cat);
                }
                else
                    AddMessageProperty(messageProperties, nameof(LogEntry<object>.Category), category);
                AddMessageProperty(messageProperties, "Message", message);

                if (exception != null)
                    AddMessageProperty(messageProperties, nameof(Exception), exception);

                AddScopeInformation(messageProperties, writer, scopeProvider);
                if (hasState)
                {
                    if (stateProperties != null)
                    {
                        foreach (KeyValuePair<string, object?> item in stateProperties)
                        {
                            if (item.Key != "{OriginalFormat}")
                                AddMessageProperty(messageProperties, item.Key, item.Value);
                        }
                    }
                    else if (stateMessage != null)
                        AddMessageProperty(messageProperties, nameof(LogEntry<object>.State), stateMessage);
                }

                writer.WriteStartObject();
                foreach (var prop in messageProperties)
                    WriteItem(writer, prop);
                writer.WriteEndObject();
                writer.Flush();
            }

            var messageBytes = output.WrittenMemory.Span;
            var logMessageBuffer = ArrayPool<char>.Shared.Rent(Encoding.UTF8.GetMaxCharCount(messageBytes.Length));
            try
            {
#if NET
                var charsWritten = Encoding.UTF8.GetChars(messageBytes, logMessageBuffer);
#else
               int charsWritten;
               unsafe
               {
                   fixed (byte* messageBytesPtr = messageBytes)
                   fixed (char* logMessageBufferPtr = logMessageBuffer)
                   {
                       charsWritten = Encoding.UTF8.GetChars(messageBytesPtr, messageBytes.Length, logMessageBufferPtr, logMessageBuffer.Length);
                   }
               }
#endif
                textWriter.Write(logMessageBuffer, 0, charsWritten);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(logMessageBuffer);
            }
        }
        textWriter.Write(Environment.NewLine);
    }

    private void AddMessageProperty(Dictionary<string, object?> messageProperties, string key, object? value)
    {
        if (FormatterOptions.MergeDuplicateKeys)
        {
            messageProperties[key] = value;
        }
        else
        {
            string k = key;
            int n = 1;
            while (messageProperties.ContainsKey(k))
                k = $"{key}_{n++}";
            messageProperties.Add(k, value);
        }
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
                if (scope is IEnumerable<KeyValuePair<string, object?>> scopeItems)
                {
                    foreach (KeyValuePair<string, object?> item in scopeItems)
                    {
                        AddMessageProperty(messageProperties,
                                           item.Key == "{OriginalFormat}" ? $"Scope{scopeNum++}" : item.Key,
                                           item.Value);
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
#if NET
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
