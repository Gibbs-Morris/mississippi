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
///     Tests for <see cref="DepositToDestinationStep" />.
/// </summary>
public sealed class DepositToDestinationStepTests
{
    /// <summary>
    ///     Verifies a successful deposit returns a succeeded step result.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncWithValidInputSucceedsAsync()
    {
        Mock<IGenericAggregateGrain<BankAccountAggregate>> grain = new();
        grain.Setup(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok());
        Mock<IAggregateGrainFactory> factory = new();
        factory.Setup(f => f.GetGenericAggregate<BankAccountAggregate>("dest")).Returns(grain.Object);
        DepositToDestinationStep step = new(factory.Object);
        MoneyTransferSagaState state = new()
        {
            Input = new()
            {
                SourceAccountId = "source",
                DestinationAccountId = "dest",
                Amount = 50m,
            },
        };
        StepResult result = await step.ExecuteAsync(state, CancellationToken.None);
        result.Success.Should().BeTrue();
        grain.Verify(g => g.ExecuteAsync(It.IsAny<DepositFunds>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Verifies missing destination account fails the step.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncWithoutDestinationFailsAsync()
    {
        Mock<IAggregateGrainFactory> factory = new();
        DepositToDestinationStep step = new(factory.Object);
        MoneyTransferSagaState state = new()
        {
            Input = new()
            {
                SourceAccountId = "source",
                DestinationAccountId = string.Empty,
                Amount = 10m,
            },
        };
        StepResult result = await step.ExecuteAsync(state, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(AggregateErrorCodes.InvalidCommand);
    }

    /// <summary>
    ///     Verifies missing input fails the step.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsyncWithoutInputFailsAsync()
    {
        Mock<IAggregateGrainFactory> factory = new();
        DepositToDestinationStep step = new(factory.Object);
        MoneyTransferSagaState state = new();
        StepResult result = await step.ExecuteAsync(state, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(AggregateErrorCodes.InvalidState);
    }
}