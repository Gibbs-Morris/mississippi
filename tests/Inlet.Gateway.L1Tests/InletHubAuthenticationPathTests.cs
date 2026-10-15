using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Inlet.Gateway;
using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Inlet.Runtime.Abstractions;
using Mississippi.Inlet.Runtime.Grains;

using Moq;

using Orleans;


namespace MississippiTests.Inlet.Gateway.L1Tests;

/// <summary>
///     Verifies selected-scheme authentication through the real SignalR protocol and mapped hub.
/// </summary>
public sealed class InletHubAuthenticationPathTests
{
    private const string BearerScheme = "Bearer";

    private const string EntityId = "entity-1";

    private const string ProjectionPath = "/api/test-projection";

    /// <summary>
    ///     Identity labels must not change selected-scheme subscription decisions.
    /// </summary>
    /// <param name="identityLabel">The label carried by the authenticated ticket.</param>
    /// <param name="permission">The caller's projection permission.</param>
    /// <returns>A task that completes when protocol and authorization assertions pass.</returns>
    [Theory]
    [InlineData("AuthenticationTypes.Federation", "read")]
    [InlineData(BearerScheme, "read")]
    [InlineData("AuthenticationTypes.Federation", "write")]
    public async Task SelectedSchemeUsesAuthenticatedTicketThroughLongPolling(
        string identityLabel,
        string permission
    )
    {
        FakeTimeProvider clock = new(new(2040, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Mock<IInletSubscriptionGrain> grain = new();
        grain.Setup(value => value.SubscribeAsync(ProjectionPath, EntityId)).ReturnsAsync("subscription-1");
        grain.Setup(value => value.ClearAllAsync()).Returns(Task.CompletedTask);
        Mock<IGrainFactory> grainFactory = new();
        grainFactory.Setup(value => value.GetGrain<IInletSubscriptionGrain>(It.IsAny<string>(), null))
            .Returns(grain.Object);
        Mock<IProjectionAuthorizationRegistry> registry = new();
        registry.Setup(value => value.GetAuthorizationMetadata(ProjectionPath))
            .Returns(new ProjectionAuthorizationMetadata(null, null, BearerScheme, true, false));
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSignalR();
        builder.Services.AddSingleton(grainFactory.Object);
        builder.Services.AddSingleton(registry.Object);
        builder.Services.Configure<InletServerOptions>(_ => { });
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAuthentication(BearerScheme)
            .AddBearerToken(BearerScheme, options => options.TimeProvider = clock);
        builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireClaim("permission", "read").Build());
        await using WebApplication app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapInletHub()
            .RequireAuthorization(new AuthorizationPolicyBuilder(BearerScheme).RequireAuthenticatedUser().Build());
        await app.StartAsync(TestContext.Current.CancellationToken);
        BearerTokenOptions options = app.Services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>()
            .Get(BearerScheme);
        ClaimsPrincipal principal = new(
            new ClaimsIdentity(
                [new(ClaimTypes.NameIdentifier, "user-1"), new("permission", permission)],
                identityLabel));
        AuthenticationTicket ticket = new(
            principal,
            new()
            {
                ExpiresUtc = clock.GetUtcNow().AddHours(1),
            },
            BearerScheme + ":AccessToken");
        string token = options.BearerTokenProtector.Protect(ticket);
        using TestServer server = app.GetTestServer();
        await using HubConnection connection = new HubConnectionBuilder().WithUrl(
                new Uri(server.BaseAddress, "/hubs/inlet"),
                connectionOptions =>
                {
                    connectionOptions.Transports = HttpTransportType.LongPolling;
                    connectionOptions.HttpMessageHandlerFactory = _ => server.CreateHandler();
                    connectionOptions.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
            .Build();
        try
        {
            await connection.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
            Assert.Equal(HubConnectionState.Connected, connection.State);
            if (permission == "read")
            {
                string subscription = await connection.InvokeAsync<string>(
                    nameof(InletHub.SubscribeAsync),
                    ProjectionPath,
                    EntityId,
                    TestContext.Current.CancellationToken);
                Assert.Equal("subscription-1", subscription);
                grain.Verify(value => value.SubscribeAsync(ProjectionPath, EntityId), Times.Once());
            }
            else
            {
                HubException exception = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync<string>(
                    nameof(InletHub.SubscribeAsync),
                    ProjectionPath,
                    EntityId,
                    TestContext.Current.CancellationToken));
                Assert.Contains(
                    InletHubConstants.SubscriptionDeniedMessage,
                    exception.Message,
                    StringComparison.Ordinal);
                grain.Verify(value => value.SubscribeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            }
        }
        finally
        {
            await connection.StopAsync(TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
            await app.StopAsync(TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        }
    }
}