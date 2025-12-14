using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.AspNetCore.Orleans.L1Tests.Infrastructure;
using Mississippi.AspNetCore.Orleans.OutputCaching;
using Mississippi.AspNetCore.Orleans.OutputCaching.Grains;
using Mississippi.AspNetCore.Orleans.OutputCaching.Options;
using Orleans;
using Orleans.TestingHost;
using Xunit;

namespace Mississippi.AspNetCore.Orleans.L1Tests.OutputCaching;

/// <summary>
/// Comprehensive L1 integration tests for <see cref="OrleansOutputCacheStore"/> using Orleans TestCluster.
/// </summary>
[Collection(ClusterTestSuite.Name)]
public sealed class OrleansOutputCacheStoreTests
{
    private readonly TestCluster cluster;

    public OrleansOutputCacheStoreTests(ClusterFixture fixture)
    {
        cluster = fixture.Cluster;
    }

    /// <summary>
    /// Creates a new OrleansOutputCacheStore instance for testing with specified options.
    /// </summary>
    private OrleansOutputCacheStore CreateStore(OrleansOutputCacheOptions? options = null)
    {
        var opts = options ?? new OrleansOutputCacheOptions { KeyPrefix = "test" };
        var logger = cluster.ServiceProvider.GetService(typeof(ILogger<OrleansOutputCacheStore>)) as ILogger<OrleansOutputCacheStore>;
        return new OrleansOutputCacheStore(
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
        var store = CreateStore();
        var key = $"nonexistent-{Guid.NewGuid()}";

        // Act
        var result = await store.GetAsync(key, CancellationToken.None);

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
        var store = CreateStore();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetAsync(null!, CancellationToken.None));
    }

