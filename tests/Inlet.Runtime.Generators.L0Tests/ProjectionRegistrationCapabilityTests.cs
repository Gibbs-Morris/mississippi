using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.DomainModeling.Gateway;
using Mississippi.DomainModeling.Runtime;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Gateway.Generators;
using Mississippi.Inlet.Generators.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime;


namespace Mississippi.Inlet.Runtime.Generators.L0Tests;

/// <summary>
///     Compiles and executes independently selected projection capabilities.
/// </summary>
public sealed class ProjectionRegistrationCapabilityTests
{
    private static CSharpCompilation CreateCompilation(
        string source
    )
    {
        string[] platformPaths = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ??
                                  throw new InvalidOperationException("Platform references are unavailable.")).Split(
            Path.PathSeparator);
        Assembly[] contractAssemblies =
        [
            typeof(GenerateProjectionRegistrationAttribute).Assembly,
            typeof(ProjectionPathAttribute).Assembly,
            typeof(BrookNameAttribute).Assembly,
            typeof(EventReducerBase<,>).Assembly,
            typeof(UxProjectionRegistrations).Assembly,
            typeof(SnapshotRegistrations).Assembly,
            typeof(UxProjectionControllerBase<,>).Assembly,
            typeof(IMapper<,>).Assembly,
            typeof(IServiceCollection).Assembly,
        ];
        MetadataReference[] references = contractAssemblies.Select(assembly => assembly.Location)
            .Concat(platformPaths)
            .DistinctBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
        return CSharpCompilation.Create(
            "TestApp.Silo",
            [
                CSharpSyntaxTree.ParseText(
                    source,
                    path: "ProjectionContract.cs",
                    cancellationToken: TestContext.Current.CancellationToken),
            ],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
    }

    private static string CreateSource(
        bool runtime,
        bool http
    )
    {
        string runtimeMarker = runtime ? "[GenerateProjectionRegistration]" : string.Empty;
        string httpMarker = http ? "[GenerateProjectionEndpoints]" : string.Empty;
        string path = http ? "[ProjectionPath(\"account-balance\")]" : string.Empty;
        return $$"""
                 using Mississippi.Brooks.Abstractions.Attributes;
                 using Mississippi.Inlet.Abstractions;
                 using Mississippi.Inlet.Generators.Abstractions;
                 using Mississippi.Tributary.Abstractions;

                 namespace TestApp.Domain.Projections.AccountBalance
                 {
                     {{runtimeMarker}}
                     {{httpMarker}}
                     {{path}}
                     [BrookName("TEST", "PROJECTION", "ACCOUNT")]
                     [SnapshotStorageName("TEST", "PROJECTION", "ACCOUNTBALANCE", 1)]
                     public sealed record AccountBalanceProjection
                     {
                         public int Balance { get; init; }
                     }

                     public sealed record AccountCredited(int Amount);
                 }

                 namespace TestApp.Domain.Projections.AccountBalance.Reducers
                 {
                     public sealed class AccountBalanceReducer :
                         EventReducerBase<AccountCredited, AccountBalanceProjection>
                     {
                         protected override AccountBalanceProjection ReduceCore(
                             AccountBalanceProjection state,
                             AccountCredited eventData
                         ) => state with { Balance = state.Balance + eventData.Amount };
                     }
                 }
                 """;
    }

