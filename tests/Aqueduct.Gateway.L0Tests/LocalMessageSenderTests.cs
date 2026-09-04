using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

using Mississippi.Testing.Utilities.SignalR;

using NSubstitute;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

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
    ///     SendAsync should complete when connection cancellation ends a pending write.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendAsync Completes When Connection Abort Cancels Pending Write")]
    public async Task SendAsyncShouldCompleteWhenConnectionAbortCancelsPendingWrite()
    {
        // Arrange
        using CancellationTokenSource connectionAborted = new();
        TaskCompletionSource writeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RecordingHubConnectionContext connection = new("conn-1", writeCompletion.Task, connectionAborted.Token);
        LocalMessageSender sender = new(Substitute.For<ILogger<LocalMessageSender>>());

        // Act
        Task sendTask = sender.SendAsync(connection, "TestMethod", []);

        // Assert
        Assert.Equal(connectionAborted.Token, connection.LastWriteCancellationToken);
        Assert.False(sendTask.IsCompleted);
        await connectionAborted.CancelAsync();
        await sendTask;
        Assert.True(sendTask.IsCompletedSuccessfully);
        Assert.True(connection.LastWriteCancellationToken.IsCancellationRequested);
        Assert.False(writeCompletion.Task.IsCompleted);
    }

    /// <summary>
    ///     SendAsync should forward the connection token and await the invocation write.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendAsync Forwards Connection Token And Awaits Write")]
    public async Task SendAsyncShouldForwardConnectionTokenAndAwaitWrite()
    {
        // Arrange
        using CancellationTokenSource connectionAborted = new();
        TaskCompletionSource writeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RecordingHubConnectionContext connection = new("conn-1", writeCompletion.Task, connectionAborted.Token);
        LocalMessageSender sender = new(Substitute.For<ILogger<LocalMessageSender>>());
        object?[] args = ["arg1", 42, null];

        // Act
        Task sendTask = sender.SendAsync(connection, "TestMethod", args);

        // Assert
        Assert.Equal(connectionAborted.Token, connection.LastWriteCancellationToken);
        InvocationMessage invocation = Assert.IsType<InvocationMessage>(connection.LastMessage);
        Assert.Equal("TestMethod", invocation.Target);
        Assert.Equal(args, invocation.Arguments);
        Assert.False(sendTask.IsCompleted);
        writeCompletion.SetResult();
        await sendTask;
        Assert.False(connection.LastWriteCancellationToken.IsCancellationRequested);
    }

    /// <summary>
    ///     SendAsync should propagate cancellation unrelated to an active connection.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "SendAsync Propagates Unrelated Cancellation")]
    public async Task SendAsyncShouldPropagateUnrelatedCancellation()
    {
        // Arrange
        using CancellationTokenSource connectionAborted = new();
        using CancellationTokenSource unrelatedCancellation = new();
        await unrelatedCancellation.CancelAsync();
        RecordingHubConnectionContext connection = new(
            "conn-1",
            Task.FromCanceled(unrelatedCancellation.Token),
            connectionAborted.Token);
        LocalMessageSender sender = new(Substitute.For<ILogger<LocalMessageSender>>());

        // Act
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sender.SendAsync(connection, "TestMethod", []));

        // Assert
        Assert.Equal(unrelatedCancellation.Token, exception.CancellationToken);
        Assert.False(connection.ConnectionAborted.IsCancellationRequested);
    }

    /// <summary>
    ///     SendAsync should propagate write failures regardless of connection cancellation.
    /// </summary>
    /// <param name="isConnectionAborted">Whether the connection has already been aborted.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory(DisplayName = "SendAsync Propagates Write Failures")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAsyncShouldPropagateWriteFailures(
        bool isConnectionAborted
    )
    {
        // Arrange
        using CancellationTokenSource connectionAborted = new();
        if (isConnectionAborted)
        {
            await connectionAborted.CancelAsync();
        }

        InvalidOperationException failure = new("Write failed.");
        RecordingHubConnectionContext connection = new("conn-1", Task.FromException(failure), connectionAborted.Token);
        LocalMessageSender sender = new(Substitute.For<ILogger<LocalMessageSender>>());

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync(connection, "TestMethod", []));

        // Assert
        Assert.Same(failure, exception);
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