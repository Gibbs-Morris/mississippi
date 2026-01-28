using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;

using Spring.Domain.Aggregates.BankAccount;


namespace Spring.Domain.L0Tests.Architecture;

/// <summary>
///     Architecture tests validating that domain types have required attributes.
/// </summary>
/// <remarks>
///     <para>
///         These tests enforce that events, aggregates, and projections have the required
///         attributes for Orleans serialization, storage naming, and type aliasing.
///     </para>
///     <para>
///         Required attributes by type:
///     </para>
///     <list type="bullet">
///         <item>
///             <term>Events</term>
///             <description>
///                 <c>[EventStorageName]</c>, <c>[GenerateSerializer]</c>, <c>[Alias]</c>
///             </description>
///         </item>
///         <item>
///             <term>Aggregates</term>
///             <description>
///                 <c>[BrookName]</c>, <c>[SnapshotStorageName]</c>, <c>[GenerateSerializer]</c>, <c>[Alias]</c>
///             </description>
///         </item>
///         <item>
///             <term>Projections</term>
///             <description>
///                 <c>[BrookName]</c>, <c>[SnapshotStorageName]</c>, <c>[GenerateSerializer]</c>, <c>[Alias]</c>
///             </description>
///         </item>
///         <item>
///             <term>Commands</term>
///             <description>
///                 <c>[GenerateSerializer]</c>, <c>[Alias]</c>
///             </description>
///         </item>
///     </list>
/// </remarks>
[AllureParentSuite("Spring Domain")]
[AllureSuite("Architecture")]
[AllureSubSuite("Required Attributes")]
public sealed class RequiredAttributeTests
{
    private static readonly Assembly DomainAssembly = typeof(BankAccountAggregate).Assembly;

    /// <summary>
    ///     Gets all aggregate types in the domain assembly.
    /// </summary>
    private static IEnumerable<Type> AggregateTypes =>
        DomainAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Aggregates", StringComparison.Ordinal) == true)
            .Where(t => t.Name.EndsWith("Aggregate", StringComparison.Ordinal))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(IsNotOrleansGeneratedType);

    /// <summary>
    ///     Gets all command types in the domain assembly.
    /// </summary>
    private static IEnumerable<Type> CommandTypes =>
        DomainAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Commands", StringComparison.Ordinal) == true)
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(IsNotOrleansGeneratedType);

    /// <summary>
    ///     Gets all event types in the domain assembly.
    /// </summary>
    private static IEnumerable<Type> EventTypes =>
        DomainAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Events", StringComparison.Ordinal) == true)
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(IsNotOrleansGeneratedType);

    /// <summary>
    ///     Gets all projection types in the domain assembly.
    /// </summary>
    private static IEnumerable<Type> ProjectionTypes =>
        DomainAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Projections", StringComparison.Ordinal) == true)
            .Where(t => t.Name.EndsWith("Projection", StringComparison.Ordinal))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(IsNotOrleansGeneratedType);

    /// <summary>
    ///     Checks if a type has an attribute by name (handles source-generated attributes).
    /// </summary>
    private static bool HasAttribute(
        Type type,
        string attributeName
    ) =>
        type.GetCustomAttributes()
            .Any(a => (a.GetType().Name == attributeName) ||
                      (a.GetType().Name == attributeName.Replace("Attribute", string.Empty, StringComparison.Ordinal)));

    /// <summary>
    ///     Filters out Orleans source-generated types (codecs, copiers, activators).
    /// </summary>
    private static bool IsNotOrleansGeneratedType(
        Type t
    ) =>
        (t.Namespace?.StartsWith("OrleansCodeGen", StringComparison.Ordinal) != true) &&
        !t.Name.StartsWith("Codec_", StringComparison.Ordinal) &&
        !t.Name.StartsWith("Copier_", StringComparison.Ordinal) &&
        !t.Name.StartsWith("Activator_", StringComparison.Ordinal);

