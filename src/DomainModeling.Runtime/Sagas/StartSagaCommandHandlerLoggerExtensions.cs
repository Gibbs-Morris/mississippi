using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.DomainModeling.Runtime.Sagas;

/// <summary>
///     Provides diagnostics for rejected saga starts.
/// </summary>
internal static partial class StartSagaCommandHandlerLoggerExtensions
{
    /// <summary>
    ///     Logs invalid workflow metadata before a saga is started.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="sagaId">The requested saga identifier.</param>
    /// <param name="exception">The metadata failure.</param>
    [LoggerMessage(
        1,
        LogLevel.Error,
        "Cannot start saga {SagaType} ({SagaId}) because its workflow metadata is invalid")]
    public static partial void SagaStartMetadataInvalid(
        this ILogger logger,
        string sagaType,
        Guid sagaId,
        Exception exception
    );
}