using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Events;


namespace Mississippi.Reservoir.Blazor;

/// <summary>
///     Background service that integrates the Reservoir store with Redux DevTools.
/// </summary>
/// <remarks>
///     <para>
///         This service subscribes to <see cref="IStore.StoreEvents" /> to observe store activity
///         and reports actions and state to the Redux DevTools browser extension via JavaScript interop.
///     </para>
///     <para>
///         Time-travel commands from DevTools (jump-to-state, reset, rollback, etc.) are translated
///         into system actions dispatched to the store, maintaining unidirectional data flow.
///     </para>
/// </remarks>
internal sealed class ReduxDevToolsService
    : IHostedService,
      IAsyncDisposable
{
    private readonly SemaphoreSlim devToolsLock = new(1, 1);

    private IReadOnlyDictionary<string, object> committedSnapshot;

    private bool devToolsConnected;

    private DotNetObjectReference<ReduxDevToolsService>? dotNetRef;

    private bool isDisposed;

    private IStore? resolvedStore;

    private IDisposable? storeEventsSubscription;

    private bool suppressDevToolsUpdates;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReduxDevToolsService" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The service provider for lazy resolution of scoped services.
    ///     Required because hosted services are singletons, but IStore may be scoped.
    /// </param>
    /// <param name="interop">The DevTools JavaScript interop.</param>
    /// <param name="options">The DevTools options.</param>
    /// <param name="hostEnvironment">The optional host environment for environment checks.</param>
    public ReduxDevToolsService(
        IServiceProvider serviceProvider,
        ReservoirDevToolsInterop interop,
        IOptions<ReservoirDevToolsOptions> options,
        IHostEnvironment? hostEnvironment = null
    )
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(interop);
        ArgumentNullException.ThrowIfNull(options);
        ServiceProvider = serviceProvider;
        Interop = interop;
        Options = options.Value;
        HostEnvironment = hostEnvironment;
        committedSnapshot = new Dictionary<string, object>(StringComparer.Ordinal);
    }

    private IHostEnvironment? HostEnvironment { get; }

    private ReservoirDevToolsInterop Interop { get; }

    private ReservoirDevToolsOptions Options { get; }

    private IServiceProvider ServiceProvider { get; }

    /// <summary>
    ///     Gets the store, lazily resolved from the service provider.
    /// </summary>
    /// <remarks>
    ///     In Blazor WASM, scoped services are effectively singletons (one scope for the app lifetime).
    ///     We resolve lazily to avoid DI lifetime issues during hosted service construction.
    /// </remarks>
    private IStore Store => resolvedStore ??= ServiceProvider.GetRequiredService<IStore>();

    private static string? TryExtractImportedStateJson(
        JsonElement payload
    )
    {
        if (!payload.TryGetProperty("nextLiftedState", out JsonElement nextLiftedState))
        {
            return null;
        }

        if (!nextLiftedState.TryGetProperty("computedStates", out JsonElement computedStates))
        {
            return null;
        }

        if (computedStates.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        using JsonElement.ArrayEnumerator enumerator = computedStates.EnumerateArray();
        JsonElement lastState = default;
        foreach (JsonElement element in enumerator)
        {
            lastState = element;
        }

        if (lastState.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!lastState.TryGetProperty("state", out JsonElement stateElement))
        {
            return null;
        }

        return stateElement.GetRawText();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        storeEventsSubscription?.Dispose();
        storeEventsSubscription = null;
        dotNetRef?.Dispose();
        dotNetRef = null;
        devToolsLock.Dispose();
        await Task.CompletedTask;
    }

    /// <summary>
    ///     Handles messages from the Redux DevTools extension.
    /// </summary>
    /// <param name="messageJson">The JSON message from DevTools.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [JSInvokable]
    public async Task OnDevToolsMessageAsync(
        string messageJson
    )
    {
        if (string.IsNullOrWhiteSpace(messageJson))
        {
            return;
        }

        ReservoirDevToolsMessage? message = DeserializeMessage(messageJson);
        if (message is null)
        {
            return;
        }

        await HandleDevToolsMessageAsync(message);
    }

    /// <inheritdoc />
    public Task StartAsync(
        CancellationToken cancellationToken
    )
    {
        if (!IsEnabled())
        {
            return Task.CompletedTask;
        }

        // Dispose any previous subscription before creating a new one
        storeEventsSubscription?.Dispose();

        // Initialize committed snapshot now that we can safely resolve the store
        // (Blazor WASM scoped services are resolvable after host construction)
        committedSnapshot = Store.GetStateSnapshot();

        // Subscribe to store events
        storeEventsSubscription = Store.StoreEvents.Subscribe(new StoreEventObserver(this));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(
        CancellationToken cancellationToken
    )
    {
        storeEventsSubscription?.Dispose();
        storeEventsSubscription = null;
        return Task.CompletedTask;
    }

    private Dictionary<string, object?> BuildOptionsPayload()
    {
        Dictionary<string, object?> payload = new(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(Options.Name))
        {
            payload["name"] = Options.Name;
        }

        if (Options.MaxAge.HasValue)
        {
            payload["maxAge"] = Options.MaxAge.Value;
        }

        if (Options.Latency.HasValue)
        {
            payload["latency"] = Options.Latency.Value;
        }

        if (Options.AutoPause.HasValue)
        {
            payload["autoPause"] = Options.AutoPause.Value;
        }

        foreach (KeyValuePair<string, object?> option in Options.AdditionalOptions)
        {
            payload[option.Key] = option.Value;
        }

        return payload;
    }

    private object CreateActionPayload(
        IAction action
    )
    {
        object? sanitized = Options.ActionSanitizer?.Invoke(action);
        if (sanitized is not null)
        {
            return sanitized;
        }

        // Serialize action using its runtime type to capture all properties.
        // IAction is a marker interface with no properties, so we must use
        // the concrete type for proper serialization to DevTools.
        string actionJson = JsonSerializer.Serialize(action, action.GetType(), Options.SerializerOptions);
        JsonElement actionElement = JsonSerializer.Deserialize<JsonElement>(actionJson, Options.SerializerOptions);
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["type"] = action.GetType().Name,
            ["payload"] = actionElement,
        };
    }

    private object CreateStatePayload(
        IReadOnlyDictionary<string, object> snapshot
    )
    {
        object? sanitized = Options.StateSanitizer?.Invoke(snapshot);
        if (sanitized is not null)
        {
            return sanitized;
        }

        // Serialize each feature state using its runtime type to capture all properties.
        // Feature states implement IFeatureState (marker interface), so we must use
        // concrete types for proper serialization to DevTools.
        Dictionary<string, JsonElement> serializedSnapshot = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object> kvp in snapshot)
        {
            string stateJson = JsonSerializer.Serialize(kvp.Value, kvp.Value.GetType(), Options.SerializerOptions);
            JsonElement stateElement = JsonSerializer.Deserialize<JsonElement>(stateJson, Options.SerializerOptions);
            serializedSnapshot[kvp.Key] = stateElement;
        }

        return serializedSnapshot;
    }

    private ReservoirDevToolsMessage? DeserializeMessage(
        string messageJson
    )
    {
        try
        {
            return JsonSerializer.Deserialize<ReservoirDevToolsMessage>(messageJson, Options.SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        if (!IsEnabled())
        {
            return false;
        }

        if (devToolsConnected)
        {
            return true;
        }

        await devToolsLock.WaitAsync();
        try
        {
            dotNetRef ??= DotNetObjectReference.Create(this);
            bool connected = await Interop.ConnectAsync(BuildOptionsPayload(), dotNetRef);
            devToolsConnected = connected;
            if (!connected)
            {
                return false;
            }

            await Interop.InitAsync(CreateStatePayload(Store.GetStateSnapshot()));

            // Note: committedSnapshot is NOT updated here - it was set in constructor
            // and should only be updated via explicit COMMIT command
            return true;
        }
        catch (JSException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        finally
        {
            devToolsLock.Release();
        }
    }

    /// <summary>
    ///     Executes an action while suppressing DevTools updates.
    /// </summary>
    /// <remarks>
    ///     Used when dispatching system actions that originate from DevTools itself
    ///     to prevent feedback loops.
    /// </remarks>
    private void ExecuteWithSuppression(
        Action action
    )
    {
        suppressDevToolsUpdates = true;
        try
        {
            action();
        }
        finally
        {
            suppressDevToolsUpdates = false;
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "JSON parsing failures should not crash the service")]
    private async Task HandleDevToolsMessageAsync(
        ReservoirDevToolsMessage message
    )
    {
        if (!string.Equals(message.Type, "DISPATCH", StringComparison.Ordinal))
        {
            return;
        }

        if (message.Payload is null)
        {
            return;
        }

        if (message.Payload.Value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (!message.Payload.Value.TryGetProperty("type", out JsonElement payloadTypeElement))
        {
            return;
        }

        string? payloadType = payloadTypeElement.GetString();
        if (string.IsNullOrWhiteSpace(payloadType))
        {
            return;
        }

        switch (payloadType)
        {
            case "JUMP_TO_STATE":
            case "JUMP_TO_ACTION":
                if (!string.IsNullOrWhiteSpace(message.State))
                {
                    bool stateApplied = TryRestoreStateFromJson(message.State);
                    if (!stateApplied)
                    {
                        // Re-sync DevTools with current store state when strict mode rejects the jump
                        await InitDevToolsAsync();
                    }
                }

                break;
            case "RESET":
                ExecuteWithSuppression(() => Store.Dispatch(new ResetToInitialStateAction()));
                await InitDevToolsAsync();
                break;
            case "ROLLBACK":
                ExecuteWithSuppression(() => Store.Dispatch(new RestoreStateAction(committedSnapshot)));
                await InitDevToolsAsync();
                break;
            case "COMMIT":
                committedSnapshot = Store.GetStateSnapshot();
                await InitDevToolsAsync();
                break;
            case "IMPORT_STATE":
                string? importedStateJson = TryExtractImportedStateJson(message.Payload.Value);
                if (!string.IsNullOrWhiteSpace(importedStateJson))
                {
                    TryRestoreStateFromJson(importedStateJson);
                }

                await InitDevToolsAsync();
                break;
        }
    }

    private async Task InitDevToolsAsync()
    {
        if (!devToolsConnected)
        {
            return;
        }

        await Interop.InitAsync(CreateStatePayload(Store.GetStateSnapshot()));
    }

    private bool IsEnabled()
    {
        return Options.Enablement switch
        {
            ReservoirDevToolsEnablement.Always => true,
            ReservoirDevToolsEnablement.DevelopmentOnly => HostEnvironment?.IsDevelopment() == true,
            var _ => false,
        };
    }

    private void OnStoreEvent(
        StoreEventBase storeEvent
    )
    {
        if (suppressDevToolsUpdates)
        {
            return;
        }

        switch (storeEvent)
        {
            case ActionDispatchedEvent actionDispatched:
                // Don't report system actions to DevTools
                if (actionDispatched.Action is ISystemAction)
                {
                    return;
                }

                // Fire-and-forget is intentional for DevTools reporting.
                _ = SendToDevToolsAsync(actionDispatched.Action, actionDispatched.StateSnapshot);
                break;
            case StateRestoredEvent:
                // State was restored - DevTools will be re-initialized by the handler
                break;
        }
    }

    private async Task SendToDevToolsAsync(
        IAction action,
        IReadOnlyDictionary<string, object> stateSnapshot
    )
    {
        if (!await EnsureConnectedAsync())
        {
            return;
        }

        try
        {
            object actionPayload = CreateActionPayload(action);
            object statePayload = CreateStatePayload(stateSnapshot);
            await Interop.SendAsync(actionPayload, statePayload);
        }
        catch (JSException)
        {
            // Ignore JS interop failures to keep app running.
        }
        catch (InvalidOperationException)
        {
            // Ignore JS interop failures to keep app running.
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "JSON parsing failures should not crash the service")]
    private bool TryRestoreStateFromJson(
        string stateJson
    )
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(stateJson);
            return TryRestoreStateFromJsonDocument(document);
        }
        catch (JsonException)
        {
            // Ignore invalid payloads.
            return false;
        }
        catch (Exception)
        {
            // Catch any other parsing failures.
            return false;
        }
    }

    private bool TryRestoreStateFromJsonDocument(
        JsonDocument document
    )
    {
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        IReadOnlyDictionary<string, object> currentSnapshot = Store.GetStateSnapshot();
        Dictionary<string, object> newStates = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object> current in currentSnapshot)
        {
            if (!root.TryGetProperty(current.Key, out JsonElement element))
            {
                if (Options.IsStrictStateRehydrationEnabled)
                {
                    // Strict mode: reject if any feature is missing.
                    return false;
                }

                continue;
            }

            Type stateType = current.Value.GetType();
            object? deserialized = null;
            try
            {
                deserialized = element.Deserialize(stateType, Options.SerializerOptions);
            }
            catch (JsonException)
            {
                // Deserialization failed.
            }

            if (deserialized is null)
            {
                if (Options.IsStrictStateRehydrationEnabled)
                {
                    // Strict mode: reject if any feature fails deserialization.
                    return false;
                }

                continue;
            }

            newStates[current.Key] = deserialized;
        }

        ExecuteWithSuppression(() => Store.Dispatch(new RestoreStateAction(newStates)));
        return true;
    }

    private sealed record ReservoirDevToolsMessage(string? Type, JsonElement? Payload, string? State);

    /// <summary>
    ///     Observer that forwards store events to the service.
    /// </summary>
    private sealed class StoreEventObserver : IObserver<StoreEventBase>
    {
        public StoreEventObserver(
            ReduxDevToolsService service
        ) =>
            Service = service;

        private ReduxDevToolsService Service { get; }

        public void OnCompleted()
        {
            // Store was disposed.
        }

        public void OnError(
            Exception error
        )
        {
            // Errors in the event stream are unexpected.
        }

        public void OnNext(
            StoreEventBase value
        )
        {
            Service.OnStoreEvent(value);
        }
    }
}