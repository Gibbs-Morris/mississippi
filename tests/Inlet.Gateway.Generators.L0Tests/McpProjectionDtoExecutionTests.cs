using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using Mississippi.Brooks.Serialization.Abstractions;
using Mississippi.Inlet.Runtime.Generators;

using ModelContextProtocol.Server;

using Moq;


namespace Mississippi.Inlet.Gateway.Generators.L0Tests;

/// <summary>
///     Compiles and executes generated projection consumers against the real transport and mapping contracts.
/// </summary>
public sealed class McpProjectionDtoExecutionTests
{
    private static Assembly CompileConsumer(
        bool hasHttpEndpoints,
        AssemblyLoadContext loadContext,
        bool missingHttpPath = false,
        bool embeddedProjectionName = false,
        bool includeSiloRegistrations = false,
        string? jsonIgnoreAttribute = null,
        bool nullSecret = false
    )
    {
        string projectionSource = $$"""
                                    using System.Collections.Immutable;
                                    using System.Text.Json.Serialization;
                                    using Mississippi.Brooks.Abstractions.Attributes;
                                    using Mississippi.Inlet.Abstractions;
                                    using Mississippi.Inlet.Generators.Abstractions;
                                    using Orleans;

                                    [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestApp.Server")]
                                    namespace TestApp.Domain.Projections.Probe;

                                    public enum ProbeStatus { Unknown, Active }
                                    [GenerateSerializer, Alias("TestApp.Domain.Projections.Probe.ProbeItem")]
                                    public sealed record ProbeItem
                                    {
                                        [Id(0)] public string Id { get; init; } = string.Empty;
                                        {{jsonIgnoreAttribute}}
                                        [Id(1)] public string? Secret { get; init; }
                                    }

                                    [GenerateMcpReadTool(ToolName = "read_probe", Title = "Read Probe")]
                                    [BrookName("TESTAPP", "TEST", "PROBE")]
                                    [SnapshotStorageName("TESTAPP", "TEST", "PROBE", version: 1)]
                                    [GenerateSerializer, Alias("TestApp.Domain.Projections.Probe.ProbeProjection")]
                                    {{(hasHttpEndpoints ? "[GenerateProjectionEndpoints, ProjectionPath(\"probe\")]" : string.Empty)}}
                                    {{(missingHttpPath ? "[GenerateProjectionEndpoints]" : string.Empty)}}
                                    public sealed record ProbeProjection
                                    {
                                        [Id(0)] public decimal Amount { get; init; }
                                        [Id(1)] public ProbeStatus Status { get; init; }
                                        [Id(2)] public ImmutableArray<ProbeItem> Items { get; init; } = [];
                                        {{jsonIgnoreAttribute}}
                                        [Id(3)] public string? Secret { get; init; }
                                    }
                                    {{(includeSiloRegistrations ? """
                                                                  [EventStorageName("TESTAPP", "TEST", "AMOUNTSET", version: 1)]
                                                                  [GenerateSerializer, Alias("TestApp.Domain.Projections.Probe.ProbeAmountSet")]
                                                                  internal sealed record ProbeAmountSet { [Id(0)] public decimal Amount { get; init; } }
                                                                  """ : string.Empty)}}
                                    """;
        if (embeddedProjectionName)
        {
            projectionSource = projectionSource.Replace("Probe", "ProjectionProbe", StringComparison.Ordinal);
        }

        MetadataReference[] references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
        CSharpCompilation domain = CSharpCompilation.Create(
            "TestApp.Domain." + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(projectionSource)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
        if (includeSiloRegistrations)
        {
            domain = domain.AddSyntaxTrees(
                CSharpSyntaxTree.ParseText(
                    """
                    using Mississippi.Tributary.Abstractions;
                    namespace TestApp.Domain.Projections.Probe.Reducers;
                    internal sealed class ProbeAmountSetReducer : EventReducerBase<ProbeAmountSet, ProbeProjection>
                    {
                        protected override ProbeProjection ReduceCore(ProbeProjection state, ProbeAmountSet eventData)
                            => state with { Amount = eventData.Amount };
                    }
                    """));
        }

        Assert.Empty(domain.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning));
        using MemoryStream domainStream = new();
        EmitResult domainResult = domain.Emit(domainStream);
        Assert.True(domainResult.Success, string.Join(Environment.NewLine, domainResult.Diagnostics));
        domainStream.Position = 0;
        loadContext.LoadFromStream(domainStream);
        CSharpCompilation input = CSharpCompilation.Create(
            "TestApp.Server",
            references: references.Append(MetadataReference.CreateFromImage(domainStream.ToArray())),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
        Assert.Empty(input.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        IIncrementalGenerator[] generators =
        [
            new ProjectionEndpointsGenerator(),
            new McpProjectionToolsGenerator(),
            new DomainServerRegistrationGenerator(),
            new McpToolRegistrationGenerator(),
        ];
        if (includeSiloRegistrations)
        {
            generators =
                [.. generators, new ProjectionSiloRegistrationGenerator(), new DomainSiloRegistrationGenerator()];
        }

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            input,
            out Compilation output,
            out ImmutableArray<Diagnostic> diagnostics);
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        string[] names = driver.GetRunResult()
            .Results.SelectMany(result => result.GeneratedSources)
            .Select(source => embeddedProjectionName
                ? source.HintName.Replace("ProjectionProbe", "Probe", StringComparison.Ordinal)
                : source.HintName)
            .ToArray();
        Assert.Single(names, name => name == "ProbeDto.g.cs");
        Assert.Single(names, name => name == "ProbeProjectionMapper.g.cs");
        Assert.Single(names, name => name == "ProbeProjectionMapperRegistrations.g.cs");
        Assert.Equal(hasHttpEndpoints, names.Contains("ProbeController.g.cs", StringComparer.Ordinal));
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning));
        string consumerSource = ConsumerSource(hasHttpEndpoints, includeSiloRegistrations, nullSecret);
        if (embeddedProjectionName)
        {
            consumerSource = consumerSource.Replace("Probe", "ProjectionProbe", StringComparison.Ordinal);
        }

