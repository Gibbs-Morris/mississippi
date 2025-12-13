namespace Mississippi.AspNetCore.Orleans.L0Tests.Authentication;

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.AspNetCore.Orleans.Authentication;
using Mississippi.AspNetCore.Orleans.Authentication.Options;
using Moq;
using Orleans;
using Xunit;

/// <summary>
/// Tests for <see cref="OrleansTicketStore"/>.
/// </summary>
public sealed class OrleansTicketStoreTests
{
    /// <summary>
    /// StoreAsync with null ticket should throw ArgumentNullException.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task StoreAsync_NullTicket_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OrleansTicketStore>>();
        var mockCluster = new Mock<IClusterClient>();
        var options = Options.Create(new OrleansTicketStoreOptions());
        var mockSerializer = new Mock<ITicketSerializer>();
        var store = new OrleansTicketStore(mockLogger.Object, mockCluster.Object, options, mockSerializer.Object, TimeProvider.System);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.StoreAsync(null!));
    }

    /// <summary>
    /// RenewAsync with null key should throw ArgumentNullException.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RenewAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OrleansTicketStore>>();
        var mockCluster = new Mock<IClusterClient>();
        var options = Options.Create(new OrleansTicketStoreOptions());
        var mockSerializer = new Mock<ITicketSerializer>();
        var store = new OrleansTicketStore(mockLogger.Object, mockCluster.Object, options, mockSerializer.Object, TimeProvider.System);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), "scheme");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.RenewAsync(null!, ticket));
    }

    /// <summary>
    /// RetrieveAsync with null key should throw ArgumentNullException.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RetrieveAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<OrleansTicketStore>>();
        var mockCluster = new Mock<IClusterClient>();
        var options = Options.Create(new OrleansTicketStoreOptions());
        var mockSerializer = new Mock<ITicketSerializer>();
        var store = new OrleansTicketStore(mockLogger.Object, mockCluster.Object, options, mockSerializer.Object, TimeProvider.System);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.RetrieveAsync(null!));
    }
}
