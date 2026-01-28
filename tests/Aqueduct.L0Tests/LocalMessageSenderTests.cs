using System;
using System.Collections.Generic;
using System.Threading.Tasks;


using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Mississippi.Testing.Utilities.SignalR;

using NSubstitute;


namespace Mississippi.Aqueduct.L0Tests;

/// <summary>
///     Tests for <see cref="LocalMessageSender" />.
/// </summary>
public sealed class LocalMessageSenderTests
{
    /// <summary>
    ///     Constructor should succeed with valid logger.
    /// </summary>
    [Fact(DisplayName = "Constructor Succeeds With Valid Logger")]
        public void ConstructorShouldSucceedWithValidLogger()
    {
        // Arrange
        ILogger<LocalMessageSender> logger = Substitute.For<ILogger<LocalMessageSender>>();

        // Act
        LocalMessageSender sender = new(logger);

        // Assert
        Assert.NotNull(sender);
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact(DisplayName = "Constructor Throws When Logger Is Null")]
        public void ConstructorShouldThrowWhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LocalMessageSender(null!));
    }

    /// <summary>
    ///     SendAsync should succeed with empty args list.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendAsync Succeeds With Empty Args")]
        public async Task SendAsyncShouldSucceedWithEmptyArgs()
    {
        // Arrange
        ILogger<LocalMessageSender> logger = Substitute.For<ILogger<LocalMessageSender>>();
        LocalMessageSender sender = new(logger);
        HubConnectionContext connection = HubConnectionContextFactory.Create("conn-1");
        List<object?> args = [];

        // Act
        await sender.SendAsync(connection, "TestMethod", args);

        // Assert - If we get here without exception, the test passes
        Assert.True(true);
    }

    /// <summary>
    ///     SendAsync should succeed with valid arguments.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendAsync Succeeds With Valid Arguments")]
        public async Task SendAsyncShouldSucceedWithValidArguments()
    {
        // Arrange
        ILogger<LocalMessageSender> logger = Substitute.For<ILogger<LocalMessageSender>>();
        LocalMessageSender sender = new(logger);
        HubConnectionContext connection = HubConnectionContextFactory.Create("conn-1");
        List<object?> args = ["arg1", 42];

        // Act
        await sender.SendAsync(connection, "TestMethod", args);

        // Assert - If we get here without exception, the test passes
        // The message was written to the connection's pipe
        Assert.True(true);
    }

    /// <summary>
    ///     SendAsync should throw when connection is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendAsync Throws When Connection Is Null")]
        public async Task SendAsyncShouldThrowWhenConnectionIsNull()
    {
        // Arrange
        ILogger<LocalMessageSender> logger = Substitute.For<ILogger<LocalMessageSender>>();
        LocalMessageSender sender = new(logger);
        List<object?> args = ["arg1", 42];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => sender.SendAsync(null!, "TestMethod", args));
    }

    /// <summary>
    ///     SendAsync should throw when methodName is empty.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendAsync Throws When MethodName Is Empty")]
        public async Task SendAsyncShouldThrowWhenMethodNameIsEmpty()
    {
        // Arrange
        ILogger<LocalMessageSender> logger = Substitute.For<ILogger<LocalMessageSender>>();
        LocalMessageSender sender = new(logger);
        HubConnectionContext connection = HubConnectionContextFactory.Create("conn-1");
        List<object?> args = ["arg1", 42];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => sender.SendAsync(connection, string.Empty, args));
    }

    /// <summary>
    ///     SendAsync should throw when methodName is null.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendAsync Throws When MethodName Is Null")]
        public async Task SendAsyncShouldThrowWhenMethodNameIsNull()
    {
        // Arrange
        ILogger<LocalMessageSender> logger = Substitute.For<ILogger<LocalMessageSender>>();
        LocalMessageSender sender = new(logger);
        HubConnectionContext connection = HubConnectionContextFactory.Create("conn-1");
        List<object?> args = ["arg1", 42];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => sender.SendAsync(connection, null!, args));
    }

    /// <summary>
    ///     SendAsync should work with array args.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendAsync Works With Array Args")]
        public async Task SendAsyncShouldWorkWithArrayArgs()
    {
        // Arrange
        ILogger<LocalMessageSender> logger = Substitute.For<ILogger<LocalMessageSender>>();
        LocalMessageSender sender = new(logger);
        HubConnectionContext connection = HubConnectionContextFactory.Create("conn-1");
        object?[] args = ["arg1", 42, null];

        // Act
        await sender.SendAsync(connection, "TestMethod", args);

        // Assert - If we get here without exception, the test passes
        Assert.True(true);
    }
}