using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Time.Testing;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Effects;
using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Aggregates.TransactionInvestigationQueue;
using Spring.Domain.Aggregates.TransactionInvestigationQueue.Commands;


namespace Spring.Domain.L0Tests.Aggregates.BankAccount.Effects;

/// <summary>
///     Unit tests for <see cref="HighValueTransactionEffect" />.
/// </summary>
/// <remarks>
///     <para>
///         These tests verify the AML threshold detection and cross-aggregate command dispatch.
///         The effect flags deposits exceeding £10,000 by dispatching a <see cref="FlagTransaction" />
///         command to the <see cref="TransactionInvestigationQueueAggregate" />.
///     </para>
/// </remarks>
[AllureParentSuite("Spring Domain")]
[AllureSuite("Aggregates")]
[AllureSubSuite("BankAccount Effects")]
public sealed class HighValueTransactionEffectTests
{
    private static readonly DateTimeOffset TestTimestamp = new(2025, 1, 15, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///     Verifies that deposits exceeding the AML threshold dispatch a FlagTransaction command.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("AML Threshold")]
    public async Task DepositAboveThresholdShouldDispatchFlagTransactionCommandAsync()
    {
        // Arrange
        EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate> harness =
            EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>.Create()
                .WithGrainKey("acc-123")
                .WithAggregateGrainResponse<TransactionInvestigationQueueAggregate>("global", OperationResult.Ok());
        HighValueTransactionEffect effect = harness.Build((
            factory,
            context,
            logger
        ) => new(factory, context, logger));
        FundsDeposited eventData = new()
        {
            Amount = 15_000m,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 15_000m,
            IsOpen = true,
        };

        // Act
        await harness.InvokeAsync(effect, eventData, state);

        // Assert
        harness.DispatchedCommands.Should().HaveCount(1);
        FlagTransaction command = harness.DispatchedCommands.ShouldHaveDispatched<FlagTransaction>();
        command.AccountId.Should().Be("acc-123");
        command.Amount.Should().Be(15_000m);
    }

    /// <summary>
    ///     Verifies that deposits at or below the AML threshold do not trigger flagging.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("AML Threshold")]
    public async Task DepositAtThresholdShouldNotTriggerFlaggingAsync()
    {
        // Arrange
        EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate> harness =
            EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>.Create()
                .WithGrainKey("acc-123");
        HighValueTransactionEffect effect = harness.Build((
            factory,
            context,
            logger
        ) => new(factory, context, logger));
        FundsDeposited eventData = new()
        {
            Amount = HighValueTransactionEffect.AmlThreshold,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 10_000m,
            IsOpen = true,
        };

        // Act
        await harness.InvokeAsync(effect, eventData, state);

        // Assert
        harness.DispatchedCommands.ShouldHaveNoDispatches();
    }

    /// <summary>
    ///     Verifies that deposits below the AML threshold do not trigger flagging.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("AML Threshold")]
    public async Task DepositBelowThresholdShouldNotTriggerFlaggingAsync()
    {
        // Arrange
        EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate> harness =
            EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>.Create()
                .WithGrainKey("acc-123");
        HighValueTransactionEffect effect = harness.Build((
            factory,
            context,
            logger
        ) => new(factory, context, logger));
        FundsDeposited eventData = new()
        {
            Amount = 5_000m,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 5_000m,
            IsOpen = true,
        };

        // Act
        await harness.InvokeAsync(effect, eventData, state);

        // Assert
        harness.DispatchedCommands.ShouldHaveNoDispatches();
    }

    /// <summary>
    ///     Verifies that large deposits just above threshold are correctly flagged.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("AML Threshold")]
    public async Task DepositJustAboveThresholdShouldTriggerFlaggingAsync()
    {
        // Arrange
        decimal justAboveThreshold = HighValueTransactionEffect.AmlThreshold + 0.01m;
        EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate> harness =
            EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>.Create()
                .WithGrainKey("acc-789")
                .WithAggregateGrainResponse<TransactionInvestigationQueueAggregate>("global", OperationResult.Ok());
        HighValueTransactionEffect effect = harness.Build((
            factory,
            context,
            logger
        ) => new(factory, context, logger));
        FundsDeposited eventData = new()
        {
            Amount = justAboveThreshold,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = justAboveThreshold,
            IsOpen = true,
        };

        // Act
        await harness.InvokeAsync(effect, eventData, state);

        // Assert
        harness.DispatchedCommands.Should().HaveCount(1);
        FlagTransaction command = harness.DispatchedCommands.ShouldHaveDispatched<FlagTransaction>();
        command.Amount.Should().Be(justAboveThreshold);
    }

    /// <summary>
    ///     Verifies that the FlagTransaction command includes a timestamp.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("Cross-Aggregate Dispatch")]
    public async Task FlagTransactionShouldIncludeTimestampAsync()
    {
        // Arrange
        FakeTimeProvider fakeTimeProvider = new(TestTimestamp);
        EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate> harness =
            EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>.Create()
                .WithGrainKey("acc-timestamp")
                .WithAggregateGrainResponse<TransactionInvestigationQueueAggregate>("global", OperationResult.Ok());
        HighValueTransactionEffect effect = harness.Build((
            factory,
            context,
            logger
        ) => new(factory, context, logger, fakeTimeProvider));
        FundsDeposited eventData = new()
        {
            Amount = 100_000m,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 100_000m,
            IsOpen = true,
        };

        // Act
        await harness.InvokeAsync(effect, eventData, state);

        // Assert
        FlagTransaction command = harness.DispatchedCommands.ShouldHaveDispatched<FlagTransaction>();
        command.Timestamp.Should().Be(TestTimestamp);
    }

    /// <summary>
    ///     Verifies that the FlagTransaction command is dispatched to the global investigation queue.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("Cross-Aggregate Dispatch")]
    public async Task ShouldDispatchToGlobalInvestigationQueueAsync()
    {
        // Arrange
        EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate> harness =
            EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>.Create()
                .WithGrainKey("acc-456")
                .WithAggregateGrainResponse<TransactionInvestigationQueueAggregate>("global", OperationResult.Ok());
        HighValueTransactionEffect effect = harness.Build((
            factory,
            context,
            logger
        ) => new(factory, context, logger));
        FundsDeposited eventData = new()
        {
            Amount = 50_000m,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 50_000m,
            IsOpen = true,
        };

        // Act
        await harness.InvokeAsync(effect, eventData, state);

        // Assert
        (Type AggregateType, string EntityId, object Command) dispatch =
            harness.DispatchedCommands.ShouldHaveDispatchedTo<TransactionInvestigationQueueAggregate>("global");
        dispatch.EntityId.Should().Be("global");
    }

    /// <summary>
    ///     Verifies that the effect handles a failed FlagTransaction response gracefully.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("Error Handling")]
    public async Task ShouldHandleFailedFlagTransactionGracefullyAsync()
    {
        // Arrange
        EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate> harness =
            EffectTestHarness<HighValueTransactionEffect, FundsDeposited, BankAccountAggregate>.Create()
                .WithGrainKey("acc-999")
                .WithAggregateGrainResponse<TransactionInvestigationQueueAggregate>(
                    "global",
                    OperationResult.Fail("QUEUE_FULL", "Investigation queue is full"));
        HighValueTransactionEffect effect = harness.Build((
            factory,
            context,
            logger
        ) => new(factory, context, logger));
        FundsDeposited eventData = new()
        {
            Amount = 20_000m,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 20_000m,
            IsOpen = true,
        };

        // Act - should not throw, effect logs the failure instead
        await harness.InvokeAsync(effect, eventData, state);

        // Assert - command was still dispatched even though it failed
        harness.DispatchedCommands.Should().HaveCount(1);
    }
}