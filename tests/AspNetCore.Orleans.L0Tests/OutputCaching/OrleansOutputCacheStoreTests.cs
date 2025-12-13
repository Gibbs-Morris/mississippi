namespace Mississippi.AspNetCore.Orleans.L0Tests.OutputCaching;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.AspNetCore.Orleans.OutputCaching;
using Mississippi.AspNetCore.Orleans.OutputCaching.Options;
using Moq;
using Orleans;
using Xunit;

/// <summary>
/// Tests for <see cref="OrleansOutputCacheStore"/>.
/// </summary>
public sealed class OrleansOutputCacheStoreTests
{
    /// <summary>
    /// GetAsync with null key should throw ArgumentNullException.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OrleansOutputCacheStore>>();
        var mockCluster = new Mock<IClusterClient>();
        var options = Options.Create(new OrleansOutputCacheOptions());
        var store = new OrleansOutputCacheStore(mockLogger.Object, mockCluster.Object, options, TimeProvider.System);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetAsync(null!, CancellationToken.None));
    }

    /// <summary>
    /// SetAsync with null key should throw ArgumentNullException.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OrleansOutputCacheStore>>();
        var mockCluster = new Mock<IClusterClient>();
        var options = Options.Create(new OrleansOutputCacheOptions());
        var store = new OrleansOutputCacheStore(mockLogger.Object, mockCluster.Object, options, TimeProvider.System);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetAsync(null!, [], null, [], CancellationToken.None));
    }

    /// <summary>
    /// EvictByTagAsync with null tag should throw ArgumentNullException.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task EvictByTagAsync_NullTag_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OrleansOutputCacheStore>>();
        var mockCluster = new Mock<IClusterClient>();
        var options = Options.Create(new OrleansOutputCacheOptions());
        var store = new OrleansOutputCacheStore(mockLogger.Object, mockCluster.Object, options, TimeProvider.System);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.EvictByTagAsync(null!, CancellationToken.None));
    }
}