    /// <summary>
    ///     All aggregates should have Alias attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Aggregates")]
    public void AggregatesShouldHaveAliasAttribute()
    {
        // Arrange & Act
        List<Type> violations = AggregateTypes
            .Where(aggregateType => aggregateType.GetCustomAttribute<AliasAttribute>() is null)
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all aggregates must have [Alias] attribute for stable serialization. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All aggregates should have BrookName attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Aggregates")]
    public void AggregatesShouldHaveBrookNameAttribute()
    {
        // Arrange & Act
        List<Type> violations = AggregateTypes
            .Where(aggregateType => aggregateType.GetCustomAttribute<BrookNameAttribute>() is null)
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all aggregates must have [BrookName] attribute. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All aggregates should have GenerateSerializer attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Aggregates")]
    public void AggregatesShouldHaveGenerateSerializerAttribute()
    {
        // Arrange & Act
        List<Type> violations = AggregateTypes
            .Where(aggregateType => !HasAttribute(aggregateType, "GenerateSerializerAttribute"))
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all aggregates must have [GenerateSerializer] attribute. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All aggregates should have SnapshotStorageName attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Aggregates")]
    public void AggregatesShouldHaveSnapshotStorageNameAttribute()
    {
        // Arrange & Act
        List<Type> violations = AggregateTypes
            .Where(aggregateType => aggregateType.GetCustomAttribute<SnapshotStorageNameAttribute>() is null)
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all aggregates must have [SnapshotStorageName] attribute. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All commands should have Alias attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Commands")]
    public void CommandsShouldHaveAliasAttribute()
    {
        // Arrange & Act
        List<Type> violations = CommandTypes
            .Where(commandType => commandType.GetCustomAttribute<AliasAttribute>() is null)
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all commands must have [Alias] attribute for stable serialization. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All commands should have GenerateSerializer attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Commands")]
    public void CommandsShouldHaveGenerateSerializerAttribute()
    {
        // Arrange & Act
        List<Type> violations = CommandTypes
            .Where(commandType => !HasAttribute(commandType, "GenerateSerializerAttribute"))
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all commands must have [GenerateSerializer] attribute. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     Event Alias values should follow naming convention.
    /// </summary>
    [Fact]
    [AllureFeature("Events")]
    public void EventAliasValuesShouldFollowNamingConvention()
    {
        // Arrange & Act
        List<string> violations = EventTypes
            .Select(eventType => (eventType, alias: eventType.GetCustomAttribute<AliasAttribute>()))
            .Where(x => x.alias is not null && !x.alias.Alias.Contains(x.eventType.Name, StringComparison.Ordinal))
            .Select(x => $"{x.eventType.Name}: Alias '{x.alias!.Alias}' does not contain type name")
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because event Alias values should contain the type name. Violations: {0}",
                string.Join("; ", violations));
    }

    /// <summary>
    ///     All events should have Alias attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Events")]
    public void EventsShouldHaveAliasAttribute()
    {
        // Arrange & Act
        List<Type> violations = EventTypes.Where(eventType => eventType.GetCustomAttribute<AliasAttribute>() is null)
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all events must have [Alias] attribute for stable serialization. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All events should have EventStorageName attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Events")]
    public void EventsShouldHaveEventStorageNameAttribute()
    {
        // Arrange & Act
        List<Type> violations = EventTypes
            .Where(eventType => eventType.GetCustomAttribute<EventStorageNameAttribute>() is null)
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all events must have [EventStorageName] attribute. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All events should have GenerateSerializer attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Events")]
    public void EventsShouldHaveGenerateSerializerAttribute()
    {
        // Arrange & Act
        List<Type> violations = EventTypes.Where(eventType => !HasAttribute(eventType, "GenerateSerializerAttribute"))
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all events must have [GenerateSerializer] attribute. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All projections should have Alias attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Projections")]
    public void ProjectionsShouldHaveAliasAttribute()
    {
        // Arrange & Act
        List<Type> violations = ProjectionTypes
            .Where(projectionType => projectionType.GetCustomAttribute<AliasAttribute>() is null)
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all projections must have [Alias] attribute for stable serialization. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All projections should have BrookName attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Projections")]
    public void ProjectionsShouldHaveBrookNameAttribute()
    {
        // Arrange & Act
        List<Type> violations = ProjectionTypes
            .Where(projectionType => projectionType.GetCustomAttribute<BrookNameAttribute>() is null)
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all projections must have [BrookName] attribute. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All projections should have GenerateSerializer attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Projections")]
    public void ProjectionsShouldHaveGenerateSerializerAttribute()
    {
        // Arrange & Act
        List<Type> violations = ProjectionTypes
            .Where(projectionType => !HasAttribute(projectionType, "GenerateSerializerAttribute"))
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all projections must have [GenerateSerializer] attribute. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All projections should have SnapshotStorageName attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Projections")]
    public void ProjectionsShouldHaveSnapshotStorageNameAttribute()
    {
        // Arrange & Act
        List<Type> violations = ProjectionTypes.Where(projectionType =>
                projectionType.GetCustomAttribute<SnapshotStorageNameAttribute>() is null)
            .ToList();

        // Assert
        violations.Should()
            .BeEmpty(
                "because all projections must have [SnapshotStorageName] attribute. " + "Missing on: {0}",
                string.Join(", ", violations.Select(t => t.Name)));
    }
}