        output = output.AddSyntaxTrees(CSharpSyntaxTree.ParseText(consumerSource));
        using MemoryStream outputStream = new();
        EmitResult result = output.Emit(outputStream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.Empty(result.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));
        outputStream.Position = 0;
        Assembly assembly = loadContext.LoadFromStream(outputStream);
        string toolsTypeName = embeddedProjectionName ? "ProjectionProbeMcpTools" : "ProbeMcpTools";
        Type toolsType = assembly.GetType("TestApp.Server.McpTools." + toolsTypeName, true)!;
        foreach ((string methodName, string toolName) in new[]
                 {
                     ("GetProbeAsync", "read_probe"),
                     ("GetProbeAtVersionAsync", "read_probe_at_version"),
                     ("GetProbeVersionAsync", "read_probe_version"),
                 })
        {
            MethodInfo method = toolsType.GetMethod(
                embeddedProjectionName
                    ? methodName.Replace("Probe", "ProjectionProbe", StringComparison.Ordinal)
                    : methodName)!;
            Assert.Equal(toolName, method.GetCustomAttribute<McpServerToolAttribute>()!.Name);
            string[] parameters = methodName == "GetProbeAtVersionAsync"
                ? ["entityId", "version", "cancellationToken"]
                : ["entityId", "cancellationToken"];
            Assert.Equal(parameters, method.GetParameters().Select(parameter => parameter.Name));
        }

