using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.SignalR.Grains;

using Moq;

using Orleans.Runtime;


namespace Mississippi.EventSourcing.UxProjections.SignalR.L0Tests.Grains;

/// <summary>
///     Unit tests for <see cref="UxClientGrain" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections SignalR")]
[AllureSubSuite("UxClientGrain")]
public sealed class UxClientGrainTests
{
    private const string HubName = "TestHub";
    private const string ConnectionId = "connection-abc123";
    private const string ServerId = "server-001";

    /// <summary>
    ///     ConnectAsync should set connection state.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConnectAsyncSetsConnectionState()
    {
        // Arrange
        UxClientGrain grain = CreateGrain($"{HubName}:{ConnectionId}");

        // Act
        await grain.ConnectAsync(HubName, ServerId);

        // Assert
        string? serverId = await grain.GetServerIdAsync();
        Assert.Equal(ServerId, serverId);
    }

    /// <summary>
    ///     DisconnectAsync should clear connection state.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DisconnectAsyncClearsConnectionState()
    {
        // Arrange
        UxClientGrain grain = CreateGrain($"{HubName}:{ConnectionId}");
        await grain.ConnectAsync(HubName, ServerId);

        // Act
        await grain.DisconnectAsync();

        // Assert
        string? serverId = await grain.GetServerIdAsync();
        Assert.Null(serverId);
    }

    /// <summary>
    ///     GetServerIdAsync returns null when not connected.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetServerIdAsyncReturnsNullWhenNotConnected()
    {
        // Arrange
        UxClientGrain grain = CreateGrain($"{HubName}:{ConnectionId}");

        // Act
        string? serverId = await grain.GetServerIdAsync();

        // Assert
        Assert.Null(serverId);
    }

    /// <summary>
    ///     ConnectAsync should throw when hubName is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConnectAsyncThrowsWhenHubNameIsNull()
    {
        // Arrange
        UxClientGrain grain = CreateGrain($"{HubName}:{ConnectionId}");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.ConnectAsync(null!, ServerId));
    }

    /// <summary>
    ///     ConnectAsync should throw when serverId is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConnectAsyncThrowsWhenServerIdIsNull()
    {
        // Arrange
        UxClientGrain grain = CreateGrain($"{HubName}:{ConnectionId}");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.ConnectAsync(HubName, null!));
    }

    /// <summary>
    ///     SendMessageAsync should not throw for valid inputs.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendMessageAsyncDoesNotThrowForValidInputs()
    {
        // Arrange
        UxClientGrain grain = CreateGrain($"{HubName}:{ConnectionId}");

        // Act
        Func<Task> actionAsync = () => grain.SendMessageAsync("TestMethod", ["arg1", 42]);

        // Assert
        Exception? exception = await Record.ExceptionAsync(actionAsync);
        Assert.Null(exception);
    }

    /// <summary>
    ///     SendMessageAsync should throw when methodName is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendMessageAsyncThrowsWhenMethodNameIsNull()
    {
        // Arrange
        UxClientGrain grain = CreateGrain($"{HubName}:{ConnectionId}");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.SendMessageAsync(null!, []));
    }

    /// <summary>
    ///     SendMessageAsync should throw when args is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SendMessageAsyncThrowsWhenArgsIsNull()
    {
        // Arrange
        UxClientGrain grain = CreateGrain($"{HubName}:{ConnectionId}");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => grain.SendMessageAsync("TestMethod", null!));
    }

    /// <summary>
    ///     OnActivateAsync should complete successfully.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnActivateAsyncCompletes()
    {
        // Arrange
        UxClientGrain grain = CreateGrain($"{HubName}:{ConnectionId}");

        // Act
        Func<Task> actionAsync = () => grain.OnActivateAsync(CancellationToken.None);

        // Assert
        Exception? exception = await Record.ExceptionAsync(actionAsync);
        Assert.Null(exception);
    }

    /// <summary>
    ///     Constructor should throw when grainContext is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenGrainContextIsNull()
    {
        // Arrange
        Mock<ILogger<UxClientGrain>> logger = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxClientGrain(null!, logger.Object));
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        Mock<IGrainContext> context = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxClientGrain(context.Object, null!));
    }

    private static UxClientGrain CreateGrain(string grainKey)
    {
        Mock<IGrainContext> context = new();
        Mock<ILogger<UxClientGrain>> logger = new();

        GrainId grainId = GrainId.Create("ux-client", grainKey);
        context.SetupGet(c => c.GrainId).Returns(grainId);

        return new UxClientGrain(context.Object, logger.Object);
    }
}
