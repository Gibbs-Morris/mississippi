namespace Mississippi.AspNetCore.Orleans.L0Tests.Caching;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.AspNetCore.Orleans.Caching;
using Mississippi.AspNetCore.Orleans.Caching.Grains;
using Mississippi.AspNetCore.Orleans.Caching.Options;
using Moq;
using Orleans;
using Xunit;

/// <summary>
/// Tests for <see cref="OrleansDistributedCache"/>.
/// </summary>
public sealed class OrleansDistributedCacheTests
{
    /// <summary>
    /// GetAsync with null key should throw ArgumentNullException.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OrleansDistributedCache>>();
        var mockCluster = new Mock<IClusterClient>();
        var options = Options.Create(new OrleansDistributedCacheOptions());
        var cache = new OrleansDistributedCache(mockLogger.Object, mockCluster.Object, options, TimeProvider.System);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.GetAsync(null!, CancellationToken.None));
    }

    /// <summary>
    /// SetAsync with null key should throw ArgumentNullException.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OrleansDistributedCache>>();
        var mockCluster = new Mock<IClusterClient>();
        var options = Options.Create(new OrleansDistributedCacheOptions());
        var cache = new OrleansDistributedCache(mockLogger.Object, mockCluster.Object, options, TimeProvider.System);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.SetAsync(null!, [], null, CancellationToken.None));
    }

    /// <summary>
    /// RemoveAsync with null key should throw ArgumentNullException.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RemoveAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OrleansDistributedCache>>();
        var mockCluster = new Mock<IClusterClient>();
        var options = Options.Create(new OrleansDistributedCacheOptions());
        var cache = new OrleansDistributedCache(mockLogger.Object, mockCluster.Object, options, TimeProvider.System);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.RemoveAsync(null!, CancellationToken.None));
    }
}
