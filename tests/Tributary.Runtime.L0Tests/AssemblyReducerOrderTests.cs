using System;
using System.Reflection;
using System.Reflection.Emit;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime;


namespace MississippiTests.Tributary.Runtime.L0Tests;

/// <summary>
///     Verifies reducer priority fingerprints across defining assemblies.
/// </summary>
public sealed class AssemblyReducerOrderTests
{
    private static IEventReducer<string> CreateReducer<TEvent>(
        string assemblyName,
        Version assemblyVersion,
        string result
    )
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new(assemblyName)
            {
                Version = assemblyVersion,
            },
            AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder module = assembly.DefineDynamicModule(assemblyName);
        Type baseType = typeof(EventReducerBase<TEvent, string>);
        TypeBuilder reducerType = module.DefineType(
            "SnapshotCompatibility.CompetingReducer",
            TypeAttributes.Public | TypeAttributes.Sealed,
            baseType);
        reducerType.DefineDefaultConstructor(MethodAttributes.Public);
        MethodBuilder reduceCore = reducerType.DefineMethod(
            "ReduceCore",
            MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            typeof(string),
            [typeof(string), typeof(TEvent)]);
        ILGenerator instructions = reduceCore.GetILGenerator();
        instructions.Emit(OpCodes.Ldstr, result);
        instructions.Emit(OpCodes.Ret);
        reducerType.DefineMethodOverride(
            reduceCore,
            baseType.GetMethod("ReduceCore", BindingFlags.Instance | BindingFlags.NonPublic)!);
        return (IEventReducer<string>)Activator.CreateInstance(reducerType.CreateType())!;
    }

    /// <summary>
    ///     Verifies that competing same-named types from distinct assemblies have priority-sensitive identities.
    /// </summary>
    [Fact]
    public void CompetingSameNamedTypesFromDifferentAssembliesShouldChangeHashAndSnapshotKey()
    {
        IEventReducer<string> first = CreateReducer<int>("SnapshotCompatibility.First", new(1, 0, 0, 0), "first");
        IEventReducer<string> second = CreateReducer<int>("SnapshotCompatibility.Second", new(1, 0, 0, 0), "second");
        Assert.Equal(first.GetType().FullName, second.GetType().FullName);
        Assert.NotEqual(first.GetType(), second.GetType());
        Assert.NotEqual(first.GetType().Assembly.GetName().Name, second.GetType().Assembly.GetName().Name);
        RootReducer<string> original = new([first, second]);
        RootReducer<string> reordered = new([second, first]);
        Assert.Equal("first", original.Reduce(string.Empty, 5));
        Assert.Equal("second", reordered.Reduce(string.Empty, 5));
        Assert.NotEqual(original.GetReducerHash(), reordered.GetReducerHash());
        SnapshotKey originalKey = new(
            new("TEST.ASSEMBLY.ORDER", "TEST.ASSEMBLY.STATE.V1", "entity-1", original.GetReducerHash()),
            0);
        SnapshotKey reorderedKey = new(
            new("TEST.ASSEMBLY.ORDER", "TEST.ASSEMBLY.STATE.V1", "entity-1", reordered.GetReducerHash()),
            0);
        Assert.NotEqual(originalKey, reorderedKey);
    }

    /// <summary>
    ///     Verifies that independent routes retain the historical full-name fingerprint across assemblies.
    /// </summary>
    [Fact]
    public void IndependentSameNamedTypesShouldKeepHistoricalHashAndReplay()
    {
        const string expectedHash = "7BAF9874205792C40DD1E9617F45ED850E6FDE3F5A8A0D87C22C91A142BBDE4F";
        IEventReducer<string> number = CreateReducer<int>("SnapshotCompatibility.Number", new(1, 0, 0, 0), "number");
        IEventReducer<string> text = CreateReducer<string>("SnapshotCompatibility.Text", new(1, 0, 0, 0), "text");
        RootReducer<string> original = new([number, text]);
        RootReducer<string> reordered = new([text, number]);
        Assert.Equal(expectedHash, original.GetReducerHash());
        Assert.Equal(expectedHash, reordered.GetReducerHash());
        Assert.Equal("number", original.Reduce(string.Empty, 5));
        Assert.Equal("number", reordered.Reduce(string.Empty, 5));
        Assert.Equal("text", original.Reduce(string.Empty, "event"));
        Assert.Equal("text", reordered.Reduce(string.Empty, "event"));
    }

    /// <summary>
    ///     Verifies that version-only assembly changes preserve equivalent competing reducer identities.
    /// </summary>
    [Fact]
    public void VersionOnlyAssemblyChangesShouldKeepHashAndReplay()
    {
        RootReducer<string> original = new(
        [
            CreateReducer<int>("SnapshotCompatibility.First", new(1, 0, 0, 0), "first"),
            CreateReducer<int>("SnapshotCompatibility.Second", new(1, 0, 0, 0), "second"),
        ]);
        RootReducer<string> equivalent = new(
        [
            CreateReducer<int>("SnapshotCompatibility.First", new(2, 0, 0, 0), "first"),
            CreateReducer<int>("SnapshotCompatibility.Second", new(2, 0, 0, 0), "second"),
        ]);
        Assert.Equal(original.GetReducerHash(), equivalent.GetReducerHash());
        Assert.Equal("first", original.Reduce(string.Empty, 5));
        Assert.Equal("first", equivalent.Reduce(string.Empty, 5));
    }
}