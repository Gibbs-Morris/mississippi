using System;

using Orleans;


namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Generic fixture used to validate generic alias handling.
/// </summary>
/// <typeparam name="T">The generic type argument.</typeparam>
[Alias("Wrong.GenericAliasFixture")]
internal static class GenericAliasFixture<T>
{
    /// <summary>
    ///     Gets the captured generic argument type so the fixture uses <typeparamref name="T" />.
    /// </summary>
    internal static Type GenericArgumentType { get; } = typeof(T);
}