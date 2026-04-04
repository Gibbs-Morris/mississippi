using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;

using Orleans;
using Orleans.Runtime;
using Orleans.Timers;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="GrainReminderManager" />.
/// </summary>
public sealed class GrainReminderManagerTests
{
    private static Mock<IGrainBase> CreateGrain(
        GrainId? grainId = null
    )
    {
        Mock<IGrainContext> grainContextMock = new();
        grainContextMock.Setup(context => context.GrainId).Returns(grainId ?? GrainId.Create("test", "entity-123"));
        Mock<IGrainBase> grainMock = new();
        grainMock.Setup(grain => grain.GrainContext).Returns(grainContextMock.Object);
        return grainMock;
    }

    /// <summary>
    ///     Verifies the constructor rejects a missing reminder registry.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenReminderRegistryMissing()
    {
        Assert.Throws<ArgumentNullException>(() => new GrainReminderManager(null!));
    }

    /// <summary>
    ///     Verifies matching reminders are returned by name.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetReminderAsyncReturnsMatchingReminder()
    {
        Mock<IReminderRegistry> reminderRegistryMock = new();
        Mock<IGrainReminder> expectedReminderMock = new();
        expectedReminderMock.SetupGet(reminder => reminder.ReminderName).Returns(SagaReminderNames.Recovery);
        Mock<IGrainReminder> otherReminderMock = new();
        otherReminderMock.SetupGet(reminder => reminder.ReminderName).Returns("other-reminder");
        Mock<IGrainBase> grainMock = CreateGrain();
        reminderRegistryMock.Setup(registry => registry.GetReminders(It.IsAny<GrainId>()))
            .ReturnsAsync(new List<IGrainReminder>
            {
                otherReminderMock.Object,
                expectedReminderMock.Object,
            });
        GrainReminderManager manager = new(reminderRegistryMock.Object);

        IGrainReminder? reminder = await manager.GetReminderAsync(grainMock.Object, SagaReminderNames.Recovery);

        Assert.Same(expectedReminderMock.Object, reminder);
    }

    /// <summary>
    ///     Verifies missing reminders return <c>null</c>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetReminderAsyncReturnsNullWhenReminderMissing()
    {
        Mock<IReminderRegistry> reminderRegistryMock = new();
        Mock<IGrainBase> grainMock = CreateGrain();
        reminderRegistryMock.Setup(registry => registry.GetReminders(It.IsAny<GrainId>()))
            .ReturnsAsync(new List<IGrainReminder>());
        GrainReminderManager manager = new(reminderRegistryMock.Object);

        IGrainReminder? reminder = await manager.GetReminderAsync(grainMock.Object, SagaReminderNames.Recovery);

        Assert.Null(reminder);
    }

    /// <summary>
    ///     Verifies reminder registration delegates to the Orleans registry.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RegisterOrUpdateReminderAsyncDelegatesToReminderRegistry()
    {
        Mock<IReminderRegistry> reminderRegistryMock = new();
        Mock<IGrainBase> grainMock = CreateGrain();
        GrainReminderManager manager = new(reminderRegistryMock.Object);
        TimeSpan dueTime = TimeSpan.FromSeconds(10);
        TimeSpan period = TimeSpan.FromMinutes(2);

        await manager.RegisterOrUpdateReminderAsync(grainMock.Object, SagaReminderNames.Recovery, dueTime, period);

        reminderRegistryMock.Verify(
            registry => registry.RegisterOrUpdateReminder(
                grainMock.Object.GrainContext.GrainId,
                SagaReminderNames.Recovery,
                dueTime,
                period),
            Times.Once);
    }

    /// <summary>
    ///     Verifies reminder removal delegates to the Orleans registry.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task UnregisterReminderAsyncDelegatesToReminderRegistry()
    {
        Mock<IReminderRegistry> reminderRegistryMock = new();
        Mock<IGrainBase> grainMock = CreateGrain();
        Mock<IGrainReminder> reminderMock = new();
        GrainReminderManager manager = new(reminderRegistryMock.Object);

        await manager.UnregisterReminderAsync(grainMock.Object, reminderMock.Object);

        reminderRegistryMock.Verify(
            registry => registry.UnregisterReminder(grainMock.Object.GrainContext.GrainId, reminderMock.Object),
            Times.Once);
    }
}