using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using Orleans;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Architecture;

/// <summary>
///     Creates isolated in-memory type identities for alias validation contracts.
/// </summary>
internal static class AliasFixtureFactory
{
    /// <summary>
    ///     Creates a type with the requested identity and type-level attributes.
    /// </summary>
    /// <param name="typeFullName">The CLR identity to classify.</param>
    /// <param name="isInterface">Whether the fixture is an interface.</param>
    /// <param name="isGenerated">Whether the compiler-generated attribute is present.</param>
    /// <param name="alias">The alias to attach, or a deliberately different legacy identity.</param>
    /// <returns>The isolated fixture type.</returns>
    internal static Type CreateType(
        string typeFullName,
        bool isInterface = false,
        bool isGenerated = false,
        string? alias = null
    )
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new("AliasValidationFixtures"),
            AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder module = assembly.DefineDynamicModule("Fixtures");
        TypeAttributes attributes = isInterface
            ? TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract
            : TypeAttributes.Public | TypeAttributes.Sealed;
        TypeBuilder builder = module.DefineType(typeFullName, attributes);
        builder.SetCustomAttribute(
            new(typeof(AliasAttribute).GetConstructor([typeof(string)])!, [alias ?? $"Legacy.{typeFullName}"]));
        if (isGenerated)
        {
            builder.SetCustomAttribute(new(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes)!, []));
        }

        return builder.CreateType();
    }
}