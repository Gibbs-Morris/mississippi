using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.AspNetCore.Orleans.Authentication;
using Mississippi.AspNetCore.Orleans.Authentication.Grains;
using Mississippi.AspNetCore.Orleans.Authentication.Options;
using Mississippi.AspNetCore.Orleans.L0Tests.Infrastructure;
using Orleans;
using Orleans.TestingHost;
using Xunit;

namespace Mississippi.AspNetCore.Orleans.L0Tests.Authentication;

/// <summary>
/// Comprehensive tests for <see cref="OrleansTicketStore"/> covering all interface methods and edge cases.
/// </summary>
[Collection(ClusterTestSuite.Name)]
public sealed class OrleansTicketStoreTests
{
    private readonly TestCluster cluster = TestClusterAccess.Cluster;

    /// <summary>
    /// Creates a new OrleansTicketStore instance for testing with specified options.
    /// </summary>
    private OrleansTicketStore CreateStore(TicketStoreOptions? options = null)
    {
        var opts = options ?? new TicketStoreOptions { KeyPrefix = "test" };
        var logger = cluster.ServiceProvider.GetService(typeof(ILogger<OrleansTicketStore>)) as ILogger<OrleansTicketStore>;
        var serializer = new TicketSerializer();
        return new OrleansTicketStore(
            logger ?? throw new InvalidOperationException("Logger not found"),
            cluster.Client,
            Options.Create(opts),
            serializer,
            TimeProvider.System);
    }

