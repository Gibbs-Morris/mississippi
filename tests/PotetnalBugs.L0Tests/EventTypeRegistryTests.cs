using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates that <see cref="IEventTypeRegistry" /> duplicate registration bugs are now fixed.
/// </summary>
public sealed class EventTypeRegistryTests
{
    /// <summary>
    ///     FIXED: Registering the same CLR event type under two different names no longer creates
    ///     an asymmetric registry state. The second registration is now silently ignored.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: EventTypeRegistry.Register previously allowed adding a second event name " +
        "for the same CLR type, creating asymmetric state. The second registration is now " +
        "silently ignored, keeping registry consistent.",
        FilePath = "src/EventSourcing.Aggregates/EventTypeRegistry.cs",
        LineNumbers = "45-48",
        Severity = "Medium",
        Category = "DeveloperExperience")]
    public void RegisteringSameTypeWithDifferentNamesIgnoresSecondRegistration()
    {
        // Arrange
        ServiceCollection services = new();
        _ = services.AddAggregateSupport();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEventTypeRegistry sut = provider.GetRequiredService<IEventTypeRegistry>();

        const string firstName = "APP.AGG.EVENT_ONE.V1";
        const string secondName = "APP.AGG.EVENT_ALIAS.V1";

        // Act
        sut.Register(firstName, typeof(TestEvent));
        sut.Register(secondName, typeof(TestEvent));

        // Assert - second registration is ignored; only first mapping exists
        _ = Assert.Single(sut.RegisteredTypes);
        Assert.Equal(typeof(TestEvent), sut.ResolveType(firstName));
        Assert.Equal(firstName, sut.ResolveName(typeof(TestEvent)));
    }

    /// <summary>
    ///     FIXED: ScanAssembly now accurately reports the count of newly registered types,
    ///     excluding duplicates that were ignored by Register.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: EventTypeRegistry.ScanAssembly previously overstated registration count " +
        "by including duplicate storage names. ScanAssembly now uses TryRegister and reports " +
        "only the count of types actually added.",
        FilePath = "src/EventSourcing.Aggregates/EventTypeRegistry.cs",
        LineNumbers = "71-86",
        Severity = "Low",
        Category = "LogicError")]
    public void ScanAssemblyAccuratelyReportsRegistrationCountWithDuplicates()
    {
        // Arrange
        ServiceCollection services = new();
        _ = services.AddAggregateSupport();
        using ServiceProvider provider = services.BuildServiceProvider();
        IEventTypeRegistry sut = provider.GetRequiredService<IEventTypeRegistry>();

        // Act
        int scannedCount = sut.ScanAssembly(typeof(DuplicateEventNameOne).Assembly);

        // Assert - scan count now matches actual registry count
        Assert.Equal(1, scannedCount);
        _ = Assert.Single(sut.RegisteredTypes);
    }

    private sealed record TestEvent(
        string Value
    );

    [EventStorageName("POTBUG", "EVENT", "DUPLICATE", 1)]
    private sealed record DuplicateEventNameOne(
        string Value
    );

    [EventStorageName("POTBUG", "EVENT", "DUPLICATE", 1)]
    private sealed record DuplicateEventNameTwo(
        string Value
    );
}
