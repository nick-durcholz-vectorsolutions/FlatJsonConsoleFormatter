using System.Text.Encodings.Web;
using System.Text.Json;
using Demo;
using FlatJsonConsoleFormatter;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddLogging(builder => builder.AddFlatJsonConsole())
    .AddTransient<MyService>()
    .BuildServiceProvider();

Console.WriteLine("Example with default configuration values:");
Console.WriteLine("==========================================");
services.GetRequiredService<MyService>().LogMessages();
services.Dispose();

Console.WriteLine();
Console.WriteLine();

services = new ServiceCollection()
    .AddLogging(builder => builder.AddFlatJsonConsole(o =>
    {
        o.TimestampFormat = "O"; //ISO 8601
        o.UseUtcTimestamp = true;
        o.TruncateCategory = false;
        o.IncludeEventId = true;
        o.JsonWriterOptions = new JsonWriterOptions
        {
            // Without the encoder, timestamps will look like:
            //    "Timestamp": "2024-12-05T17:26:21.2701987\u002B00:00"
            // With the encoder, timestamps will look like:
            //    "Timestamp": "2024-12-05T17:26:21.2701987+00:00"
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = true,
        };
    }))
    .AddTransient<MyService>()
    .BuildServiceProvider();

Console.WriteLine("Example with formatting, timestamps, and other options:");
Console.WriteLine("=======================================================");
services.GetRequiredService<MyService>().LogMessages();

//disposing the service collection causes it to dispose Microsoft.Extensions.Logging classes, which flushes buffers
services.Dispose();
