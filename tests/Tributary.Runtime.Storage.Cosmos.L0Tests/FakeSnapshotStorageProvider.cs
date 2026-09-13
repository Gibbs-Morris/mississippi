using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Provides a test-only snapshot provider for registration identity tests.
/// </summary>
internal sealed class FakeSnapshotStorageProvider : ISnapshotStorageProvider
{
    /// <inheritdoc />
    public string Format => "fake";

    /// <inheritdoc />
    public Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    ) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    ) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    ) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult<SnapshotEnvelope?>(null);

    /// <inheritdoc />
    public Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
        CancellationToken cancellationToken = default
    ) =>
        Task.CompletedTask;
}