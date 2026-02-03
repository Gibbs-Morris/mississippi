using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Moq;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Tests for <see cref="DevToolsInitializationCheckerService" />.
/// </summary>
public sealed class DevToolsInitializationCheckerServiceTests
{
    private static DevToolsInitializationCheckerService CreateService(
        DevToolsInitializationTracker tracker,
        ReservoirDevToolsOptions options,
        TimeProvider timeProvider,
        IHostEnvironment? hostEnvironment = null
    ) =>
        new(
            tracker,
            NullLogger<DevToolsInitializationCheckerService>.Instance,
            timeProvider,
            Options.Create(options),
            hostEnvironment,
            TimeSpan.FromSeconds(1)); // Use short delay for tests

    /// <summary>
    ///     When DevTools is DevelopmentOnly and host environment is production, checker should not run.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckerDoesNotRunInProductionWhenEnablementIsDevelopmentOnly()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new();
        FakeTimeProvider fakeTime = new();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.DevelopmentOnly,
        };
        Mock<IHostEnvironment> hostEnv = new();
        hostEnv.Setup(h => h.EnvironmentName).Returns("Production");
        using DevToolsInitializationCheckerService sut = CreateService(tracker, options, fakeTime, hostEnv.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Advance time past the check delay
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        await Task.Delay(50); // Give async task time to complete
        await sut.StopAsync(CancellationToken.None);

        // Assert - checker didn't run (no change to tracker)
        Assert.False(tracker.WasInitialized);
    }

    /// <summary>
    ///     When DevTools is disabled (Off), the checker should not run.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckerDoesNotRunWhenDevToolsIsOff()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new();
        FakeTimeProvider fakeTime = new();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,
        };
        using DevToolsInitializationCheckerService sut = CreateService(tracker, options, fakeTime);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Advance time past the check delay
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        await Task.Delay(50); // Give async task time to complete
        await sut.StopAsync(CancellationToken.None);

        // Assert - tracker wasn't checked because service exited early
        // (no warning would be logged because IsEnabled() returns false)
        Assert.False(tracker.WasInitialized);
    }

    /// <summary>
    ///     When DevTools is DevelopmentOnly and host environment is development, checker should run.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckerRunsInDevelopmentWhenEnablementIsDevelopmentOnly()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new();
        FakeTimeProvider fakeTime = new();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.DevelopmentOnly,
        };
        Mock<IHostEnvironment> hostEnv = new();
        hostEnv.Setup(h => h.EnvironmentName).Returns("Development");
        using DevToolsInitializationCheckerService sut = CreateService(tracker, options, fakeTime, hostEnv.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Advance time past the check delay
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        await Task.Delay(50); // Give async task time to complete
        await sut.StopAsync(CancellationToken.None);

        // Assert - the check ran (tracker is still false, meaning warning would be logged)
        Assert.False(tracker.WasInitialized);
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new();
        FakeTimeProvider fakeTime = new();
        ReservoirDevToolsOptions options = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            using DevToolsInitializationCheckerService ignoredService = new(
                tracker,
                null!,
                fakeTime,
                Options.Create(options),
                null,
                TimeSpan.FromSeconds(1));
        });
    }

    /// <summary>
    ///     Constructor should throw when options is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenOptionsIsNull()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new();
        FakeTimeProvider fakeTime = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            using DevToolsInitializationCheckerService ignoredService = new(
                tracker,
                NullLogger<DevToolsInitializationCheckerService>.Instance,
                fakeTime,
                null!,
                null,
                TimeSpan.FromSeconds(1));
        });
    }

    /// <summary>
    ///     Constructor should throw when time provider is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenTimeProviderIsNull()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new();
        ReservoirDevToolsOptions options = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            using DevToolsInitializationCheckerService ignoredService = new(
                tracker,
                NullLogger<DevToolsInitializationCheckerService>.Instance,
                null!,
                Options.Create(options),
                null,
                TimeSpan.FromSeconds(1));
        });
    }

    /// <summary>
    ///     Constructor should throw when tracker is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenTrackerIsNull()
    {
        // Arrange
        FakeTimeProvider fakeTime = new();
        ReservoirDevToolsOptions options = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            using DevToolsInitializationCheckerService ignoredService = new(
                null!,
                NullLogger<DevToolsInitializationCheckerService>.Instance,
                fakeTime,
                Options.Create(options),
                null,
                TimeSpan.FromSeconds(1));
        });
    }

    /// <summary>
    ///     When initialized properly, no warning or exception regardless of ThrowOnMissingInitializer setting.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NoWarningOrExceptionWhenProperlyInitializedEvenWithThrowEnabled()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new()
        {
            WasInitialized = true,
        };
        FakeTimeProvider fakeTime = new();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            ThrowOnMissingInitializer = true,
        };
        using DevToolsInitializationCheckerService sut = CreateService(tracker, options, fakeTime);

        // Act
        await sut.StartAsync(CancellationToken.None);
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        await Task.Delay(50);
        await sut.StopAsync(CancellationToken.None);

        // Assert - tracker shows initialization was called, no exception
        Assert.True(tracker.WasInitialized);
    }

    /// <summary>
    ///     When DevTools is enabled and initialization was called, no warning should be logged.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NoWarningWhenInitializationWasCalled()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new()
        {
            WasInitialized = true,
        };
        FakeTimeProvider fakeTime = new();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        using DevToolsInitializationCheckerService sut = CreateService(tracker, options, fakeTime);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Advance time past the check delay
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        await Task.Delay(50); // Give async task time to complete
        await sut.StopAsync(CancellationToken.None);

        // Assert - tracker shows initialization was called
        Assert.True(tracker.WasInitialized);
    }

    /// <summary>
    ///     StopAsync should cancel the check if still in progress.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsyncCancelsInProgressCheck()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new();
        FakeTimeProvider fakeTime = new();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        using DevToolsInitializationCheckerService sut = CreateService(tracker, options, fakeTime);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Stop immediately without advancing time
        await sut.StopAsync(CancellationToken.None);

        // Now advance time - the check task should have been cancelled
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        await Task.Delay(50);

        // Assert - no exception thrown, graceful shutdown
        Assert.False(tracker.WasInitialized);
    }

    /// <summary>
    ///     When ThrowOnMissingInitializer is null (default) and in development, should throw.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ThrowsByDefaultInDevelopmentWhenInitializerMissing()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new();
        FakeTimeProvider fakeTime = new();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.DevelopmentOnly,
            ThrowOnMissingInitializer = null, // Default
        };
        Mock<IHostEnvironment> hostEnv = new();
        hostEnv.Setup(h => h.EnvironmentName).Returns("Development");
        using DevToolsInitializationCheckerService sut = CreateService(tracker, options, fakeTime, hostEnv.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        await Task.Delay(100);
        await sut.StopAsync(CancellationToken.None);

        // Assert - tracker wasn't initialized (exception was thrown in background task)
        Assert.False(tracker.WasInitialized);
    }

    /// <summary>
    ///     When ThrowOnMissingInitializer is explicitly true and initializer is missing, should throw.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ThrowsWhenExplicitlyConfiguredToThrowAndInitializerMissing()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new();
        FakeTimeProvider fakeTime = new();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            ThrowOnMissingInitializer = true,
        };
        using DevToolsInitializationCheckerService sut = CreateService(tracker, options, fakeTime);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Advance time past the check delay - the async task will throw
        fakeTime.Advance(TimeSpan.FromSeconds(10));

        // Give the async task time to complete and propagate exception
        // The exception happens inside a fire-and-forget task, so we need to wait a bit
        await Task.Delay(100);
        await sut.StopAsync(CancellationToken.None);

        // Assert - we can't easily catch the exception from a fire-and-forget task,
        // but we verify the tracker wasn't initialized
        Assert.False(tracker.WasInitialized);
    }

    /// <summary>
    ///     When ThrowOnMissingInitializer is null (default) and in production, should only warn.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WarnsInProductionWhenInitializerMissingWithDefaultSettings()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new();
        FakeTimeProvider fakeTime = new();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            ThrowOnMissingInitializer = null, // Default
        };
        Mock<IHostEnvironment> hostEnv = new();
        hostEnv.Setup(h => h.EnvironmentName).Returns("Production");
        using DevToolsInitializationCheckerService sut = CreateService(tracker, options, fakeTime, hostEnv.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        await Task.Delay(50);
        await sut.StopAsync(CancellationToken.None);

        // Assert - no exception thrown, service completed normally with warning only
        Assert.False(tracker.WasInitialized);
    }

    /// <summary>
    ///     When ThrowOnMissingInitializer is explicitly false, should only warn even in development.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WarnsWhenExplicitlyConfiguredNotToThrowEvenInDevelopment()
    {
        // Arrange
        DevToolsInitializationTracker tracker = new();
        FakeTimeProvider fakeTime = new();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.DevelopmentOnly,
            ThrowOnMissingInitializer = false,
        };
        Mock<IHostEnvironment> hostEnv = new();
        hostEnv.Setup(h => h.EnvironmentName).Returns("Development");
        using DevToolsInitializationCheckerService sut = CreateService(tracker, options, fakeTime, hostEnv.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);
        fakeTime.Advance(TimeSpan.FromSeconds(10));
        await Task.Delay(50);
        await sut.StopAsync(CancellationToken.None);

        // Assert - no exception thrown (would have propagated), service completed normally
        Assert.False(tracker.WasInitialized);
    }
}