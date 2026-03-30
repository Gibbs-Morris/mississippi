namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Executes the minimal emulator-backed Cosmos provider calibration scenarios required for Increment 04.
/// </summary>
[Collection(CosmosReplicaSinkCalibrationL2TestSuite.Name)]
#pragma warning disable CA1515 // xUnit test classes must be public for the existing repo test pattern
public sealed class CosmosReplicaSinkCalibrationHarnessTests
#pragma warning restore CA1515
{
    private readonly CosmosReplicaSinkCalibrationAppHostFixture fixture;

    private readonly ITestOutputHelper testOutputHelper;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkCalibrationHarnessTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Cosmos emulator fixture.</param>
    /// <param name="testOutputHelper">The xUnit output helper.</param>
    public CosmosReplicaSinkCalibrationHarnessTests(
        CosmosReplicaSinkCalibrationAppHostFixture fixture,
        ITestOutputHelper testOutputHelper
    )
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(testOutputHelper);
        this.fixture = fixture;
        this.testOutputHelper = testOutputHelper;
    }

    /// <summary>
    ///     Verifies the single-sink replay-backlog scenario against the emulator-backed provider path.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public Task ReplicaSinkCosmosCalibrationShouldExecuteSingleSinkReplayBacklog() =>
        ExecuteScenarioAsync(CosmosReplicaSinkCalibrationScenario.CreateSingleSinkReplayBacklog());

    /// <summary>
    ///     Verifies the two-sink fan-out replay-backlog scenario against the emulator-backed provider path.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public Task ReplicaSinkCosmosCalibrationShouldExecuteTwoSinkFanOutReplayBacklog() =>
        ExecuteScenarioAsync(CosmosReplicaSinkCalibrationScenario.CreateTwoSinkFanOutReplayBacklog());

    /// <summary>
    ///     Verifies the replay-plus-live scenario against the emulator-backed provider path.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public Task ReplicaSinkCosmosCalibrationShouldExecuteReplayBacklogFollowedByLiveWrites() =>
        ExecuteScenarioAsync(CosmosReplicaSinkCalibrationScenario.CreateReplayBacklogFollowedByLiveWrites());

    private async Task ExecuteScenarioAsync(
        CosmosReplicaSinkCalibrationScenario scenario
    )
    {
        CosmosReplicaSinkCalibrationWorkloadRunner runner = fixture.CreateWorkloadRunner();
        CosmosReplicaSinkCalibrationScenarioMetrics metrics = await runner.RunAsync(scenario);

        testOutputHelper.WriteLine(JsonSerializer.Serialize(metrics));

        metrics.ScenarioName.Should().Be(scenario.Name);
        metrics.EntityCount.Should().Be(scenario.EntityCount);
        metrics.SinkCount.Should().Be(scenario.SinkCount);
        metrics.PayloadSizeClass.Should().Be(scenario.PayloadSizeClass.ToString());
        metrics.WriteCount.Should().Be(scenario.TotalWriteCount);
        metrics.InFlightBudget.Should().Be(scenario.InFlightBudget);
        metrics.ReplayElapsedMilliseconds.Should().BeGreaterThan(0);
        if (scenario.LiveWriteCount == 0)
        {
            metrics.LiveWriteElapsedMilliseconds.Should().BeNull();
        }
        else
        {
            metrics.LiveWriteElapsedMilliseconds.Should().NotBeNull();
            metrics.LiveWriteElapsedMilliseconds.Should().BeGreaterThan(0);
        }

        metrics.RetryCount.Should().Be(0);
        metrics.DeadLetterCount.Should().Be(0);
        metrics.Targets.Should().HaveCount(scenario.SinkCount);
        metrics.Targets.Should().OnlyContain(target => target.TargetExists);
        metrics.Targets.Should().OnlyContain(target => target.WriteCount == (scenario.EntityCount + scenario.LiveWriteCount));
        metrics.Targets.Should().OnlyContain(target => target.LatestSourcePosition == (scenario.EntityCount + scenario.LiveWriteCount));
        metrics.Targets.Should().OnlyContain(target => target.SampleCommittedSourcePosition == (scenario.EntityCount + scenario.LiveWriteCount));
    }
}
