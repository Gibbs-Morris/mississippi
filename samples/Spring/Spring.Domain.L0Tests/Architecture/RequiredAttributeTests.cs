using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Allure.Xunit.Attributes;

using FluentAssertions;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;

using Spring.Domain.Aggregates.BankAccount;

using Xunit;


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
    ///     Gets all event types in the domain assembly.
    /// </summary>
    private static IEnumerable<Type> EventTypes =>
        DomainAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Events", StringComparison.Ordinal) == true)
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(IsNotOrleansGeneratedType);

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
    ///     Gets all projection types in the domain assembly.
    /// </summary>
    private static IEnumerable<Type> ProjectionTypes =>
        DomainAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Projections", StringComparison.Ordinal) == true)
            .Where(t => t.Name.EndsWith("Projection", StringComparison.Ordinal))
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
    ///     Filters out Orleans source-generated types (codecs, copiers, activators).
    /// </summary>
    private static bool IsNotOrleansGeneratedType(Type t) =>
        t.Namespace?.StartsWith("OrleansCodeGen", StringComparison.Ordinal) != true &&
        !t.Name.StartsWith("Codec_", StringComparison.Ordinal) &&
        !t.Name.StartsWith("Copier_", StringComparison.Ordinal) &&
        !t.Name.StartsWith("Activator_", StringComparison.Ordinal);

    /// <summary>
    ///     All events should have EventStorageName attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Events")]
    public void EventsShouldHaveEventStorageNameAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type eventType in EventTypes)
        {
            if (eventType.GetCustomAttribute<EventStorageNameAttribute>() is null)
            {
                violations.Add(eventType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all events must have [EventStorageName] attribute. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All events should have GenerateSerializer attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Events")]
    public void EventsShouldHaveGenerateSerializerAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type eventType in EventTypes)
        {
            if (!HasAttribute(eventType, "GenerateSerializerAttribute"))
            {
                violations.Add(eventType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all events must have [GenerateSerializer] attribute. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All events should have Alias attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Events")]
    public void EventsShouldHaveAliasAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type eventType in EventTypes)
        {
            if (eventType.GetCustomAttribute<AliasAttribute>() is null)
            {
                violations.Add(eventType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all events must have [Alias] attribute for stable serialization. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All aggregates should have BrookName attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Aggregates")]
    public void AggregatesShouldHaveBrookNameAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type aggregateType in AggregateTypes)
        {
            if (aggregateType.GetCustomAttribute<BrookNameAttribute>() is null)
            {
                violations.Add(aggregateType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all aggregates must have [BrookName] attribute. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All aggregates should have SnapshotStorageName attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Aggregates")]
    public void AggregatesShouldHaveSnapshotStorageNameAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type aggregateType in AggregateTypes)
        {
            if (aggregateType.GetCustomAttribute<SnapshotStorageNameAttribute>() is null)
            {
                violations.Add(aggregateType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all aggregates must have [SnapshotStorageName] attribute. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All aggregates should have GenerateSerializer attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Aggregates")]
    public void AggregatesShouldHaveGenerateSerializerAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type aggregateType in AggregateTypes)
        {
            if (!HasAttribute(aggregateType, "GenerateSerializerAttribute"))
            {
                violations.Add(aggregateType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all aggregates must have [GenerateSerializer] attribute. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All aggregates should have Alias attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Aggregates")]
    public void AggregatesShouldHaveAliasAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type aggregateType in AggregateTypes)
        {
            if (aggregateType.GetCustomAttribute<AliasAttribute>() is null)
            {
                violations.Add(aggregateType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all aggregates must have [Alias] attribute for stable serialization. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All projections should have BrookName attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Projections")]
    public void ProjectionsShouldHaveBrookNameAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type projectionType in ProjectionTypes)
        {
            if (projectionType.GetCustomAttribute<BrookNameAttribute>() is null)
            {
                violations.Add(projectionType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all projections must have [BrookName] attribute. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All projections should have SnapshotStorageName attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Projections")]
    public void ProjectionsShouldHaveSnapshotStorageNameAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type projectionType in ProjectionTypes)
        {
            if (projectionType.GetCustomAttribute<SnapshotStorageNameAttribute>() is null)
            {
                violations.Add(projectionType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all projections must have [SnapshotStorageName] attribute. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All projections should have GenerateSerializer attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Projections")]
    public void ProjectionsShouldHaveGenerateSerializerAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type projectionType in ProjectionTypes)
        {
            if (!HasAttribute(projectionType, "GenerateSerializerAttribute"))
            {
                violations.Add(projectionType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all projections must have [GenerateSerializer] attribute. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All projections should have Alias attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Projections")]
    public void ProjectionsShouldHaveAliasAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type projectionType in ProjectionTypes)
        {
            if (projectionType.GetCustomAttribute<AliasAttribute>() is null)
            {
                violations.Add(projectionType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all projections must have [Alias] attribute for stable serialization. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All commands should have GenerateSerializer attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Commands")]
    public void CommandsShouldHaveGenerateSerializerAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type commandType in CommandTypes)
        {
            if (!HasAttribute(commandType, "GenerateSerializerAttribute"))
            {
                violations.Add(commandType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all commands must have [GenerateSerializer] attribute. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     All commands should have Alias attribute.
    /// </summary>
    [Fact]
    [AllureFeature("Commands")]
    public void CommandsShouldHaveAliasAttribute()
    {
        // Arrange
        List<Type> violations = [];

        // Act
        foreach (Type commandType in CommandTypes)
        {
            if (commandType.GetCustomAttribute<AliasAttribute>() is null)
            {
                violations.Add(commandType);
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because all commands must have [Alias] attribute for stable serialization. " +
            "Missing on: {0}",
            string.Join(", ", violations.Select(t => t.Name)));
    }

    /// <summary>
    ///     Event Alias values should follow naming convention.
    /// </summary>
    [Fact]
    [AllureFeature("Events")]
    public void EventAliasValuesShouldFollowNamingConvention()
    {
        // Arrange
        List<string> violations = [];

        // Act
        foreach (Type eventType in EventTypes)
        {
            AliasAttribute? alias = eventType.GetCustomAttribute<AliasAttribute>();

            // Allow either fully qualified or shorter form as long as type name is included
            if (alias is not null && !alias.Alias.Contains(eventType.Name, StringComparison.Ordinal))
            {
                violations.Add($"{eventType.Name}: Alias '{alias.Alias}' does not contain type name");
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "because event Alias values should contain the type name. Violations: {0}",
            string.Join("; ", violations));
    }

    /// <summary>
    ///     Checks if a type has an attribute by name (handles source-generated attributes).
    /// </summary>
    private static bool HasAttribute(Type type, string attributeName) =>
        type.GetCustomAttributes()
            .Any(a => a.GetType().Name == attributeName ||
                      a.GetType().Name == attributeName.Replace("Attribute", string.Empty, StringComparison.Ordinal));
}
