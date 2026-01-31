using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;


namespace Mississippi.EventSourcing.Testing.Effects;

/// <summary>
///     Extension methods for effect testing assertions.
/// </summary>
public static class EffectTestExtensions
{
    // ShouldHaveDispatched overloads grouped together

    /// <summary>
    ///     Asserts that a command of the specified type was dispatched.
    /// </summary>
    /// <typeparam name="TCommand">The expected command type.</typeparam>
    /// <param name="commands">The list of dispatched commands.</param>
    /// <returns>The matching command for further assertions.</returns>
    public static TCommand ShouldHaveDispatched<TCommand>(
        this IReadOnlyList<(Type AggregateType, string EntityId, object Command)> commands
    )
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(commands);
        commands.Any(c => c.Command is TCommand)
            .Should()
            .BeTrue($"because a {typeof(TCommand).Name} command should have been dispatched");
        (Type AggregateType, string EntityId, object Command) match = commands.First(c => c.Command is TCommand);
        return (TCommand)match.Command;
    }

    /// <summary>
    ///     Asserts that a command of the specified type was dispatched from an <see cref="EffectTestResult" />.
    /// </summary>
    /// <typeparam name="TCommand">The expected command type.</typeparam>
    /// <param name="result">The effect test result.</param>
    /// <returns>The matching command for further assertions.</returns>
    public static TCommand ShouldHaveDispatched<TCommand>(
        this EffectTestResult result
    )
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.DispatchedCommands.ShouldHaveDispatched<TCommand>();
    }

    // ShouldHaveDispatchedTo overloads grouped together

    /// <summary>
    ///     Asserts that a command was dispatched to the specified aggregate type.
    /// </summary>
    /// <typeparam name="TAggregate">The expected target aggregate type.</typeparam>
    /// <param name="commands">The list of dispatched commands.</param>
    /// <param name="entityId">Optional expected entity ID.</param>
    /// <returns>The matching command record for further assertions.</returns>
    public static (Type AggregateType, string EntityId, object Command) ShouldHaveDispatchedTo<TAggregate>(
        this IReadOnlyList<(Type AggregateType, string EntityId, object Command)> commands,
        string? entityId = null
    )
        where TAggregate : class
    {
        ArgumentNullException.ThrowIfNull(commands);
        (Type AggregateType, string EntityId, object Command)? match = commands.FirstOrDefault(c =>
            (c.AggregateType == typeof(TAggregate)) && ((entityId == null) || (c.EntityId == entityId)));
        match.Should().NotBeNull($"because a command should have been dispatched to {typeof(TAggregate).Name}");
        return match!.Value;
    }

    /// <summary>
    ///     Asserts that a command was dispatched to the specified aggregate type from an <see cref="EffectTestResult" />.
    /// </summary>
    /// <typeparam name="TAggregate">The expected target aggregate type.</typeparam>
    /// <param name="result">The effect test result.</param>
    /// <param name="entityId">Optional expected entity ID.</param>
    /// <returns>The matching command record for further assertions.</returns>
    public static (Type AggregateType, string EntityId, object Command) ShouldHaveDispatchedTo<TAggregate>(
        this EffectTestResult result,
        string? entityId = null
    )
        where TAggregate : class
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.DispatchedCommands.ShouldHaveDispatchedTo<TAggregate>(entityId);
    }

    // ShouldHaveNoDispatches overloads grouped together

    /// <summary>
    ///     Asserts that no commands were dispatched.
    /// </summary>
    /// <param name="commands">The list of dispatched commands.</param>
    public static void ShouldHaveNoDispatches(
        this IReadOnlyList<(Type AggregateType, string EntityId, object Command)> commands
    )
    {
        ArgumentNullException.ThrowIfNull(commands);
        commands.Should().BeEmpty("because no commands should have been dispatched");
    }

    /// <summary>
    ///     Asserts that no commands were dispatched from an <see cref="EffectTestResult" />.
    /// </summary>
    /// <param name="result">The effect test result.</param>
    /// <returns>The result for fluent chaining.</returns>
    public static EffectTestResult ShouldHaveNoDispatches(
        this EffectTestResult result
    )
    {
        ArgumentNullException.ThrowIfNull(result);
        result.DispatchedCommands.ShouldHaveNoDispatches();
        return result;
    }
}