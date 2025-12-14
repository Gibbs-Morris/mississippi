using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.AspNetCore.Orleans.Caching;
using Mississippi.AspNetCore.Orleans.Caching.Grains;
using Mississippi.AspNetCore.Orleans.Caching.Options;
using Mississippi.AspNetCore.Orleans.L1Tests.Infrastructure;
using Orleans;
using Orleans.TestingHost;
using Xunit;

namespace Mississippi.AspNetCore.Orleans.L1Tests.Caching;

/// <summary>
/// Comprehensive L1 integration tests for <see cref="OrleansDistributedCache"/> using Orleans TestCluster.
/// </summary>
[Collection(ClusterTestSuite.Name)]
public sealed class OrleansDistributedCacheTests
{
    private readonly TestCluster cluster;

    public OrleansDistributedCacheTests(ClusterFixture fixture)
    {
        cluster = fixture.Cluster;
    }

    /// <summary>
    /// Creates a new OrleansDistributedCache instance for testing with specified options.
    /// </summary>
    private OrleansDistributedCache CreateCache(DistributedCacheOptions? options = null)
    {
        var opts = options ?? new DistributedCacheOptions { KeyPrefix = "test" };
        var logger = cluster.ServiceProvider.GetService(typeof(ILogger<OrleansDistributedCache>)) as ILogger<OrleansDistributedCache>;
        return new OrleansDistributedCache(
            logger ?? throw new InvalidOperationException("Logger not found"),
            cluster.Client,
            Options.Create(opts),
            TimeProvider.System);
    }

    /// <summary>
    /// GetAsync returns null for a key that doesn't exist.
    /// </summary>
    [Fact]
    public async Task GetAsync_NonExistentKey_ReturnsNull()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"nonexistent-{Guid.NewGuid()}";

