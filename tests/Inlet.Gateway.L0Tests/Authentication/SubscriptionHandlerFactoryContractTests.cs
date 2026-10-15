using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Inlet.Gateway;
using Mississippi.Inlet.Gateway.Authentication;


namespace MississippiTests.Inlet.Gateway.L0Tests.Authentication;

/// <summary>
///     Checks fresh handler ownership when a valid scoped factory aliases an existing keyed registration.
/// </summary>
public sealed class SubscriptionHandlerFactoryContractTests
{
    /// <summary>
    ///     Replaying a scoped factory must not return the host-owned handler as a decision-owned instance.
    /// </summary>
    /// <returns>A task that completes when factory identity and ownership have been checked.</returns>
    [Fact]
    public async Task ScopedFactoryMustCreateDistinctDecisionOwnedHandlers()
    {
        const string handlerKey = "handler-implementation";
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddInletServer();
        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, HeaderPermissionAuthenticationHandler>("Other", _ => { });
        builder.Services.AddScoped<ConnectionHandlerScope>();
        builder.Services.AddKeyedScoped<HeaderPermissionAuthenticationHandler>(
            handlerKey,
            (services, _) => new(
                services.GetRequiredService<IOptionsMonitor<AuthenticationSchemeOptions>>(),
                services.GetRequiredService<ILoggerFactory>(),
                services.GetRequiredService<UrlEncoder>(),
                services.GetRequiredService<ConnectionHandlerScope>(),
                "host-factory"));
        builder.Services.AddScoped(services =>
            services.GetRequiredKeyedService<HeaderPermissionAuthenticationHandler>(handlerKey));
        await using WebApplication app = builder.Build();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        HeaderPermissionAuthenticationHandler hostOwned =
            scope.ServiceProvider.GetRequiredKeyedService<HeaderPermissionAuthenticationHandler>(handlerKey);
        AuthenticationService? framework = ConnectionAuthenticationRouter.GetFrameworkAuthentication(
            scope.ServiceProvider.GetRequiredService<IAuthenticationService>());
        Assert.NotNull(framework);
        IAuthenticationSchemeProvider schemes =
            scope.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        DefaultHttpContext firstContext = new()
        {
            RequestServices = scope.ServiceProvider,
        };
        DefaultHttpContext secondContext = new()
        {
            RequestServices = scope.ServiceProvider,
        };
        await using SubscriptionAuthenticationServices firstServices = new(
            scope.ServiceProvider,
            framework,
            schemes,
            firstContext,
            () => firstContext.User);
        await using SubscriptionAuthenticationServices secondServices = new(
            scope.ServiceProvider,
            framework,
            schemes,
            secondContext,
            () => secondContext.User);
        firstContext.RequestServices = firstServices;
        secondContext.RequestServices = secondServices;
        HeaderPermissionAuthenticationHandler first = Assert.IsType<HeaderPermissionAuthenticationHandler>(
            firstServices.GetService(typeof(HeaderPermissionAuthenticationHandler)));
        HeaderPermissionAuthenticationHandler second = Assert.IsType<HeaderPermissionAuthenticationHandler>(
            secondServices.GetService(typeof(HeaderPermissionAuthenticationHandler)));
        Assert.NotSame(first, second);
        Assert.NotSame(hostOwned, first);
        Assert.NotSame(hostOwned, second);
        await firstServices.DisposeAsync();
        await secondServices.DisposeAsync();
        Assert.False(hostOwned.IsDisposed);
        ConnectionHandlerScope state = scope.ServiceProvider.GetRequiredService<ConnectionHandlerScope>();
        Assert.Equal(2, state.Disposals);
        await scope.DisposeAsync();
        Assert.True(hostOwned.IsDisposed);
        Assert.Equal(3, state.Disposals);
    }
}