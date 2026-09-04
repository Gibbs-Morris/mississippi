using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;
using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Provides an in-memory SignalR transport and captures externally dispatched actions.
/// </summary>
internal sealed class InletSignalRActionEffectFixture : IAsyncDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InletSignalRActionEffectFixture" /> class.
    /// </summary>
    public InletSignalRActionEffectFixture()
    {
        Connection = new(
            Mock.Of<IConnectionFactory>(),
            new JsonHubProtocol(),
            new IPEndPoint(IPAddress.Loopback, 0),
            Mock.Of<IServiceProvider>(),
            NullLoggerFactory.Instance)
        {
            CallBase = true,
        };
        Connection.Setup(connection => connection.InvokeCoreAsync(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("subscription-1");
        Provider.SetupGet(provider => provider.Connection).Returns(Connection.Object);
        Provider.Setup(provider => provider.EnsureConnectedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Provider.Setup(provider => provider.RegisterHandler(
                InletHubConstants.ProjectionUpdatedMethod,
                It.IsAny<Func<string, string, long, Task>>()))
            .Callback<string, Func<string, string, long, Task>>((_, handler) => UpdateHandler = handler)
            .Returns(CallbackRegistration.Object);
        Provider.Setup(provider => provider.OnReconnected(It.IsAny<Func<string?, Task>>()))
            .Callback<Func<string?, Task>>(handler => ReconnectedHandler = handler);
        Registry.Setup(registry => registry.GetPath(typeof(TestProjection))).Returns("accounts");
        Registry.Setup(registry => registry.GetDtoType("accounts")).Returns(typeof(TestProjection));
        Store.Setup(store => store.Dispatch(It.IsAny<IAction>())).Callback<IAction>(DispatchedActions.Add);
        Fetcher.Setup(fetcher => fetcher.FetchAsync(
                typeof(TestProjection),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                ProjectionFetchResult.Create(
                    new TestProjection
                    {
                        Name = "initial",
                    },
                    3));
        Effect = new(new(() => Store.Object), Provider.Object, Fetcher.Object, Registry.Object, Time);
    }

    /// <summary>Gets the notification registration.</summary>
    public Mock<IDisposable> CallbackRegistration { get; } = new();

    /// <summary>Gets the in-memory hub connection.</summary>
    public Mock<HubConnection> Connection { get; }

    /// <summary>Gets the actions dispatched by server callbacks.</summary>
    public List<IAction> DispatchedActions { get; } = [];

    /// <summary>Gets the effect under test.</summary>
    public InletSignalRActionEffect Effect { get; }

    /// <summary>Gets the configurable projection fetcher.</summary>
    public Mock<IProjectionFetcher> Fetcher { get; } = new();

    /// <summary>Gets the connection provider.</summary>
    public Mock<IHubConnectionProvider> Provider { get; } = new();

    /// <summary>Gets the DTO path registry.</summary>
    public Mock<IProjectionDtoRegistry> Registry { get; } = new();

    /// <summary>Gets the store that receives callback actions.</summary>
    public Mock<IInletStore> Store { get; } = new();

    /// <summary>Gets the deterministic callback clock.</summary>
    public FakeTimeProvider Time { get; } = new(new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero));

    private Func<string?, Task>? ReconnectedHandler { get; set; }

    private Func<string, string, long, Task>? UpdateHandler { get; set; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Effect.DisposeAsync();
        await Connection.Object.DisposeAsync();
    }

    /// <summary>Delivers a server notification through the registered callback.</summary>
    /// <param name="path">The projection path.</param>
    /// <param name="entityId">The updated entity.</param>
    /// <param name="version">The announced projection version.</param>
    /// <returns>A task representing notification handling.</returns>
    public Task NotifyAsync(
        string path,
        string entityId,
        long version
    ) =>
        (UpdateHandler ?? throw new InvalidOperationException("No update handler registered."))(
            path,
            entityId,
            version);

    /// <summary>Delivers a successful reconnect through the registered callback.</summary>
    /// <returns>A task representing reconnect handling.</returns>
    public Task ReconnectAsync() =>
        (ReconnectedHandler ?? throw new InvalidOperationException("No reconnect handler registered."))("connection-2");

    /// <summary>Collects actions emitted by the effect for one request.</summary>
    /// <param name="action">The incoming request action.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The actions emitted by the effect.</returns>
    public async Task<List<IAction>> RunAsync(
        IAction action,
        CancellationToken cancellationToken = default
    )
    {
        List<IAction> actions = [];
        await foreach (IAction result in Effect.HandleAsync(action, new(), cancellationToken))
        {
            actions.Add(result);
        }

        return actions;
    }
}