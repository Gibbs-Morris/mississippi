using System;


namespace Mississippi.Inlet.Generators.Abstractions;

/// <summary>
///     Marks a projection for saga status reducer generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateSagaStatusReducersAttribute : Attribute
{
}