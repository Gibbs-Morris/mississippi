using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;

using Moq;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.MoneyTransferSaga;
using Spring.Domain.Aggregates.MoneyTransferSaga.Steps;


namespace Spring.Domain.L0Tests.Aggregates.MoneyTransferSaga.Steps;

/// <summary>
///     Tests for <see cref="WithdrawFromSourceStep" />.
/// </summary>
public sealed class WithdrawFromSourceStepTests
{
    /// <summary>
    ///     Verifies compensation failure surfaces as a failed result.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CompensateAsyncWithFailureReturnsFailedAsync()
    {
        Mock<IGenericAggregateGrain<BankAccountAggregate>> grain = new();
        grain.Setup(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("ERR", "deposit failed"));
        Mock<IAggregateGrainFactory> factory = new();
        factory.Setup(f => f.GetGenericAggregate<BankAccountAggregate>("source")).Returns(grain.Object);
        WithdrawFromSourceStep step = new(factory.Object);
        MoneyTransferSagaState state = new()
        {
            Input = new()
            {
                SourceAccountId = "source",
                DestinationAccountId = "dest",
                Amount = 25m,
            },
        };
        CompensationResult result = await step.CompensateAsync(state, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ERR");
        grain.Verify(g => g.ExecuteAsync(It.IsAny<DepositFunds>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Verifies compensation skips when input is missing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CompensateAsyncWithoutInputSkipsAsync()
    {
        Mock<IAggregateGrainFactory> factory = new();
        WithdrawFromSourceStep step = new(factory.Object);
        MoneyTransferSagaState state = new();
        CompensationResult result = await step.CompensateAsync(state, CancellationToken.None);
        result.Skipped.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies same-account transfers are rejected.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncWithSameAccountFailsAsync()
    {
        Mock<IAggregateGrainFactory> factory = new();
        WithdrawFromSourceStep step = new(factory.Object);
        MoneyTransferSagaState state = new()
        {
            Input = new()
            {
                SourceAccountId = "acc-1",
                DestinationAccountId = "acc-1",
                Amount = 10m,
            },
        };
        StepResult result = await step.ExecuteAsync(state, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(AggregateErrorCodes.InvalidCommand);
    }

    /// <summary>
    ///     Verifies a successful withdrawal returns a succeeded step result.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncWithValidInputSucceedsAsync()
    {
        Mock<IGenericAggregateGrain<BankAccountAggregate>> grain = new();
        grain.Setup(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok());
        Mock<IAggregateGrainFactory> factory = new();
        factory.Setup(f => f.GetGenericAggregate<BankAccountAggregate>("source")).Returns(grain.Object);
        WithdrawFromSourceStep step = new(factory.Object);
        MoneyTransferSagaState state = new()
        {
            Input = new()
            {
                SourceAccountId = "source",
                DestinationAccountId = "dest",
                Amount = 25m,
            },
        };
        StepResult result = await step.ExecuteAsync(state, CancellationToken.None);
        result.Success.Should().BeTrue();
        grain.Verify(g => g.ExecuteAsync(It.IsAny<WithdrawFunds>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Verifies missing input fails the step.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncWithoutInputFailsAsync()
    {
        Mock<IAggregateGrainFactory> factory = new();
        WithdrawFromSourceStep step = new(factory.Object);
        MoneyTransferSagaState state = new();
        StepResult result = await step.ExecuteAsync(state, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(AggregateErrorCodes.InvalidState);
    }
}