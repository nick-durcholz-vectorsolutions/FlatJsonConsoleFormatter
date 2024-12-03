// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Reports;

namespace Benchmarks.Infrastructure;

public static class SummaryExtensions
{
    public static int ToExitCode(this IEnumerable<Summary>? summaries)
    {
        // an empty summary means that initial filtering and validation did not allow running any benchmarks
        var sums = summaries?.ToArray() ?? Array.Empty<Summary>();
        if (!sums.Any())
            return 1;

        // if anything has failed, it's an error
        if (sums.Any(summary => summary.HasAnyErrors()))
            return 1;

        return 0;
    }

    public static bool HasAnyErrors(this Summary summary) => summary.HasCriticalValidationErrors ||
                                                             summary.Reports.Any(report => report.HasAnyErrors());

    public static bool HasAnyErrors(this BenchmarkReport report) =>
        !report.BuildResult.IsBuildSuccess || !report.AllMeasurements.Any();
}