        return assembly;
    }

    private static string ConsumerSource(
        bool hasHttpEndpoints,
        bool includeSiloRegistrations,
        bool nullSecret
    ) =>
        $$"""
          using System;
          using System.Collections.Immutable;
          using System.Linq;
          using System.Text.Json;
          using System.Threading;
          using System.Threading.Tasks;
          using Microsoft.AspNetCore.Http;
          using Microsoft.AspNetCore.Mvc;
          using Microsoft.Extensions.DependencyInjection;
          using Microsoft.Extensions.Hosting;
          using Mississippi.Brooks.Abstractions;
          using Mississippi.Common.Abstractions.Mapping;
          using Mississippi.DomainModeling.Abstractions;
          using TestApp.Domain.Projections.Probe;
          using TestApp.Server.Controllers.Mappers;
          using TestApp.Server.Controllers.Projections;
          using TestApp.Server.McpTools;
          using Xunit;

          public static class Consumer
          {
              {{(includeSiloRegistrations ? """
                                            public static decimal ReduceUsingSilo(Mississippi.Brooks.Serialization.Abstractions.ISerializationProvider serializer)
                                            {
                                                var services = new ServiceCollection();
                                                services.AddSingleton(serializer);
                                                services.AddSingleton<IUxProjectionGrainFactory>(new ProbeFactory(new ProbeGrain(null, null, default)));
                                                TestApp.Server.Registrations.DomainSiloRegistrations.AddTestAppDomainSilo(services);
                                                using var provider = services.BuildServiceProvider(new ServiceProviderOptions
                                                    { ValidateOnBuild = true, ValidateScopes = true });
                                                Assert.NotNull(provider.GetRequiredService<Mississippi.Tributary.Abstractions.ISnapshotStateConverter<ProbeProjection>>());
                                                var reducer = provider.GetRequiredService<Mississippi.Tributary.Abstractions.IEventReducer<ProbeAmountSet, ProbeProjection>>();
                                                return reducer.Reduce(new ProbeProjection(), new ProbeAmountSet { Amount = 99m }).Amount;
                                            }
                                            """ : string.Empty)}}
              public static async Task<string[]> RunAsync(bool customMapper, bool missing, CancellationToken token)
              {
                  var latest = new ProbeProjection { Amount = 123m, Status = ProbeStatus.Active,
                      Secret = {{(nullSecret ? "null" : "\"sensitive\"")}},
                      Items = [new ProbeItem { Id = "latest", Secret = {{(nullSecret ? "null" : "\"sensitive\"")}} }] };
                  var historical = latest with { Amount = 100m, Items = [latest.Items[0] with { Id = "historical" }] };
                  var grain = new ProbeGrain(missing ? null : latest, missing ? null : historical, token);
                  IUxProjectionGrainFactory factory = new ProbeFactory(grain);
                  var hostBuilder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
                  var services = hostBuilder.Services;
                  services.AddLogging();
                  services.AddSingleton(factory);
                  services.AddTestAppDomainServer();
                  services.AddMcpServer().WithHttpTransport().WithGeneratedMcpTools();
                  var mapper = new DistinguishableMapper();
                  if (customMapper) { services.AddSingleton<IMapper<ProbeProjection, ProbeDto>>(mapper); }
                  hostBuilder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
                      { ValidateOnBuild = true, ValidateScopes = true }));
                  using var host = hostBuilder.Build();
                  var provider = host.Services;
                  var tools = ActivatorUtilities.CreateInstance<ProbeMcpTools>(provider);
                  string version = await tools.GetProbeVersionAsync("entity", token);
                  Assert.Equal(0, mapper.Calls);
                  Assert.Equal(0, grain.LatestCalls);
                  Assert.Equal(0, grain.ExactCalls);
                  string current = await tools.GetProbeAsync("entity", token);
                  string exact = await tools.GetProbeAtVersionAsync("entity", 42, token);
                  string httpCurrent = string.Empty;
                  string httpExact = string.Empty;
                  {{(hasHttpEndpoints ? """
                                        var controller = ActivatorUtilities.CreateInstance<ProbeController>(provider);
                                        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
                                        var currentResult = await controller.GetAsync("entity", token);
                                        var exactResult = await controller.GetAtVersionAsync("entity", 42, token);
                                        if (missing)
                                        {
                                            Assert.IsType<NotFoundResult>(currentResult.Result);
                                            Assert.IsType<NotFoundResult>(exactResult.Result);
                                        }
                                        else
                                        {
                                            httpCurrent = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(currentResult.Result).Value,
                                                JsonSerializerOptions.Web);
                                            httpExact = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(exactResult.Result).Value,
                                                JsonSerializerOptions.Web);
                                        }
                                        """ : string.Empty)}}
                  Assert.Equal({{(hasHttpEndpoints ? "2" : "1")}} -
                      (missing && {{(hasHttpEndpoints ? "true" : "false")}} ? 1 : 0), grain.LatestCalls);
                  Assert.Equal({{(hasHttpEndpoints ? "2" : "1")}}, grain.ExactCalls);
                  if (missing) { Assert.Equal(0, mapper.Calls); }
                  return [current, exact, version, httpCurrent, httpExact];
              }
          }

          public sealed class DistinguishableMapper : IMapper<ProbeProjection, ProbeDto>
          {
              public int Calls { get; private set; }
              public ProbeDto Map(ProbeProjection source)
              {
                  Calls++;
                  return new ProbeDto { Amount = source.Amount + 1000m, Status = (ProbeStatusDto)source.Status,
                      Secret = source.Secret,
                      Items = source.Items.Select(item => new ProbeItemDto { Id = "mapped-" + item.Id, Secret = item.Secret }).ToImmutableArray() };
              }
          }

          public sealed class ProbeGrain(ProbeProjection? latest, ProbeProjection? historical, CancellationToken expectedToken)
              : IUxProjectionGrain<ProbeProjection>
          {
              public int LatestCalls { get; private set; }
              public int ExactCalls { get; private set; }
              public ValueTask<ProbeProjection?> GetAsync(CancellationToken token = default)
              {
                  Assert.Equal(expectedToken, token);
                  LatestCalls++;
                  return new(latest);
              }
              public ValueTask<ProbeProjection?> GetAtVersionAsync(BrookPosition version, CancellationToken token = default)
              {
                  Assert.Equal(expectedToken, token);
                  if (version.Value == 47)
                  {
                      LatestCalls++;
                      return new(latest);
                  }
                  Assert.Equal(42, version.Value);
                  ExactCalls++;
                  return new(historical);
              }
              public ValueTask<BrookPosition> GetLatestVersionAsync(CancellationToken token = default)
              {
                  Assert.Equal(expectedToken, token);
                  return new(new BrookPosition(latest is null ? -1 : 47));
              }
          }

          public sealed class ProbeFactory(ProbeGrain grain) : IUxProjectionGrainFactory
          {
              public IUxProjectionGrain<T> GetUxProjectionGrain<T>(string entityId) where T : class
              {
                  Assert.Equal("entity", entityId);
                  Assert.Equal(typeof(ProbeProjection), typeof(T));
                  return (IUxProjectionGrain<T>)(object)grain;
              }
              public IUxProjectionCursorGrain GetUxProjectionCursorGrain(UxProjectionCursorKey key) => throw new NotSupportedException();
              public IUxProjectionCursorGrain GetUxProjectionCursorGrain<T>(string entityId) where T : class => throw new NotSupportedException();
              public IUxProjectionVersionedCacheGrain<T> GetUxProjectionVersionedCacheGrain<T>(UxProjectionVersionedCacheKey key) => throw new NotSupportedException();
              public IUxProjectionVersionedCacheGrain<T> GetUxProjectionVersionedCacheGrain<T>(string entityId, BrookPosition version) where T : class => throw new NotSupportedException();
          }
          """;

    private static async Task<string[]> ExecuteAsync(
        bool hasHttpEndpoints,
        bool customMapper,
        bool missing,
        bool missingHttpPath = false,
        bool embeddedProjectionName = false,
        string? jsonIgnoreAttribute = null,
        bool nullSecret = false
    )
    {
        AssemblyLoadContext loadContext = new(Guid.NewGuid().ToString("N"), true);
        try
        {
            Assembly assembly = CompileConsumer(
                hasHttpEndpoints,
                loadContext,
                missingHttpPath,
                embeddedProjectionName,
                jsonIgnoreAttribute: jsonIgnoreAttribute,
                nullSecret: nullSecret);
            using CancellationTokenSource cancellation = new();
            MethodInfo method = assembly.GetType("Consumer", true)!.GetMethod("RunAsync")!;
            Task<string[]> task = (Task<string[]>)method.Invoke(null, [customMapper, missing, cancellation.Token])!;
            return await task;
        }
        finally
        {
            loadContext.Unload();
        }
    }

    /// <summary>
    ///     A dual marker without a route still supplies mapped MCP reads without an HTTP controller.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task DualMarkerWithoutPathReadsMappedDtoAsync()
    {
        string[] results = await ExecuteAsync(false, true, false, true);
        using JsonDocument latest = JsonDocument.Parse(results[0]);
        using JsonDocument exact = JsonDocument.Parse(results[1]);
        Assert.Equal(1123m, latest.RootElement.GetProperty("amount").GetDecimal());
        Assert.Equal(1100m, exact.RootElement.GetProperty("amount").GetDecimal());
    }

    /// <summary>
    ///     A projection name containing the suffix word retains its leading name through all generated registrations.
    /// </summary>
    /// <param name="hasHttpEndpoints">Whether the projection also opts into HTTP endpoints.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmbeddedProjectionNameResolvesMappingsAsync(
        bool hasHttpEndpoints
    )
    {
        string[] results = await ExecuteAsync(hasHttpEndpoints, true, false, embeddedProjectionName: true);
        using JsonDocument latest = JsonDocument.Parse(results[0]);
        Assert.Equal(1123m, latest.RootElement.GetProperty("amount").GetDecimal());
    }

    /// <summary>
    ///     Source JSON exclusions survive mapping for root and collection-item properties on both transports.
    /// </summary>
    /// <param name="attribute">The source property's JSON exclusion metadata.</param>
    /// <param name="hasHttpEndpoints">Whether the projection also opts into HTTP endpoints.</param>
    /// <param name="customMapper">Whether the host replaces the generated mapping.</param>
    /// <param name="nullSecret">Whether the source values are null.</param>
    /// <param name="serialized">Whether the selected condition permits the values in the response.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData("[JsonIgnore]", true, false, false, false)]
    [InlineData("[JsonIgnore]", false, false, false, false)]
    [InlineData("[JsonIgnore]", true, true, false, false)]
    [InlineData("[JsonIgnore]", false, true, false, false)]
    [InlineData("[JsonIgnore(Condition = JsonIgnoreCondition.Always)]", true, false, false, false)]
    [InlineData("[JsonIgnore(Condition = JsonIgnoreCondition.Never)]", true, false, false, true)]
    [InlineData("[JsonIgnore(Condition = JsonIgnoreCondition.Never)]", false, true, true, true)]
    [InlineData("[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]", true, false, true, false)]
    [InlineData("[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]", false, true, false, true)]
    [InlineData("[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]", false, true, true, false)]
    [InlineData("[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]", true, false, false, true)]
    [InlineData("[JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]", false, false, false, false)]
    [InlineData("[JsonIgnore(Condition = JsonIgnoreCondition.WhenReading)]", true, true, false, true)]
    public async Task JsonExclusionsSurviveMappingAsync(
        string attribute,
        bool hasHttpEndpoints,
        bool customMapper,
        bool nullSecret,
        bool serialized
    )
    {
        string[] results = await ExecuteAsync(
            hasHttpEndpoints,
            customMapper,
            false,
            jsonIgnoreAttribute: attribute,
            nullSecret: nullSecret);
        string[] responses = hasHttpEndpoints
            ? [results[0], results[1], results[3], results[4]]
            : [results[0], results[1]];
        foreach (JsonDocument json in responses.Select(value => JsonDocument.Parse(value)))
        {
            using (json)
            {
                using JsonElement.ObjectEnumerator rootProperties = json.RootElement.EnumerateObject();
                using JsonElement.ObjectEnumerator itemProperties =
                    json.RootElement.GetProperty("items")[0].EnumerateObject();
                Assert.Equal(serialized, rootProperties.Any(property => property.NameEquals("secret")));
                Assert.Equal(serialized, itemProperties.Any(property => property.NameEquals("secret")));
            }
        }
    }

    /// <summary>
    ///     Missing projection and version results preserve their messages without invoking a mapper.
    /// </summary>
    /// <param name="hasHttpEndpoints">Whether the projection also opts into HTTP endpoints.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingReadsPreserveMessagesAsync(
        bool hasHttpEndpoints
    )
    {
        string[] results = await ExecuteAsync(hasHttpEndpoints, true, true);
        Assert.Equal("No Probe found for entity 'entity'.", results[0]);
        Assert.Equal("No Probe found for entity 'entity' at version 42.", results[1]);
        Assert.Equal("No events found for Probe entity 'entity'.", results[2]);
    }

    /// <summary>
    ///     Latest and historical reads use the generated DTO and the selected mapper through supported registration.
    /// </summary>
    /// <param name="hasHttpEndpoints">Whether the projection also opts into HTTP endpoints.</param>
    /// <param name="customMapper">Whether the host overrides the generated mapper.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task ReadsUseSharedMappedDtoAsync(
        bool hasHttpEndpoints,
        bool customMapper
    )
    {
        string[] results = await ExecuteAsync(hasHttpEndpoints, customMapper, false);
        using JsonDocument latest = JsonDocument.Parse(results[0]);
        using JsonDocument exact = JsonDocument.Parse(results[1]);
        decimal offset = customMapper ? 1000m : 0m;
        Assert.Equal(
            [123m + offset, 100m + offset],
            new[]
            {
                latest.RootElement.GetProperty("amount").GetDecimal(),
                exact.RootElement.GetProperty("amount").GetDecimal(),
            });
        string prefix = customMapper ? "mapped-" : string.Empty;
        Assert.Equal(prefix + "latest", latest.RootElement.GetProperty("items")[0].GetProperty("id").GetString());
        Assert.Equal(prefix + "historical", exact.RootElement.GetProperty("items")[0].GetProperty("id").GetString());
        Assert.Equal(1, latest.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("47", results[2]);
        if (hasHttpEndpoints)
        {
            Assert.Equal(results[0], results[3]);
            Assert.Equal(results[1], results[4]);
        }
    }

    /// <summary>
    ///     The domain silo registration resolves and executes projection reducers for both transport declarations.
    /// </summary>
    /// <param name="hasHttpEndpoints">Whether the projection also opts into HTTP endpoints.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SiloRegistrationsMaterializeProjection(
        bool hasHttpEndpoints
    )
    {
        AssemblyLoadContext loadContext = new(Guid.NewGuid().ToString("N"), true);
        try
        {
            Assembly assembly = CompileConsumer(hasHttpEndpoints, loadContext, includeSiloRegistrations: true);
            MethodInfo method = assembly.GetType("Consumer", true)!.GetMethod("ReduceUsingSilo")!;
            Mock<ISerializationProvider> serializer = new();
            Assert.Equal(99m, method.Invoke(null, [serializer.Object]));
        }
        finally
        {
            loadContext.Unload();
        }
    }
}