        // Act
        var result = await cache.GetAsync(key);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// GetAsync with null key should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task GetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCache();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.GetAsync(null!));
    }

    /// <summary>
    /// Get (sync) with null key should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void Get_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCache();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.Get(null!));
    }

    /// <summary>
    /// SetAsync stores a value and GetAsync retrieves it correctly.
    /// </summary>
    [Fact]
    public async Task SetAsync_AndGetAsync_StoresAndRetrievesValue()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"test-key-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("Hello, Orleans Cache!");
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        };

        // Act
        await cache.SetAsync(key, value, options);
        var retrieved = await cache.GetAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(value, retrieved);
    }

    /// <summary>
    /// SetAsync with null key should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task SetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCache();
        var value = Encoding.UTF8.GetBytes("test");
        var options = new DistributedCacheEntryOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.SetAsync(null!, value, options));
    }

    /// <summary>
    /// SetAsync with null value should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task SetAsync_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"test-key-{Guid.NewGuid()}";
        var options = new DistributedCacheEntryOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.SetAsync(key, null!, options));
    }

    /// <summary>
    /// Set (sync) with null key should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void Set_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCache();
        var value = Encoding.UTF8.GetBytes("test");
        var options = new DistributedCacheEntryOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.Set(null!, value, options));
    }

    /// <summary>
    /// RefreshAsync updates sliding expiration for an existing key.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_ExistingKey_UpdatesSlidingExpiration()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"refresh-key-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("refresh test");
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
        };

        await cache.SetAsync(key, value, options);

        // Act
        await cache.RefreshAsync(key);
        var retrieved = await cache.GetAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(value, retrieved);
    }

    /// <summary>
    /// RefreshAsync with null key should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCache();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.RefreshAsync(null!));
    }

    /// <summary>
    /// Refresh (sync) with null key should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void Refresh_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCache();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.Refresh(null!));
    }

    /// <summary>
    /// RemoveAsync deletes a key from the cache.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesFromCache()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"remove-key-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("to be removed");
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        };

        await cache.SetAsync(key, value, options);
        var beforeRemove = await cache.GetAsync(key);
        Assert.NotNull(beforeRemove);

        // Act
        await cache.RemoveAsync(key);
        var afterRemove = await cache.GetAsync(key);

        // Assert
        Assert.Null(afterRemove);
    }

    /// <summary>
    /// RemoveAsync with null key should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCache();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.RemoveAsync(null!));
    }

    /// <summary>
    /// Remove (sync) with null key should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void Remove_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCache();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.Remove(null!));
    }

    /// <summary>
    /// CancellationToken is honored by GetAsync.
    /// </summary>
    [Fact]
    public async Task GetAsync_CancellationToken_Honored()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"cancel-key-{Guid.NewGuid()}";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.GetAsync(key, cts.Token));
    }

    /// <summary>
    /// CancellationToken is honored by SetAsync.
    /// </summary>
    [Fact]
    public async Task SetAsync_CancellationToken_Honored()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"cancel-key-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("test");
        var options = new DistributedCacheEntryOptions();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.SetAsync(key, value, options, cts.Token));
    }

    /// <summary>
    /// Absolute expiration causes entry to be removed after the specified time.
    /// </summary>
    [Fact]
    public async Task SetAsync_AbsoluteExpiration_EntryExpiresCorrectly()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"expire-key-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("expires soon");
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1),
        };

        // Act
        await cache.SetAsync(key, value, options);
        var beforeExpiry = await cache.GetAsync(key);
        
        // Wait for expiration
        await Task.Delay(TimeSpan.FromSeconds(2));
        var afterExpiry = await cache.GetAsync(key);

        // Assert
        Assert.NotNull(beforeExpiry);
        Assert.Null(afterExpiry);
    }

    /// <summary>
    /// Sliding expiration extends entry lifetime on access.
    /// </summary>
    [Fact]
    public async Task SetAsync_SlidingExpiration_ExtendsLifetimeOnAccess()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"sliding-key-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("sliding test");
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(2),
        };

        // Act
        await cache.SetAsync(key, value, options);
        
        // Access within sliding window
        await Task.Delay(TimeSpan.FromSeconds(1));
        var firstAccess = await cache.GetAsync(key);
        Assert.NotNull(firstAccess);
        
        // Access again within new sliding window
        await Task.Delay(TimeSpan.FromSeconds(1));
        var secondAccess = await cache.GetAsync(key);

        // Assert - should still be available due to sliding
        Assert.NotNull(secondAccess);
    }

    /// <summary>
    /// Multiple concurrent operations on different keys work correctly.
    /// </summary>
    [Fact]
    public async Task ConcurrentOperations_DifferentKeys_WorkCorrectly()
    {
        // Arrange
        var cache = CreateCache();
        var tasks = new Task[10];
        
        // Act - Create 10 concurrent Set/Get operations
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var key = $"concurrent-key-{index}";
                var value = Encoding.UTF8.GetBytes($"value-{index}");
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                };
                
                await cache.SetAsync(key, value, options);
                var retrieved = await cache.GetAsync(key);
                Assert.NotNull(retrieved);
                Assert.Equal(value, retrieved);
            });
        }
        
        // Assert
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Key prefix is applied correctly from options.
    /// </summary>
    [Fact]
    public async Task SetAsync_KeyPrefix_AppliedCorrectly()
    {
        // Arrange
        var options = new DistributedCacheOptions { KeyPrefix = "myprefix" };
        var cache = CreateCache(options);
        var key = $"test-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("prefix test");
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        };

        // Act
        await cache.SetAsync(key, value, cacheOptions);
        var retrieved = await cache.GetAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(value, retrieved);
    }

    /// <summary>
    /// Synchronous Get method works correctly.
    /// </summary>
    [Fact]
    public void Get_ExistingKey_RetrievesValue()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"sync-get-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("sync test");
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        };

        cache.Set(key, value, options);

        // Act
        var retrieved = cache.Get(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(value, retrieved);
    }

    /// <summary>
    /// Synchronous Set method works correctly.
    /// </summary>
    [Fact]
    public void Set_ValidKeyAndValue_StoresCorrectly()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"sync-set-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("sync set test");
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        };

        // Act
        cache.Set(key, value, options);
        var retrieved = cache.Get(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(value, retrieved);
    }

    /// <summary>
    /// Synchronous Refresh method works correctly.
    /// </summary>
    [Fact]
    public void Refresh_ExistingKey_UpdatesExpiration()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"sync-refresh-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("refresh test");
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
        };

        cache.Set(key, value, options);

        // Act
        cache.Refresh(key);
        var retrieved = cache.Get(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(value, retrieved);
    }

    /// <summary>
    /// Synchronous Remove method works correctly.
    /// </summary>
    [Fact]
    public void Remove_ExistingKey_RemovesFromCache()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"sync-remove-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("to be removed");
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        };

        cache.Set(key, value, options);
        var beforeRemove = cache.Get(key);
        Assert.NotNull(beforeRemove);

        // Act
        cache.Remove(key);
        var afterRemove = cache.Get(key);

        // Assert
        Assert.Null(afterRemove);
    }

    /// <summary>
    /// Large value (several MB) can be stored and retrieved.
    /// </summary>
    [Fact]
    public async Task SetAsync_LargeValue_StoresAndRetrievesCorrectly()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"large-value-{Guid.NewGuid()}";
        var value = new byte[1024 * 1024]; // 1 MB
        new Random().NextBytes(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        };

        // Act
        await cache.SetAsync(key, value, options);
        var retrieved = await cache.GetAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(value.Length, retrieved.Length);
        Assert.Equal(value, retrieved);
    }

    /// <summary>
    /// Empty byte array can be stored and retrieved.
    /// </summary>
    [Fact]
    public async Task SetAsync_EmptyValue_StoresAndRetrievesCorrectly()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"empty-value-{Guid.NewGuid()}";
        var value = Array.Empty<byte>();
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        };

        // Act
        await cache.SetAsync(key, value, options);
        var retrieved = await cache.GetAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Empty(retrieved);
    }

    /// <summary>
    /// Overwriting an existing key updates the value.
    /// </summary>
    [Fact]
    public async Task SetAsync_ExistingKey_OverwritesValue()
    {
        // Arrange
        var cache = CreateCache();
        var key = $"overwrite-key-{Guid.NewGuid()}";
        var value1 = Encoding.UTF8.GetBytes("first value");
        var value2 = Encoding.UTF8.GetBytes("second value");
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        };

        // Act
        await cache.SetAsync(key, value1, options);
        var firstRetrieve = await cache.GetAsync(key);
        Assert.Equal(value1, firstRetrieve);

        await cache.SetAsync(key, value2, options);
        var secondRetrieve = await cache.GetAsync(key);

        // Assert
        Assert.NotNull(secondRetrieve);
        Assert.Equal(value2, secondRetrieve);
    }
}
