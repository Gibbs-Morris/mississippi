using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Temporary Brooks Azure provider shell used to freeze the public registration surface and startup validation while
///     the event read and append implementation is completed in Increment 2.
/// </summary>
internal sealed class BrookStorageProvider : IBrookStorageProvider
{
    private const string NotYetImplementedMessage =
        "Brooks Azure event operations are not available yet. Increment 1 only establishes registration, validation, and startup initialization scaffolding; append and read behavior will arrive in Increment 2.";

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookStorageProvider" /> class.
    /// </summary>
    /// <param name="logger">The logger used for unsupported-operation diagnostics.</param>
    public BrookStorageProvider(
        ILogger<BrookStorageProvider> logger
    )
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ILogger<BrookStorageProvider> Logger { get; }

    /// <inheritdoc />
    public string Format => "azure-blob";

    /// <inheritdoc />
    public Task<BrookPosition> AppendEventsAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition? expectedVersion = null,
        CancellationToken cancellationToken = default
    )
    {
        Logger.OperationNotYetAvailable(nameof(AppendEventsAsync));
        throw new NotSupportedException(NotYetImplementedMessage);
    }

    /// <inheritdoc />
    public Task<BrookPosition> ReadCursorPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    )
    {
        Logger.OperationNotYetAvailable(nameof(ReadCursorPositionAsync));
        throw new NotSupportedException(NotYetImplementedMessage);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookRangeKey brookRange,
        CancellationToken cancellationToken = default
    )
    {
        Logger.OperationNotYetAvailable(nameof(ReadEventsAsync));
        return new ThrowingAsyncEnumerable<BrookEvent>(() => new NotSupportedException(NotYetImplementedMessage));
    }
}