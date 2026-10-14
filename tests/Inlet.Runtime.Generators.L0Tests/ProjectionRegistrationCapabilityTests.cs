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
using Mississippi.DomainModeling.Abstractions;
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

    private static string CreateSagaSource(
        bool authoredReducer
    )
    {
        const string sagaProperties = """
                                      public int Balance { get; init; }
                                      public global::Mississippi.DomainModeling.Abstractions.SagaPhase Phase { get; init; }
                                      public int LastCompletedStepIndex { get; init; } = -1;
                                      public string? ErrorCode { get; init; }
                                      public string? ErrorMessage { get; init; }
                                      public global::System.DateTimeOffset? StartedAt { get; init; }
                                      public global::System.DateTimeOffset? CompletedAt { get; init; }
                                      """;
        string source = CreateSource(true, false)
            .Replace(
                "[GenerateProjectionRegistration]",
                "[GenerateProjectionRegistration, GenerateSagaStatusReducers]",
                StringComparison.Ordinal)
            .Replace("public int Balance { get; init; }", sagaProperties, StringComparison.Ordinal);
        if (!authoredReducer)
        {
            source = source[..source.IndexOf(
                "namespace TestApp.Domain.Projections.AccountBalance.Reducers",
                StringComparison.Ordinal)];
        }

        return source
            .Replace(
                "EventReducerBase<AccountCredited, AccountBalanceProjection>",
                "EventReducerBase<global::Mississippi.DomainModeling.Abstractions.SagaStartedEvent, AccountBalanceProjection>",
                StringComparison.Ordinal)
            .Replace(
                "AccountCredited eventData",
                "global::Mississippi.DomainModeling.Abstractions.SagaStartedEvent eventData",
                StringComparison.Ordinal)
            .Replace("state.Balance + eventData.Amount", "42", StringComparison.Ordinal);
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
        string? source = null,
        bool includeSaga = false,
        CSharpCompilation? input = null
    )
    {
        input ??= CreateCompilation(source ?? CreateSource(runtime, http));
        Assert.Empty(
            input.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ProjectionSiloRegistrationGenerator().AsSourceGenerator(),
            new DomainSiloRegistrationGenerator().AsSourceGenerator(),
            new ProjectionEndpointsGenerator().AsSourceGenerator());
        if (includeSaga)
        {
            driver = driver.AddGenerators([new SagaStatusReducersGenerator().AsSourceGenerator()]);
        }

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

    private static object ReduceSagaEvent(
        ServiceProvider provider,
        Type stateType,
        object state,
        object eventData
    )
    {
        Type contract = typeof(IEventReducer<,>).MakeGenericType(eventData.GetType(), stateType);
        object reducer = provider.GetRequiredService(contract);
        return reducer.GetType().GetMethod("Reduce")!.Invoke(reducer, [state, eventData])!;
    }

    private static void VerifySagaRegistrations(
        Compilation output,
        bool authoredReducer
    )
    {
        ProjectionFixtureLoadContext context = new();
        try
        {
            Assembly assembly = LoadFixture(output, context);
            Type stateType = assembly.GetType(
                "TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection",
                true)!;
            ServiceCollection services = new();
            MethodInfo domain = GetDomainRegistration(assembly);
            MethodInfo projection = assembly.GetTypes()
                .Single(type => type.Name == "AccountBalanceProjectionRegistrations")
                .GetMethod("AddAccountBalanceProjection")!;
            Assert.Same(services, projection.Invoke(null, [services]));
            Assert.Same(services, domain.Invoke(null, [services]));
            Assert.Same(services, projection.Invoke(null, [services]));
            Type reducerContract = typeof(IEventReducer<>).MakeGenericType(stateType);
            Assert.Equal(authoredReducer ? 8 : 7, services.Count(item => item.ServiceType == reducerContract));
            Assert.Single(
                services,
                item => item.ServiceType == typeof(ISnapshotStateConverter<>).MakeGenericType(stateType));
            using ServiceProvider provider = services.BuildServiceProvider();
            object state = Activator.CreateInstance(stateType)!;
            DateTimeOffset timestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            state = ReduceSagaEvent(
                provider,
                stateType,
                state,
                new SagaStartedEvent
                {
                    SagaId = Guid.Empty,
                    StepHash = "steps",
                    StartedAt = timestamp,
                });
            Assert.Equal(SagaPhase.Running, stateType.GetProperty("Phase")!.GetValue(state));
            Assert.Equal(timestamp, stateType.GetProperty("StartedAt")!.GetValue(state));
            Assert.Equal(-1, stateType.GetProperty("LastCompletedStepIndex")!.GetValue(state));
            Assert.Null(stateType.GetProperty("ErrorCode")!.GetValue(state));
            Assert.Null(stateType.GetProperty("ErrorMessage")!.GetValue(state));
            Assert.Null(stateType.GetProperty("CompletedAt")!.GetValue(state));
            state = ReduceSagaEvent(
                provider,
                stateType,
                state,
                new SagaStepCompleted
                {
                    StepIndex = 2,
                    StepName = "credit",
                    CompletedAt = timestamp,
                });
            Assert.Equal(2, stateType.GetProperty("LastCompletedStepIndex")!.GetValue(state));
            state = ReduceSagaEvent(
                provider,
                stateType,
                state,
                new SagaStepFailed
                {
                    StepIndex = 3,
                    StepName = "debit",
                    ErrorCode = "step",
                    ErrorMessage = "step failed",
                });
            Assert.Equal("step", stateType.GetProperty("ErrorCode")!.GetValue(state));
            Assert.Equal("step failed", stateType.GetProperty("ErrorMessage")!.GetValue(state));
            state = ReduceSagaEvent(
                provider,
                stateType,
                state,
                new SagaFailed
                {
                    ErrorCode = "saga",
                    ErrorMessage = "saga failed",
                    FailedAt = timestamp,
                });
            Assert.Equal(SagaPhase.Failed, stateType.GetProperty("Phase")!.GetValue(state));
            Assert.Equal("saga", stateType.GetProperty("ErrorCode")!.GetValue(state));
            Assert.Equal("saga failed", stateType.GetProperty("ErrorMessage")!.GetValue(state));
            Assert.Equal(timestamp, stateType.GetProperty("CompletedAt")!.GetValue(state));
            state = ReduceSagaEvent(
                provider,
                stateType,
                state,
                new SagaCompleted
                {
                    CompletedAt = timestamp,
                });
            Assert.Equal(SagaPhase.Completed, stateType.GetProperty("Phase")!.GetValue(state));
            state = ReduceSagaEvent(
                provider,
                stateType,
                state,
                new SagaCompensating
                {
                    FromStepIndex = 2,
                });
            Assert.Equal(SagaPhase.Compensating, stateType.GetProperty("Phase")!.GetValue(state));
            state = ReduceSagaEvent(
                provider,
                stateType,
                state,
                new SagaCompensated
                {
                    CompletedAt = timestamp,
                });
            Assert.Equal(SagaPhase.Compensated, stateType.GetProperty("Phase")!.GetValue(state));
            Assert.Equal(timestamp, stateType.GetProperty("CompletedAt")!.GetValue(state));
            if (authoredReducer)
            {
                Type rootContract = typeof(IRootReducer<>).MakeGenericType(stateType);
                object root = provider.GetRequiredService(rootContract);
                object? prioritized = root.GetType().GetMethod("Reduce")!.Invoke(
                    root,
                    [
                        state, new SagaStartedEvent
                        {
                            SagaId = Guid.Empty,
                            StepHash = "steps",
                            StartedAt = timestamp,
                        },
                    ]);
                Assert.Equal(42, stateType.GetProperty("Balance")!.GetValue(prioritized));
                Assert.Equal(SagaPhase.Compensated, stateType.GetProperty("Phase")!.GetValue(prioritized));
            }
        }
        finally
        {
            context.Unload();
        }
    }

    /// <summary>
    ///     Consumer extensions cannot capture generated framework registrations.
    /// </summary>
    /// <param name="methodName">The competing framework method.</param>
    /// <param name="typeParameters">The method's generic parameters.</param>
    [Theory]
    [InlineData("AddReducer", "<TEvent, TProjection, TReducer>")]
    [InlineData("AddSnapshotStateConverter", "<TSnapshot>")]
    [InlineData("AddUxProjections", "")]
    public void ConsumerExtensionsCannotCaptureFrameworkRegistrations(
        string methodName,
        string typeParameters
    )
    {
        string source = CreateSource(true, false) +
                        $$"""
                          namespace TestApp.Domain.Projections.AccountBalance.Reducers
                          {
                              public static class ConsumerExtensions
                              {
                                  public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection {{methodName}}{{typeParameters}}(
                                      this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services
                                  ) => services;
                              }
                          }
                          """;
        (Compilation output, GeneratorDriverRunResult _) = Generate(true, false, source);
        ProjectionFixtureLoadContext context = new();
        try
        {
            Assembly assembly = LoadFixture(output, context);
            ServiceCollection services = new();
            GetDomainRegistration(assembly).Invoke(null, [services]);
            Type state = assembly.GetType("TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection", true)!;
            Assert.Single(services, item => item.ServiceType == typeof(IEventReducer<>).MakeGenericType(state));
            Assert.Single(
                services,
                item => item.ServiceType == typeof(ISnapshotStateConverter<>).MakeGenericType(state));
            Assert.Single(services, item => item.ServiceType == typeof(IUxProjectionGrainFactory));
        }
        finally
        {
            context.Unload();
        }
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
    ///     Consumer singleton extensions do not capture marker registration.
    /// </summary>
    [Fact]
    public void ConsumerSingletonExtensionsCannotCaptureRegistration()
    {
        string source = CreateSource(true, false) +
                        """

                        namespace TestApp.Domain.Projections.AccountBalance.Reducers
                        {
                            public static class ConsumerExtensions
                            {
                                public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddSingleton<T>(
                                    this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                                    T instance
                                ) where T : class => services;
                            }
                        }
                        """;
        _ = Generate(true, false, source);
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
    ///     Referenced saga reducers are discovered once through their emitted symbols.
    /// </summary>
    /// <param name="authoredReducer">Whether the referenced domain also contains an authored reducer.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReferencedSagaReducersAreNotDuplicated(
        bool authoredReducer
    )
    {
        string source = CreateSagaSource(authoredReducer);
        const string friendAssembly =
            "[assembly: global::System.Runtime.CompilerServices.InternalsVisibleTo(\"TestApp.Silo\")]";
        source = source.Insert(
            source.IndexOf("namespace TestApp.", StringComparison.Ordinal),
            friendAssembly + Environment.NewLine);
        CSharpCompilation domain = CreateCompilation(source).WithAssemblyName("TestApp.Domain");
        GeneratorDriver domainDriver =
            CSharpGeneratorDriver.Create(new SagaStatusReducersGenerator().AsSourceGenerator());
        domainDriver.RunGeneratorsAndUpdateCompilation(
            domain,
            out Compilation generatedDomain,
            out ImmutableArray<Diagnostic> diagnostics,
            TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error));
        using MemoryStream image = new();
        EmitResult emitted = generatedDomain.Emit(image, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        CSharpCompilation silo = CreateCompilation(string.Empty)
            .AddReferences(MetadataReference.CreateFromImage(image.ToArray()));
        (Compilation output, GeneratorDriverRunResult _) = Generate(true, false, includeSaga: true, input: silo);
        ProjectionFixtureLoadContext context = new();
        try
        {
            image.Position = 0;
            Assembly domainAssembly = context.LoadFromStream(image);
            Assembly siloAssembly = LoadFixture(output, context);
            ServiceCollection services = new();
            GetDomainRegistration(siloAssembly).Invoke(null, [services]);
            GetDomainRegistration(siloAssembly).Invoke(null, [services]);
            Type state = domainAssembly.GetType(
                "TestApp.Domain.Projections.AccountBalance.AccountBalanceProjection",
                true)!;
            Assert.Equal(
                authoredReducer ? 8 : 7,
                services.Count(item => item.ServiceType == typeof(IEventReducer<>).MakeGenericType(state)));
            Assert.Single(
                services,
                item => item.ServiceType == typeof(ISnapshotStateConverter<>).MakeGenericType(state));
        }
        finally
        {
            context.Unload();
        }
    }

    /// <summary>
    ///     Saga reducer generation does not infer runtime registration.
    /// </summary>
    [Fact]
    public void SagaReducersDoNotSelectRuntimeRegistration()
    {
        string source = CreateSagaSource(false)
            .Replace(
                "[GenerateProjectionRegistration, GenerateSagaStatusReducers]",
                "[GenerateSagaStatusReducers]",
                StringComparison.Ordinal);
        (Compilation _, GeneratorDriverRunResult result) = Generate(false, false, source, true);
        Assert.Empty(result.Results[0].GeneratedSources);
        Assert.Empty(result.Results[1].GeneratedSources);
        Assert.Empty(result.Results[2].GeneratedSources);
        Assert.Single(result.Results[3].GeneratedSources);
    }

    /// <summary>
    ///     Saga reducer generation and registration work in one generator invocation.
    /// </summary>
    /// <param name="authoredReducer">Whether an authored reducer supplements generated saga reducers.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameCompilationSagaReducersAreRegistered(
        bool authoredReducer
    )
    {
        (Compilation output, GeneratorDriverRunResult result) = Generate(
            true,
            false,
            CreateSagaSource(authoredReducer),
            true);
        Assert.Single(result.Results[0].GeneratedSources);
        Assert.Single(result.Results[1].GeneratedSources);
        Assert.Single(result.Results[3].GeneratedSources);
        VerifySagaRegistrations(output, authoredReducer);
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