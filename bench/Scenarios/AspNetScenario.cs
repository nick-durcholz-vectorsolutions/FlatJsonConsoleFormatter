using System.Runtime.CompilerServices;
using Benchmarks.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Benchmarks.Scenarios;

/// <summary>
///     Taken from ASP.NET Core app running minimal API
/// </summary>
public static partial class AspNetScenario
{
    private static readonly StaticScope[] AspNetHttpClientScopes =
    {
        new(
            "SpanId:e042d430e99e7c3e, TraceId:c703d6abe4ff99707d563be72b5f4356, ParentId:0000000000000000",
            new Dictionary<string, object>
            {
                ["SpanId"] = "e042d430e99e7c3e",
                ["TraceId"] = "c703d6abe4ff99707d563be72b5f4356",
                ["ParentId"] = "0000000000000000"
            }),
        new("ConnectionId:0HN33MSVUUJUN",
            new Dictionary<string, object> { ["ConnectionId"] = "0HN33MSVUUJUN" }),
        new(
            "formTypeReferenceId:80A6AB4B-821E-417E-BB6A-C8322B9ED219, submissionId:caca0399-274a-4fe4-81af-9ddd42b4a22d",
            new Dictionary<string, object>
            {
                ["FormTypeReferenceId"] = "80A6AB4B-821E-417E-BB6A-C8322B9ED219",
                ["SubmissionId"] = "caca0399-274a-4fe4-81af-9ddd42b4a22d",
                ["{OriginalFormat}"] =
                    "IncomingTypeReferenceId:{IncomingTypeReferenceId}, submissionId:{SubmissionId}"
            }),
        new(
            "RequestPath:/something/long/80A6AB4B-821E-417E-BB6A-C8322B9ED219 RequestId:0HN33MSVUUJUN:0000000",
            new Dictionary<string, object>
            {
                ["RequestPath"] = "something/long/80A6AB4B-821E-417E-BB6A-C8322B9ED219",
                ["RequestId"] = "0HN33MSVUUJUN:0000000"
            }),
        new(
            "HTTP POST https://some-identity-server.example.local/adfs/services/trust/13/issuedtokenmixedsymmetricbasic256",
            new Dictionary<string, object>
            {
                ["Method"] = "POST",
                ["Uri"] =
                    "https://some-identity-server.example.local/adfs/services/trust/13/issuedtokenmixedsymmetricbasic256",
                ["{OriginalFormat}"] = "HTTP {Method} {Uri}"
            })
    };

    public static readonly StaticExternalScopeProvider ScopeProvider = new(AspNetHttpClientScopes);

    private static readonly Guid _guid = new("caca0399-274a-4fe4-81af-9ddd42b4a22d");


    [LoggerMessage(LogLevel.Information, "End processing HTTP request after {ElapsedMilliseconds}ms - {StatusCode}",
        EventId = 101, EventName = "RequestPipelineEnd")]
    static partial void LogRequestPipelineEnd(ILogger logger, double elapsedMilliseconds, int statusCode);

    [LoggerMessage(
        "Metadata: submissionId:{SubmissionId}, formTypeReferenceId:{FormTypeReferenceId}, userEmailAddress:{UserEmailAddress}, " +
        "userName:{UserName}, userSurname:{UserSurname}, userGivenName:{UserGivenName}, " +
        "cachedSubmissionContentKey:{CachedSubmissionContentKey}, currentSubmissionCounter:{CurrentSubmissionCounter}" +
        "timeGoneBy:{TimeGoneBy:0000.00000000000}")]
    static partial void LogNineAttributesWithDuplicatesToScope(ILogger logger,LogLevel logLevel,  Exception? exception, Guid submissionId, string formTypeReferenceId,
        string userEmailAddress, string userName, string userSurname, string userGivenName,
        Guid cachedSubmissionContentKey, long currentSubmissionCounter, double timeGoneBy);

    [LoggerMessage(LogLevel.Error, "An unhandled exception has occurred while executing the request")]
    static partial void LogHugeException(ILogger logger, Exception exception);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Small(ILogger logger) => LogRequestPipelineEnd(logger, 123.45, 200);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NineAttributes(ILogger logger) => LogNineAttributesWithDuplicatesToScope(logger,
        LogLevel.Information,null, _guid, "80A6AB4B-821E-417E-BB6A-C8322B9ED219",
        "test.test@example.local", "test.test.test", "test", "test",
        _guid, int.MaxValue, 123.453283898734168367812);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ExHuge(ILogger logger) => LogHugeException(logger, Exceptions.HugeAggregateException);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Max(ILogger logger) => LogNineAttributesWithDuplicatesToScope(logger,
        LogLevel.Error,Exceptions.HugeAggregateException, _guid, "80A6AB4B-821E-417E-BB6A-C8322B9ED219",
        "test.test@example.local", "test.test.test", "test", "test",
        _guid, int.MaxValue, 123.453283898734168367812);
}
