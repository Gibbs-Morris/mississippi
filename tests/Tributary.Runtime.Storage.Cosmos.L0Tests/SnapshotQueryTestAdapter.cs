using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;

using Mississippi.Tributary.Runtime.Storage.Cosmos.Storage;

using Moq;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos.L0Tests;

/// <summary>
///     Runs Snapshot container query tests against its private query DTO without widening production visibility.
/// </summary>
internal static class SnapshotQueryTestAdapter
{
    /// <summary>
    ///     Runs a Snapshot identifier query against deterministic pages for a private DTO type.
    /// </summary>
    /// <typeparam name="T">The private DTO type used by <see cref="SnapshotContainerOperations" />.</typeparam>
    /// <param name="container">The mocked Cosmos container.</param>
    /// <param name="pageValues">The identifier and version values returned by each page.</param>
    /// <param name="operationsFactory">Creates operations with the supplied options.</param>
    /// <param name="partitionKey">The partition key passed to the query.</param>
    /// <param name="cancellationToken">The cancellation token used for enumeration.</param>
    /// <returns>The captured page size, iterator state, and returned identifiers.</returns>
    internal static async Task<(
        int CapturedBatchSize, bool HasMoreResults, IReadOnlyList<(string Id, long Version)> Items )> RunAsync<T>(
        Mock<Container> container,
        IEnumerable<IEnumerable<(string Id, long Version)>> pageValues,
        Func<Mock<Container>, SnapshotStorageOptions, SnapshotContainerOperations> operationsFactory,
        string partitionKey,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(pageValues);
        ArgumentNullException.ThrowIfNull(operationsFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        ConstructorInfo dtoConstructor = typeof(T).GetConstructor(
                                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                             null,
                                             new[] { typeof(string), typeof(long) },
                                             null) ??
                                         throw new InvalidOperationException(
                                             "Snapshot query DTO constructor was not found.");
        List<List<T>> pages = pageValues.Select(page =>
                page.Select(value => (T)dtoConstructor.Invoke(new object?[] { value.Id, value.Version })).ToList())
            .ToList();
        using TestFeedIterator<T> iterator = new(pages);
        int capturedBatchSize = 0;
        container.Setup(c => c.GetItemQueryIterator<T>(
                It.IsAny<QueryDefinition>(),
                null,
                It.Is<QueryRequestOptions>(options => CaptureMaxItemCount(options, out capturedBatchSize))))
            .Returns(iterator);
        SnapshotContainerOperations operations = operationsFactory(
            container,
            new()
            {
                QueryBatchSize = -1,
            });
        List<(string Id, long Version)> results = [];
        await foreach (SnapshotIdVersion item in operations.QuerySnapshotIdsAsync(partitionKey, cancellationToken))
        {
            results.Add((item.Id, item.Version));
        }

        return (capturedBatchSize, iterator.HasMoreResults, results);
    }

    private static bool CaptureMaxItemCount(
        QueryRequestOptions options,
        out int count
    )
    {
        count = options.MaxItemCount ?? 0;
        return true;
    }
}