    /// <summary>
    /// Creates a sample authentication ticket for testing.
    /// </summary>
    private AuthenticationTicket CreateTestTicket(string username = "testuser")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, $"{username}@test.com"),
            new Claim(ClaimTypes.Role, "User"),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
        };

        return new AuthenticationTicket(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// RetrieveAsync returns null for a key that doesn't exist.
    /// </summary>
    [Fact]
    public async Task RetrieveAsync_NonExistentKey_ReturnsNull()
    {
        // Arrange
        var store = CreateStore();
        var key = $"nonexistent-{Guid.NewGuid()}";

        // Act
        var result = await store.RetrieveAsync(key);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// RetrieveAsync with null key should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task RetrieveAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.RetrieveAsync(null!));
    }

    /// <summary>
    /// StoreAsync stores a ticket and RetrieveAsync retrieves it correctly.
    /// </summary>
    [Fact]
    public async Task StoreAsync_AndRetrieveAsync_StoresAndRetrievesTicket()
    {
        // Arrange
        var store = CreateStore();
        var ticket = CreateTestTicket("storetest");

        // Act
        var key = await store.StoreAsync(ticket);
        var retrieved = await store.RetrieveAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(ticket.Principal.Identity?.Name, retrieved.Principal.Identity?.Name);
        Assert.Equal(ticket.AuthenticationScheme, retrieved.AuthenticationScheme);
    }

    /// <summary>
    /// StoreAsync with null ticket should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task StoreAsync_NullTicket_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.StoreAsync(null!));
    }

    /// <summary>
    /// RenewAsync updates an existing ticket.
    /// </summary>
    [Fact]
    public async Task RenewAsync_ExistingTicket_UpdatesTicket()
    {
        // Arrange
        var store = CreateStore();
        var ticket = CreateTestTicket("renewtest");

        var key = await store.StoreAsync(ticket);
        var originalRetrieved = await store.RetrieveAsync(key);
        Assert.NotNull(originalRetrieved);

        // Create updated ticket
        var updatedTicket = CreateTestTicket("renewtest-updated");

        // Act
        await store.RenewAsync(key, updatedTicket);
        var renewedRetrieved = await store.RetrieveAsync(key);

        // Assert
        Assert.NotNull(renewedRetrieved);
        Assert.Equal(updatedTicket.Principal.Identity?.Name, renewedRetrieved.Principal.Identity?.Name);
    }

    /// <summary>
    /// RenewAsync with null key should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task RenewAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        var ticket = CreateTestTicket();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.RenewAsync(null!, ticket));
    }

    /// <summary>
    /// RenewAsync with null ticket should throw ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task RenewAsync_NullTicket_ThrowsArgumentNullException()
    {
        // Arrange
        var store = CreateStore();
        var key = $"test-key-{Guid.NewGuid()}";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.RenewAsync(key, null!));
    }

    /// <summary>
    /// RemoveAsync deletes a ticket from the store.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_ExistingTicket_RemovesFromStore()
    {
        // Arrange
        var store = CreateStore();
        var ticket = CreateTestTicket("removetest");

        var key = await store.StoreAsync(ticket);
        var beforeRemove = await store.RetrieveAsync(key);
        Assert.NotNull(beforeRemove);

        // Act
        await store.RemoveAsync(key);
        var afterRemove = await store.RetrieveAsync(key);

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
        var store = CreateStore();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.RemoveAsync(null!));
    }

    /// <summary>
    /// Ticket with expiration is removed after expiry time.
    /// </summary>
    [Fact]
    public async Task StoreAsync_TicketExpires_AfterExpiryTime()
    {
        // Arrange
        var store = CreateStore();
        var claims = new[] { new Claim(ClaimTypes.Name, "expiretest") };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(1),
        };
        var ticket = new AuthenticationTicket(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme);

        // Act
        var key = await store.StoreAsync(ticket);
        var beforeExpiry = await store.RetrieveAsync(key);
        Assert.NotNull(beforeExpiry);

        // Wait for expiration
        await Task.Delay(TimeSpan.FromSeconds(2));
        var afterExpiry = await store.RetrieveAsync(key);

        // Assert
        Assert.Null(afterExpiry);
    }

    /// <summary>
    /// Multiple concurrent operations on different keys work correctly.
    /// </summary>
    [Fact]
    public async Task ConcurrentOperations_DifferentTickets_WorkCorrectly()
    {
        // Arrange
        var store = CreateStore();
        var tasks = new Task[10];
        
        // Act - Create 10 concurrent Store/Retrieve operations
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var ticket = CreateTestTicket($"concurrent-user-{index}");
                
                var key = await store.StoreAsync(ticket);
                var retrieved = await store.RetrieveAsync(key);
                Assert.NotNull(retrieved);
                Assert.Equal($"concurrent-user-{index}", retrieved.Principal.Identity?.Name);
            });
        }
        
        // Assert
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Key prefix is applied correctly from options.
    /// </summary>
    [Fact]
    public async Task StoreAsync_KeyPrefix_AppliedCorrectly()
    {
        // Arrange
        var options = new TicketStoreOptions { KeyPrefix = "authprefix" };
        var store = CreateStore(options);
        var ticket = CreateTestTicket("prefixtest");

        // Act
        var key = await store.StoreAsync(ticket);
        var retrieved = await store.RetrieveAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(ticket.Principal.Identity?.Name, retrieved.Principal.Identity?.Name);
        Assert.StartsWith("authprefix:", key);
    }

    /// <summary>
    /// Ticket with multiple claims is serialized and deserialized correctly.
    /// </summary>
    [Fact]
    public async Task StoreAsync_TicketWithMultipleClaims_SerializesCorrectly()
    {
        // Arrange
        var store = CreateStore();
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "multiclaim"),
            new Claim(ClaimTypes.Email, "multiclaim@test.com"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "User"),
            new Claim("CustomClaim", "CustomValue"),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc = DateTimeOffset.UtcNow,
        };
        var ticket = new AuthenticationTicket(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme);

        // Act
        var key = await store.StoreAsync(ticket);
        var retrieved = await store.RetrieveAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(5, retrieved.Principal.Claims.Count());
        Assert.Contains(retrieved.Principal.Claims, c => c.Type == "CustomClaim" && c.Value == "CustomValue");
    }

    /// <summary>
    /// Ticket with authentication properties is preserved correctly.
    /// </summary>
    [Fact]
    public async Task StoreAsync_TicketWithProperties_PreservesProperties()
    {
        // Arrange
        var store = CreateStore();
        var claims = new[] { new Claim(ClaimTypes.Name, "proptest") };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
        };
        properties.Items["custom-key"] = "custom-value";
        var ticket = new AuthenticationTicket(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme);

        // Act
        var key = await store.StoreAsync(ticket);
        var retrieved = await store.RetrieveAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.True(retrieved.Properties.IsPersistent);
        Assert.True(retrieved.Properties.AllowRefresh);
        Assert.True(retrieved.Properties.Items.ContainsKey("custom-key"));
        Assert.Equal("custom-value", retrieved.Properties.Items["custom-key"]);
    }

    /// <summary>
    /// StoreAsync generates unique keys for different tickets.
    /// </summary>
    [Fact]
    public async Task StoreAsync_MultipleTickets_GeneratesUniqueKeys()
    {
        // Arrange
        var store = CreateStore();
        var ticket1 = CreateTestTicket("user1");
        var ticket2 = CreateTestTicket("user2");

        // Act
        var key1 = await store.StoreAsync(ticket1);
        var key2 = await store.StoreAsync(ticket2);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    /// <summary>
    /// RenewAsync on non-existent key creates a new ticket.
    /// </summary>
    [Fact]
    public async Task RenewAsync_NonExistentKey_CreatesNewTicket()
    {
        // Arrange
        var store = CreateStore();
        var key = $"nonexistent-{Guid.NewGuid()}";
        var ticket = CreateTestTicket("renewnew");

        // Act
        await store.RenewAsync(key, ticket);
        var retrieved = await store.RetrieveAsync(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(ticket.Principal.Identity?.Name, retrieved.Principal.Identity?.Name);
    }

    /// <summary>
    /// RemoveAsync on non-existent key does not throw.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_NonExistentKey_DoesNotThrow()
    {
        // Arrange
        var store = CreateStore();
        var key = $"nonexistent-{Guid.NewGuid()}";

        // Act & Assert (should not throw)
        await store.RemoveAsync(key);
    }
}