    private static (Compilation Output, GeneratorDriverRunResult Result) Generate(
        bool runtime,
        bool http,
        string? source = null
    )
    {
        CSharpCompilation input = CreateCompilation(source ?? CreateSource(runtime, http));
        Assert.Empty(
            input.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ProjectionSiloRegistrationGenerator().AsSourceGenerator(),
            new DomainSiloRegistrationGenerator().AsSourceGenerator(),
            new ProjectionEndpointsGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(
            input,
            out Compilation output,
            out ImmutableArray<Diagnostic> diagnostics,
            TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(
            output.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        GeneratorDriverRunResult result = driver.GetRunResult();
        Assert.All(result.Results, generator => Assert.Null(generator.Exception));
        return (output, result);
    }

    private static MethodInfo GetDomainRegistration(
        Assembly assembly
    )
    {
        Type registrations = assembly.GetTypes().Single(type => type.Name == "DomainSiloRegistrations");
        MethodInfo? method = registrations.GetMethod("AddTestAppDomainSilo", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        return method;
    }

    private static Assembly LoadFixture(
        Compilation output,
        ProjectionFixtureLoadContext context
    )
    {
        using MemoryStream image = new();
        EmitResult emitted = output.Emit(image, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        image.Position = 0;
        return context.LoadFromStream(image);
    }

    /// <summary>
    ///     Consumer extensions cannot alter the generated registration guard.
    /// </summary>
    /// <param name="serviceCollectionOverload">Whether the competing extension targets the service collection.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConsumerExtensionsCannotChangeRegistrationGuard(
        bool serviceCollectionOverload
    )
    {
        string genericParameter = serviceCollectionOverload ? string.Empty : "<T>";
        string elementType = serviceCollectionOverload
            ? "global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor"
            : "T";
        string receiverType = serviceCollectionOverload
            ? "global::Microsoft.Extensions.DependencyInjection.IServiceCollection"
            : "global::System.Collections.Generic.IEnumerable<T>";
        string source = CreateSource(true, false) +
                        $$"""

                          namespace TestApp.Domain.Projections.AccountBalance.Reducers
                          {
                              public static class ConsumerExtensions
                              {
                                  public static bool Any{{genericParameter}}(
                                      this {{receiverType}} source,
                                      global::System.Func<{{elementType}}, bool> predicate
                                  ) => false;
                              }
                          }
                          """;
        (Compilation output, GeneratorDriverRunResult _) = Generate(true, false, source);
        ProjectionFixtureLoadContext context = new();
        try
        {
            Assembly assembly = LoadFixture(output, context);
            ServiceCollection services = new();
            MethodInfo registration = GetDomainRegistration(assembly);
            Assert.Same(services, registration.Invoke(null, [services]));
            Assert.Same(services, registration.Invoke(null, [services]));
            Type state = assembly.GetType("TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection", true)!;
            Type reducerContract = typeof(IEventReducer<>).MakeGenericType(state);
            Assert.Single(services, descriptor => descriptor.ServiceType == reducerContract);
        }
        finally
        {
            context.Unload();
        }
    }

    /// <summary>
    ///     Consumer types retain their identity when their name matches the private guard.
    /// </summary>
    /// <param name="consumerTypeName">The projection, event, or reducer name to replace.</param>
    [Theory]
    [InlineData("AccountBalanceProjection")]
    [InlineData("AccountCredited")]
    [InlineData("AccountBalanceReducer")]
    public void ConsumerTypesCanShareTheRegistrationMarkerName(
        string consumerTypeName
    ) =>
        _ = Generate(
            true,
            false,
            CreateSource(true, false).Replace(consumerTypeName, "RegistrationMarker", StringComparison.Ordinal));

    /// <summary>
    ///     Direct and composite helper calls represent the same projection inclusion.
    /// </summary>
    [Fact]
    public void DirectAndCompositeRegistrationRemainIdempotent()
    {
        (Compilation output, GeneratorDriverRunResult _) = Generate(true, true);
        ProjectionFixtureLoadContext context = new();
        try
        {
            Assembly assembly = LoadFixture(output, context);
            ServiceCollection services = new();
            MethodInfo domain = GetDomainRegistration(assembly);
            Assert.Same(services, domain.Invoke(null, [services]));
            Type registrations =
                assembly.GetTypes().Single(type => type.Name == "AccountBalanceProjectionRegistrations");
            MethodInfo? projection = registrations.GetMethod("AddAccountBalanceProjection");
            Assert.NotNull(projection);
            Assert.Same(services, projection.Invoke(null, [services]));
            Assert.Same(services, domain.Invoke(null, [services]));
            Type state = assembly.GetType("TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection", true)!;
            Type reducerContract = typeof(IEventReducer<>).MakeGenericType(state);
            Assert.Single(services, descriptor => descriptor.ServiceType == reducerContract);
        }
        finally
        {
            context.Unload();
        }
    }

    /// <summary>
    ///     Neither capability infers the other, and every selected output compiles.
    /// </summary>
    /// <param name="runtime">Whether runtime registration is selected.</param>
    /// <param name="http">Whether HTTP generation is selected.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SelectedCapabilitiesCompileIndependently(
        bool runtime,
        bool http
    )
    {
        (Compilation output, GeneratorDriverRunResult result) = Generate(runtime, http);
        Assert.Equal(runtime ? 1 : 0, result.Results[0].GeneratedSources.Length);
        Assert.Equal(runtime ? 1 : 0, result.Results[1].GeneratedSources.Length);
        Assert.Equal(http ? 4 : 0, result.Results[2].GeneratedSources.Length);
        ProjectionFixtureLoadContext context = new();
        try
        {
            Assembly assembly = LoadFixture(output, context);
            Type state = assembly.GetType("TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection", true)!;
            Assert.Equal("TEST.PROJECTION.ACCOUNT", state.GetCustomAttribute<BrookNameAttribute>()!.BrookName);
            Assert.Equal(
                "TEST.PROJECTION.ACCOUNTBALANCE.V1",
                state.GetCustomAttribute<SnapshotStorageNameAttribute>()!.StorageName);
            Assert.Equal(http, state.GetCustomAttribute<ProjectionPathAttribute>() is not null);
            if (!runtime)
            {
                Assert.DoesNotContain(
                    assembly.GetTypes(),
                    type => type.Name == "AccountBalanceProjectionRegistrations");
                return;
            }

            ServiceCollection services = new();
            Assert.Same(services, GetDomainRegistration(assembly).Invoke(null, [services]));
            Type converter = typeof(ISnapshotStateConverter<>).MakeGenericType(state);
            Assert.Single(services, descriptor => descriptor.ServiceType == converter);
            Type credited = assembly.GetType("TestApp.Domain.Projections.AccountBalance.AccountCredited", true)!;
            Type reducerContract = typeof(IEventReducer<,>).MakeGenericType(credited, state);
            using ServiceProvider provider = services.BuildServiceProvider();
            object reducer = provider.GetRequiredService(reducerContract);
            object initial = Activator.CreateInstance(state)!;
            object eventData = Activator.CreateInstance(credited, 7)!;
            object? reduced = reducer.GetType().GetMethod("Reduce")!.Invoke(reducer, [initial, eventData]);
            Assert.Equal(7, state.GetProperty("Balance")!.GetValue(reduced));
        }
        finally
        {
            context.Unload();
        }
    }

    /// <summary>
    ///     The real declaration rejects unsupported targets, multiplicity, and settings.
    /// </summary>
    /// <param name="declaration">The unsupported attribute declaration.</param>
    /// <param name="diagnosticText">The identifying diagnostic text.</param>
    [Theory]
    [InlineData(
        "[GenerateProjectionRegistration] public struct InvalidProjection {}",
        "GenerateProjectionRegistration")]
    [InlineData(
        "[GenerateProjectionRegistration, GenerateProjectionRegistration] public class InvalidProjection {}",
        "Duplicate")]
    [InlineData(
        "[GenerateProjectionRegistration(1)] public class InvalidProjection {}",
        "GenerateProjectionRegistration")]
    [InlineData("[GenerateProjectionRegistration(Unknown = true)] public class InvalidProjection {}", "Unknown")]
    public void UnsupportedDeclarationsFailClearly(
        string declaration,
        string diagnosticText
    )
    {
        CSharpCompilation input = CreateCompilation(
            "using Mississippi.Inlet.Generators.Abstractions;" + Environment.NewLine + declaration);
        Diagnostic[] errors = input.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.Contains(
            errors,
            diagnostic => diagnostic.GetMessage(CultureInfo.InvariantCulture)
                .Contains(diagnosticText, StringComparison.Ordinal));
    }
}