    /// <summary>
    /// SetAsync stores a value and GetAsync retrieves it correctly.
    /// </summary>
    [Fact]
    public async Task SetAsync_AndGetAsync_StoresAndRetrievesValue()
    {
        // Arrange
        var store = CreateStore();
        var key = $"test-key-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("Output cache test");
        var validFor = TimeSpan.FromMinutes(5);

        // Act
        await store.SetAsync(key, value, null, validFor, CancellationToken.None);
        var retrieved = await store.GetAsync(key, CancellationToken.None);

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
        var store = CreateStore();
        var value = Encoding.UTF8.GetBytes("test");
        var validFor = TimeSpan.FromMinutes(5);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetAsync(null!, value, null, validFor, CancellationToken.None));
    }

    /// <summary>
    /// SetAsync with null value should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task SetAsync_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        var key = $"test-key-{Guid.NewGuid()}";
        var validFor = TimeSpan.FromMinutes(5);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetAsync(key, null!, null, validFor, CancellationToken.None));
    }

    /// <summary>
    /// EvictByTagAsync with null tag should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task EvictByTagAsync_NullTag_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.EvictByTagAsync(null!, CancellationToken.None));
    }

    /// <summary>
    /// EvictByTagAsync removes entries with specified tag.
    /// </summary>
    [Fact]
    public async Task EvictByTagAsync_WithTag_RemovesTaggedEntries()
    {
        // Arrange
        var store = CreateStore();
        var key1 = $"tagged-key1-{Guid.NewGuid()}";
        var key2 = $"tagged-key2-{Guid.NewGuid()}";
        var key3 = $"untagged-key-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("tagged value");
        var tag = "test-tag";
        var validFor = TimeSpan.FromMinutes(5);

        // Set entries with and without tags
        await store.SetAsync(key1, value, new[] { tag }, validFor, CancellationToken.None);
        await store.SetAsync(key2, value, new[] { tag }, validFor, CancellationToken.None);
        await store.SetAsync(key3, value, null, validFor, CancellationToken.None);

        // Act
        await store.EvictByTagAsync(tag, CancellationToken.None);

        // Assert
        var result1 = await store.GetAsync(key1, CancellationToken.None);
        var result2 = await store.GetAsync(key2, CancellationToken.None);
        var result3 = await store.GetAsync(key3, CancellationToken.None);

        Assert.Null(result1);
        Assert.Null(result2);
        Assert.NotNull(result3); // Untagged entry should remain
    }

    /// <summary>
    /// CancellationToken is honored by GetAsync.
    /// </summary>
    [Fact]
    public async Task GetAsync_CancellationToken_Honored()
    {
        // Arrange
        var store = CreateStore();
        var key = $"cancel-key-{Guid.NewGuid()}";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await store.GetAsync(key, cts.Token));
    }

    /// <summary>
    /// CancellationToken is honored by SetAsync.
    /// </summary>
    [Fact]
    public async Task SetAsync_CancellationToken_Honored()
    {
        // Arrange
        var store = CreateStore();
        var key = $"cancel-key-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("test");
        var validFor = TimeSpan.FromMinutes(5);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await store.SetAsync(key, value, null, validFor, cts.Token));
    }

    /// <summary>
    /// Entry expires after the specified time.
    /// </summary>
    [Fact]
    public async Task SetAsync_EntryExpires_AfterValidFor()
    {
        // Arrange
        var store = CreateStore();
        var key = $"expire-key-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("expires soon");
        var validFor = TimeSpan.FromSeconds(1);

        // Act
        await store.SetAsync(key, value, null, validFor, CancellationToken.None);
        var beforeExpiry = await store.GetAsync(key, CancellationToken.None);
        
        // Wait for expiration
        await Task.Delay(TimeSpan.FromSeconds(2));
        var afterExpiry = await store.GetAsync(key, CancellationToken.None);

        // Assert
        Assert.NotNull(beforeExpiry);
        Assert.Null(afterExpiry);
    }

    /// <summary>
    /// Multiple concurrent operations on different keys work correctly.
    /// </summary>
    [Fact]
    public async Task ConcurrentOperations_DifferentKeys_WorkCorrectly()
    {
        // Arrange
        var store = CreateStore();
        var tasks = new Task[10];
        
        // Act - Create 10 concurrent Set/Get operations
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var key = $"concurrent-key-{index}";
                var value = Encoding.UTF8.GetBytes($"value-{index}");
                var validFor = TimeSpan.FromMinutes(5);
                
                await store.SetAsync(key, value, null, validFor, CancellationToken.None);
                var retrieved = await store.GetAsync(key, CancellationToken.None);
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
        var options = new OrleansOutputCacheOptions { KeyPrefix = "outputprefix" };
        var store = CreateStore(options);
        var key = $"test-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("prefix test");
        var validFor = TimeSpan.FromMinutes(5);

        // Act
        await store.SetAsync(key, value, null, validFor, CancellationToken.None);
        var retrieved = await store.GetAsync(key, CancellationToken.None);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(value, retrieved);
    }

    /// <summary>
    /// Large value (several MB) can be stored and retrieved.
    /// </summary>
    [Fact]
    public async Task SetAsync_LargeValue_StoresAndRetrievesCorrectly()
    {
        // Arrange
        var store = CreateStore();
        var key = $"large-value-{Guid.NewGuid()}";
        var value = new byte[1024 * 1024]; // 1 MB
        new Random().NextBytes(value);
        var validFor = TimeSpan.FromMinutes(5);

        // Act
        await store.SetAsync(key, value, null, validFor, CancellationToken.None);
        var retrieved = await store.GetAsync(key, CancellationToken.None);

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
        var store = CreateStore();
        var key = $"empty-value-{Guid.NewGuid()}";
        var value = Array.Empty<byte>();
        var validFor = TimeSpan.FromMinutes(5);

        // Act
        await store.SetAsync(key, value, null, validFor, CancellationToken.None);
        var retrieved = await store.GetAsync(key, CancellationToken.None);

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
        var store = CreateStore();
        var key = $"overwrite-key-{Guid.NewGuid()}";
        var value1 = Encoding.UTF8.GetBytes("first value");
        var value2 = Encoding.UTF8.GetBytes("second value");
        var validFor = TimeSpan.FromMinutes(5);

        // Act
        await store.SetAsync(key, value1, null, validFor, CancellationToken.None);
        var firstRetrieve = await store.GetAsync(key, CancellationToken.None);
        Assert.Equal(value1, firstRetrieve);

        await store.SetAsync(key, value2, null, validFor, CancellationToken.None);
        var secondRetrieve = await store.GetAsync(key, CancellationToken.None);

        // Assert
        Assert.NotNull(secondRetrieve);
        Assert.Equal(value2, secondRetrieve);
    }

    /// <summary>
    /// Multiple tags can be assigned to an entry.
    /// </summary>
    [Fact]
    public async Task SetAsync_MultipleTags_StoresAndEvictsCorrectly()
    {
        // Arrange
        var store = CreateStore();
        var key = $"multi-tag-key-{Guid.NewGuid()}";
        var value = Encoding.UTF8.GetBytes("multi-tagged value");
        var tags = new[] { "tag1", "tag2", "tag3" };
        var validFor = TimeSpan.FromMinutes(5);

        // Act
        await store.SetAsync(key, value, tags, validFor, CancellationToken.None);
        var beforeEvict = await store.GetAsync(key, CancellationToken.None);
        Assert.NotNull(beforeEvict);

        // Evict by one of the tags
        await store.EvictByTagAsync("tag2", CancellationToken.None);
        var afterEvict = await store.GetAsync(key, CancellationToken.None);

        // Assert
        Assert.Null(afterEvict);
    }
}
