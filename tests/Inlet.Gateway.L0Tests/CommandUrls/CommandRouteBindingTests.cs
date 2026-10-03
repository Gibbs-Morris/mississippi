using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Gateway.L0Tests.CommandUrls;

/// <summary>Verifies identifiers received by the command API's MVC binding contract.</summary>
public sealed class CommandRouteBindingTests
{
    private static async Task<(int Status, string? EntityId)> DispatchAsync(
        Uri uri
    )
    {
        CommandRouteBindingController controller = new();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(CommandRouteBindingController).Assembly)
            .ConfigureApplicationPartManager(manager =>
                manager.FeatureProviders.Add(new CommandRouteBindingFeatureProvider()))
            .AddControllersAsServices();
        builder.Services.AddSingleton(controller);
        using WebApplication host = builder.Build();
        ApplicationBuilder application = new(host.Services);
        application.UseRouting();
        application.UseEndpoints(endpoints => endpoints.MapControllers());
        RequestDelegate pipeline = application.Build();
        using MemoryStream responseBody = new();
        DefaultHttpContext context = new()
        {
            RequestServices = host.Services,
            RequestAborted = TestContext.Current.CancellationToken,
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.Scheme = uri.Scheme;
        context.Request.Host = HostString.FromUriComponent(uri);
        context.Request.Path = PathString.FromUriComponent(uri);
        context.Request.QueryString = new(uri.Query);
        context.Response.Body = responseBody;
        await pipeline(context);
        return (context.Response.StatusCode, controller.DispatchedEntityId);
    }

    /// <summary>Preserves a supported escaped identifier at the dispatch boundary.</summary>
    /// <param name="escapedSegment">The segment produced by the command client.</param>
    /// <param name="entityId">The exact original identifier.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData("customer-42", "customer-42")]
    [InlineData("customer%2342", "customer#42")]
    [InlineData("customer%3Fregion%3D42", "customer?region=42")]
    [InlineData("customer%252F42", "customer%2F42")]
    [InlineData("customer%252f42", "customer%2f42")]
    [InlineData("customer%252342", "customer%2342")]
    [InlineData("customer%253F42", "customer%3F42")]
    [InlineData("customer%2042", "customer 42")]
    [InlineData("customer%2B42", "customer+42")]
    [InlineData("%E5%AE%A2%E6%88%B742", "客户42")]
    [InlineData("customer%5C42", "customer\\42")]
    [InlineData("%252E", "%2E")]
    [InlineData("%252e%252e", "%2e%2e")]
    [InlineData("customer..42", "customer..42")]
    public async Task EscapedSegmentDispatchesExactIdentifier(
        string escapedSegment,
        string entityId
    )
    {
        (int status, string? dispatchedEntityId) = await DispatchAsync(
            new($"https://example.test/api/aggregates/customer/{escapedSegment}/submit"));
        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal(entityId, dispatchedEntityId);
    }

    /// <summary>Demonstrates why a default slash identifier cannot simply be escaped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task EscapedSlashCollidesWithLiteralPercentSequence()
    {
        (int slashStatus, string? slashId) = await DispatchAsync(
            new("https://example.test/api/aggregates/customer/customer%2F42/submit"));
        (int percentStatus, string? percentId) = await DispatchAsync(
            new("https://example.test/api/aggregates/customer/customer%252F42/submit"));
        Assert.Equal(StatusCodes.Status200OK, slashStatus);
        Assert.Equal(StatusCodes.Status200OK, percentStatus);
        Assert.Equal("customer%2F42", slashId);
        Assert.Equal(slashId, percentId);
        Assert.NotEqual("customer/42", slashId);
    }

    /// <summary>Confirms raw URL syntax can prevent the command route from dispatching.</summary>
    /// <param name="rawEntityId">The raw segment used by the original effect.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData("customer#42")]
    [InlineData("customer?region=42")]
    [InlineData(".")]
    [InlineData("..")]
    public async Task RawReservedSegmentCannotReachCommandDispatch(
        string rawEntityId
    )
    {
        (int status, string? dispatchedEntityId) = await DispatchAsync(
            new($"https://example.test/api/aggregates/customer/{rawEntityId}/submit"));
        Assert.Equal(StatusCodes.Status404NotFound, status);
        Assert.Null(dispatchedEntityId);
    }
}