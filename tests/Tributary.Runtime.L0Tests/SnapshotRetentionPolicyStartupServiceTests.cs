using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotRetentionPolicyStartupService" />.
/// </summary>
public sealed class SnapshotRetentionPolicyStartupServiceTests
{
    private sealed class CapturingLogger : ILogger<SnapshotRetentionPolicyStartupService>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(
            TState state
        )
            where TState : notnull =>
            NoopScope.Instance;

        public bool IsEnabled(
            LogLevel logLevel
        ) =>
            true;

        void ILogger.Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    /// <summary>
    ///     Verifies that the configured policy is logged without a save-all warning when save-all is disabled.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task StartAsyncLogsConfiguredPolicyWithoutSaveAllWarning()
    {
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = 20,
        };
        options.StateTypeOverrides["STATE.V1"] = 10;
        CapturingLogger logger = new();
        SnapshotRetentionPolicyStartupService service = new(Options.Create(options), logger);
        await service.StartAsync(CancellationToken.None);
        (LogLevel Level, string Message) entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("default modulus 20", entry.Message, StringComparison.Ordinal);
        Assert.Contains("1 state overrides", entry.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that enabling save-all logs the operational warning in addition to the policy.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task StartAsyncLogsWarningWhenSaveAllIsEnabled()
    {
        SnapshotRetentionOptions options = new()
        {
            ShouldPersistAllSnapshots = true,
        };
        CapturingLogger logger = new();
        SnapshotRetentionPolicyStartupService service = new(Options.Create(options), logger);
        await service.StartAsync(CancellationToken.None);
        Assert.Equal(2, logger.Entries.Count);
        Assert.Contains(
            logger.Entries,
            entry => (entry.Level == LogLevel.Information) &&
                     entry.Message.Contains("persist-all True", StringComparison.Ordinal));
        Assert.Contains(
            logger.Entries,
            entry => (entry.Level == LogLevel.Warning) &&
                     entry.Message.Contains("persist every reconstructed snapshot", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies cancellation prevents startup policy logging.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task StartAsyncThrowsWhenCancellationIsRequestedBeforeLoggingPolicy()
    {
        CancellationToken cancellationToken = new(true);
        CapturingLogger logger = new();
        SnapshotRetentionPolicyStartupService service = new(Options.Create(new SnapshotRetentionOptions()), logger);
        OperationCanceledException exception =
            await Assert.ThrowsAsync<OperationCanceledException>(() => service.StartAsync(cancellationToken));
        Assert.Equal(cancellationToken, exception.CancellationToken);
        Assert.Empty(logger.Entries);
    }

    /// <summary>
    ///     Verifies the no-op shutdown completes even when cancellation is already requested.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task StopAsyncCompletesWhenCancellationIsAlreadyRequested()
    {
        CancellationToken cancellationToken = new(true);
        CapturingLogger logger = new();
        SnapshotRetentionPolicyStartupService service = new(Options.Create(new SnapshotRetentionOptions()), logger);
        await service.StopAsync(cancellationToken);
        Assert.Empty(logger.Entries);
    }
}