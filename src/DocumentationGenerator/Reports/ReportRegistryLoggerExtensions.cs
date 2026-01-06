using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.DocumentationGenerator.Reports;

/// <summary>
///     Logger extensions for the report registry.
/// </summary>
public static partial class ReportRegistryLoggerExtensions
{
    /// <summary>
    ///     Logs when a report is registered.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="name">The report name.</param>
    /// <param name="order">The report order.</param>
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Registered report '{Name}' with order {Order}")]
    public static partial void ReportRegistered(
        this ILogger logger,
        string name,
        int order
    );

    /// <summary>
    ///     Logs when executing reports.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of reports to execute.</param>
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Executing {Count} report(s)")]
    public static partial void ExecutingReports(
        this ILogger logger,
        int count
    );

    /// <summary>
    ///     Logs when starting a specific report.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="name">The report name.</param>
    /// <param name="description">The report description.</param>
    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Executing report '{Name}': {Description}")]
    public static partial void ExecutingReport(
        this ILogger logger,
        string name,
        string description
    );

    /// <summary>
    ///     Logs when a report completes successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="name">The report name.</param>
    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Report '{Name}' completed successfully")]
    public static partial void ReportCompleted(
        this ILogger logger,
        string name
    );

    /// <summary>
    ///     Logs when a report fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="name">The report name.</param>
    /// <param name="exception">The exception that occurred.</param>
    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Error,
        Message = "Report '{Name}' failed")]
    public static partial void ReportFailed(
        this ILogger logger,
        string name,
        Exception exception
    );

    /// <summary>
    ///     Logs when all reports complete.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of reports completed.</param>
    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Information,
        Message = "All {Count} report(s) completed successfully")]
    public static partial void AllReportsCompleted(
        this ILogger logger,
        int count
    );

    /// <summary>
    ///     Logs when no reports match the filter.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="filter">The filter that was applied.</param>
    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Warning,
        Message = "No reports matched the specified filter: {Filter}")]
    public static partial void NoMatchingReports(
        this ILogger logger,
        string filter
    );
}
