using System;
using System.Linq;
using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Orleans.Configuration;
using Orleans.Hosting;


namespace Mississippi.Hosting.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiRuntimeBuilder" /> and
///     <see cref="MississippiRuntimeHostBuilderRegistrations" />.
/// </summary>
public sealed class MississippiRuntimeBuilderTests
{
    private const string SecretValue = "super-secret-key";

    private sealed class TestSiloBuilder : ISiloBuilder
    {
        public IServiceCollection Services { get; } = new ServiceCollection();

        public IConfiguration Configuration { get; } = new ConfigurationBuilder().Build();
    }

    private sealed class TestMarker
    {
        public string Value { get; } = nameof(TestMarker);
    }

    private static MississippiRuntimeBuilderState GetState(
        IServiceCollection services
    )
    {
        ServiceDescriptor descriptor = Assert.Single(
            services,
            serviceDescriptor => serviceDescriptor.ServiceType == typeof(MississippiRuntimeBuilderState));
        MississippiRuntimeBuilderState? state = descriptor.ImplementationInstance as MississippiRuntimeBuilderState;
        Assert.NotNull(state);
        return state;
    }

    private static WebApplicationBuilder CreateBuilder()
        => CreateBuilder(Environments.Development);

    private static WebApplicationBuilder CreateBuilder(
        string environmentName
    ) => WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });

    private static void AssertFrozenOwnershipViolation(
        InvalidOperationException exception,
        string expectedCategory
    )
    {
        Assert.Contains(expectedCategory, exception.Message, StringComparison.Ordinal);
        Assert.Contains("MississippiRuntimeBuilder.Orleans(...)", exception.Message, StringComparison.Ordinal);
        Assert.Contains("additive silo tuning", exception.Message, StringComparison.Ordinal);
    }

    private static void AddCompetingOrleansOwnershipMarker(
        IServiceCollection services
    )
    {
        services.AddSingleton(new MississippiCompetingOrleansOwnershipMarker());
    }

    private static void AddGatewayHostModeMarker(
        IServiceCollection services
    )
    {
        services.AddSingleton(new MississippiGatewayHostModeMarker());
    }

    /// <summary>
    ///     AddMississippiRuntime configure overload should invoke the callback and return the original host builder.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeConfigureOverloadReturnsOriginalBuilder()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiRuntimeBuilder? configuredBuilder = null;
        WebApplicationBuilder result = builder.AddMississippiRuntime(runtime => configuredBuilder = runtime);
        Assert.Same(builder, result);
        Assert.NotNull(configuredBuilder);
        Assert.Same(builder.Services, configuredBuilder.Services);
    }

    /// <summary>
    ///     AddMississippiRuntime should return the runtime builder.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeReturnsRuntimeBuilder()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        Assert.NotNull(runtimeBuilder);
        Assert.Same(builder.Services, runtimeBuilder.Services);
    }

    /// <summary>
    ///     AddMississippiRuntime should reject duplicate runtime attachment on the same host.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeRejectsDuplicateAttachment()
    {
        WebApplicationBuilder builder = CreateBuilder();
        builder.AddMississippiRuntime();
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => builder.AddMississippiRuntime());
        Assert.Contains("AddMississippiRuntime can only be called once per host", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     AddMississippiRuntime should reject unsupported same-host gateway composition.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeRejectsSameHostGatewayComposition()
    {
        WebApplicationBuilder builder = CreateBuilder();
        AddGatewayHostModeMarker(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => builder.AddMississippiRuntime());
        Assert.Contains("runtime and gateway composition cannot share the same host", exception.Message, StringComparison.Ordinal);
        Assert.Contains("runtime-only path", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     AddMississippiRuntime should reject loopback or emulator connection strings outside Development.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeRejectsLoopbackConnectionStringsOutsideDevelopment()
    {
        WebApplicationBuilder builder = CreateBuilder(Environments.Production);
        builder.Configuration["ConnectionStrings:cosmos"] = $"AccountEndpoint=http://127.0.0.1:8081/;AccountKey={SecretValue};";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => builder.AddMississippiRuntime());

        Assert.Contains("[config-trust]", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings:cosmos", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretValue, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     AddMississippiRuntime should allow loopback or emulator connection strings in Development.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeAllowsLoopbackConnectionStringsInDevelopment()
    {
        WebApplicationBuilder builder = CreateBuilder();
        builder.Configuration["ConnectionStrings:cosmos"] = $"AccountEndpoint=http://{IPAddress.Loopback}:8081/;AccountKey={SecretValue};";

        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();

        Assert.NotNull(runtimeBuilder);
    }

    /// <summary>
    ///     AddMississippiRuntime should allow explicitly approved loopback endpoints outside Development.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeAllowsExplicitlyApprovedLoopbackOutsideDevelopment()
    {
        WebApplicationBuilder builder = CreateBuilder(Environments.Production);
        builder.Configuration["Mississippi:Runtime:Trust:AllowLocalEndpointsOutsideDevelopment"] = bool.TrueString;
        builder.Configuration["ConnectionStrings:cosmos"] = $"AccountEndpoint=http://localhost:8081/;AccountKey={SecretValue};";

        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();

        Assert.NotNull(runtimeBuilder);
    }

    /// <summary>
    ///     AddMississippiRuntime should reject insecure external endpoint transport outside Development.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeRejectsInsecureExternalEndpointTransport()
    {
        WebApplicationBuilder builder = CreateBuilder(Environments.Production);
        builder.Configuration["ConnectionStrings:cosmos"] = $"AccountEndpoint=http://prod.example.com/;AccountKey={SecretValue};";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => builder.AddMississippiRuntime());

        Assert.Contains("[config-trust]", exception.Message, StringComparison.Ordinal);
        Assert.Contains("scheme 'http'", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("prod.example.com", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretValue, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     AddMississippiRuntime should reject derived insecure external transport from storage-style connection strings.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeRejectsDerivedInsecureExternalTransport()
    {
        WebApplicationBuilder builder = CreateBuilder(Environments.Production);
        builder.Configuration["ConnectionStrings:storage"] =
            $"DefaultEndpointsProtocol=http;AccountName=productionaccount;AccountKey={SecretValue};EndpointSuffix=core.windows.net";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => builder.AddMississippiRuntime());

        Assert.Contains("[config-trust]", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings:storage", exception.Message, StringComparison.Ordinal);
        Assert.Contains("scheme 'http'", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("productionaccount", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretValue, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     AddMississippiRuntime should reject connection strings that mix local and external endpoint sets.
    /// </summary>
    [Fact]
    public void AddMississippiRuntimeRejectsConflictingEndpointSet()
    {
        WebApplicationBuilder builder = CreateBuilder(Environments.Production);
        builder.Configuration["ConnectionStrings:storage"] =
            $"BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;TableEndpoint=https://storage.example.com/;AccountKey={SecretValue};";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => builder.AddMississippiRuntime());

        Assert.Contains("[config-trust]", exception.Message, StringComparison.Ordinal);
        Assert.Contains("mixes local or emulator hosts with external hosts", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretValue, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Orleans should queue additional silo configuration on the runtime-owned state object.
    /// </summary>
    [Fact]
    public void OrleansQueuesConfigurationOnRuntimeState()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        runtimeBuilder.Orleans(silo => silo.Services.AddSingleton<TestMarker>());
        MississippiRuntimeBuilderState state = GetState(builder.Services);
        Assert.Single(state.QueuedOrleansConfigurations);
    }

    /// <summary>
    ///     Queued Orleans callbacks should replay against the silo builder in registration order.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationReplaysQueuedCallbacksInOrder()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        runtimeBuilder.Orleans(silo => silo.Services.AddSingleton(new TestMarker()));
        runtimeBuilder.Orleans(silo => silo.Services.AddSingleton("queued-second"));

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        TestSiloBuilder siloBuilder = new();

        state.ApplyOrleansConfiguration(siloBuilder);

        object[] implementations = siloBuilder.Services
            .Where(descriptor => descriptor.ImplementationInstance is not null)
            .Select(descriptor => descriptor.ImplementationInstance!)
            .ToArray();

        Assert.Collection(
            implementations,
            implementation => Assert.IsType<TestMarker>(implementation),
            implementation => Assert.Equal("queued-second", implementation));
    }

    /// <summary>
    ///     Runtime-owned Orleans attachment should allow additive silo tuning that does not mutate frozen ownership.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationAllowsAdditiveSiloTuningThatPreservesOwnership()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        runtimeBuilder.Orleans(silo => silo.Configure<SiloOptions>(options => options.SiloName = "allowed-runtime-silo"));

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        TestSiloBuilder siloBuilder = new();

        state.ApplyOrleansConfiguration(siloBuilder);

        Assert.Contains(
            siloBuilder.Services,
            descriptor => descriptor.ServiceType.FullName?.Contains(nameof(SiloOptions), StringComparison.Ordinal) == true);
    }

    /// <summary>
    ///     Runtime-owned Orleans attachment should reject same-host gateway composition added after runtime attachment.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationRejectsSameHostGatewayCompositionAddedAfterRuntimeAttachment()
    {
        WebApplicationBuilder builder = CreateBuilder();
        builder.AddMississippiRuntime();
        AddGatewayHostModeMarker(builder.Services);

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => state.ApplyOrleansConfiguration(new TestSiloBuilder()));

        Assert.Contains("runtime and gateway composition cannot share the same host", exception.Message, StringComparison.Ordinal);
        Assert.Contains("runtime-only path", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Runtime-owned Orleans attachment should reject competing Orleans ownership added after runtime attachment.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationRejectsCompetingOrleansOwnershipAddedAfterRuntimeAttachment()
    {
        WebApplicationBuilder builder = CreateBuilder();
        builder.AddMississippiRuntime();
        AddCompetingOrleansOwnershipMarker(builder.Services);

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => state.ApplyOrleansConfiguration(new TestSiloBuilder()));

        Assert.Contains("runtime owns the top-level Orleans silo attachment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("MississippiRuntimeBuilder.Orleans(...)", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Runtime-owned Orleans attachment should reject competing Orleans ownership introduced through queued callback execution.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationRejectsCompetingOrleansOwnershipAddedThroughQueuedCallback()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        runtimeBuilder.Orleans(silo => AddCompetingOrleansOwnershipMarker(silo.Services));

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => state.ApplyOrleansConfiguration(new TestSiloBuilder()));

        Assert.Contains("runtime owns the top-level Orleans silo attachment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("MississippiRuntimeBuilder.Orleans(...)", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Runtime-owned Orleans attachment should reject endpoint ownership overrides introduced through queued callback execution.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationRejectsEndpointOwnershipOverrideAddedThroughQueuedCallback()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        runtimeBuilder.Orleans(silo => silo.ConfigureEndpoints(11111, 30000));

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => state.ApplyOrleansConfiguration(new TestSiloBuilder()));

        AssertFrozenOwnershipViolation(exception, "endpoint");
    }

    /// <summary>
    ///     Runtime-owned Orleans attachment should reject clustering ownership overrides introduced through queued callback execution.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationRejectsClusteringOwnershipOverrideAddedThroughQueuedCallback()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        runtimeBuilder.Orleans(silo => silo.UseLocalhostClustering());

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => state.ApplyOrleansConfiguration(new TestSiloBuilder()));

        AssertFrozenOwnershipViolation(exception, "clustering");
    }

    /// <summary>
    ///     Runtime-owned Orleans attachment should reject storage ownership overrides introduced through queued callback execution.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationRejectsStorageOwnershipOverrideAddedThroughQueuedCallback()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        runtimeBuilder.Orleans(silo => silo.AddMemoryGrainStorage("forbidden-store"));

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => state.ApplyOrleansConfiguration(new TestSiloBuilder()));

        AssertFrozenOwnershipViolation(exception, "storage");
    }

    /// <summary>
    ///     Runtime-owned Orleans attachment should reject provider ownership overrides introduced through queued callback execution.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationRejectsProviderOwnershipOverrideAddedThroughQueuedCallback()
    {
        WebApplicationBuilder builder = CreateBuilder();
        MississippiRuntimeBuilder runtimeBuilder = builder.AddMississippiRuntime();
        runtimeBuilder.Orleans(silo => silo.AddMemoryStreams("forbidden-provider"));

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => state.ApplyOrleansConfiguration(new TestSiloBuilder()));

        AssertFrozenOwnershipViolation(exception, "provider");
    }

    /// <summary>
    ///     Runtime-owned Orleans attachment should reject unsafe endpoint overrides added after runtime attachment.
    /// </summary>
    [Fact]
    public void ApplyOrleansConfigurationRejectsUnsafeEndpointOverrideAddedAfterRuntimeAttachment()
    {
        WebApplicationBuilder builder = CreateBuilder(Environments.Production);
        builder.AddMississippiRuntime();
        builder.Configuration["ConnectionStrings:cosmos"] = $"AccountEndpoint=http://localhost:8081/;AccountKey={SecretValue};";

        MississippiRuntimeBuilderState state = GetState(builder.Services);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => state.ApplyOrleansConfiguration(new TestSiloBuilder()));

        Assert.Contains("[config-trust]", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings:cosmos", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("localhost", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretValue, exception.Message, StringComparison.Ordinal);
    }
}