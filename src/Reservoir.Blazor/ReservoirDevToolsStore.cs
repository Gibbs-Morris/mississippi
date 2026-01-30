using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Blazor;

/// <summary>
///     A store implementation that reports actions and state to Redux DevTools.
/// </summary>
internal sealed class ReservoirDevToolsStore : Store
{
    private ReservoirDevToolsInterop Interop { get; }

    private ReservoirDevToolsOptions Options { get; }

    private IHostEnvironment? HostEnvironment { get; }

    private readonly SemaphoreSlim devToolsLock = new(1, 1);

    private DotNetObjectReference<ReservoirDevToolsStore>? dotNetRef;

    private bool devToolsConnected;

    private bool suppressDevToolsUpdates;

    private IReadOnlyDictionary<string, object> committedSnapshot;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirDevToolsStore"/> class.
    /// </summary>
    /// <param name="featureRegistrations">The feature state registrations.</param>
    /// <param name="middlewaresCollection">The middleware collection.</param>
    /// <param name="interop">The DevTools JavaScript interop.</param>
    /// <param name="options">The DevTools options.</param>
    /// <param name="hostEnvironment">The optional host environment for environment checks.</param>
    public ReservoirDevToolsStore(
        IEnumerable<IFeatureStateRegistration> featureRegistrations,
        IEnumerable<IMiddleware> middlewaresCollection,
        ReservoirDevToolsInterop interop,
        IOptions<ReservoirDevToolsOptions> options,
        IHostEnvironment? hostEnvironment = null
    )
        : base(featureRegistrations, middlewaresCollection)
    {
        ArgumentNullException.ThrowIfNull(interop);
        ArgumentNullException.ThrowIfNull(options);

        Interop = interop;
        Options = options.Value;
        HostEnvironment = hostEnvironment;
        committedSnapshot = CreateFeatureStateSnapshot();
    }

    /// <inheritdoc />
    protected override void OnActionDispatched(
        IAction action
    )
    {
        if (suppressDevToolsUpdates)
        {
            return;
        }

        // Fire-and-forget is intentional for DevTools reporting.
        _ = SendToDevToolsAsync(action);
    }

    /// <inheritdoc />
    protected override void Dispose(
        bool disposing
    )
    {
        if (disposing)
        {
            dotNetRef?.Dispose();
            dotNetRef = null;
            devToolsLock.Dispose();
        }

        base.Dispose(disposing);
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

    private bool IsEnabled()
    {
        return Options.Enablement switch
        {
            ReservoirDevToolsEnablement.Always => true,
            ReservoirDevToolsEnablement.DevelopmentOnly => HostEnvironment?.IsDevelopment() == true,
            _ => false,
        };
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
            if (devToolsConnected)
            {
                return true;
            }

            dotNetRef ??= DotNetObjectReference.Create(this);
            bool connected = await Interop.ConnectAsync(BuildOptionsPayload(), dotNetRef);
            devToolsConnected = connected;
            if (!connected)
            {
                return false;
            }

            await Interop.InitAsync(CreateStatePayload());
            committedSnapshot = CreateFeatureStateSnapshot();
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
                    ReplaceStateFromJson(message.State);
                }

                break;
            case "RESET":
                ReplaceStateFromSnapshot(CreateInitialFeatureStateSnapshot());
                await InitDevToolsAsync();
                break;
            case "ROLLBACK":
                ReplaceStateFromSnapshot(committedSnapshot);
                await InitDevToolsAsync();
                break;
            case "COMMIT":
                committedSnapshot = CreateFeatureStateSnapshot();
                await InitDevToolsAsync();
                break;
            case "IMPORT_STATE":
                string? importedStateJson = TryExtractImportedStateJson(message.Payload.Value);
                if (!string.IsNullOrWhiteSpace(importedStateJson))
                {
                    ReplaceStateFromJson(importedStateJson);
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

        await Interop.InitAsync(CreateStatePayload());
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

    private object CreateStatePayload()
    {
        IReadOnlyDictionary<string, object> snapshot = CreateFeatureStateSnapshot();
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
            return JsonSerializer.Deserialize<ReservoirDevToolsMessage>(
                messageJson,
                Options.SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void ReplaceStateFromJson(
        string stateJson
    )
    {
        JsonDocument? document = null;
        try
        {
            document = JsonDocument.Parse(stateJson);
            ReplaceStateFromJsonDocument(document);
        }
        catch (JsonException)
        {
            // Ignore invalid payloads.
        }
        finally
        {
            document?.Dispose();
        }
    }

    private void ReplaceStateFromJsonDocument(
        JsonDocument document
    )
    {
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        IReadOnlyDictionary<string, object> currentSnapshot = CreateFeatureStateSnapshot();
        Dictionary<string, object> newStates = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, object> current in currentSnapshot)
        {
            if (!root.TryGetProperty(current.Key, out JsonElement element))
            {
                if (Options.IsStrictStateRehydrationEnabled)
                {
                    // Strict mode: reject if any feature is missing.
                    return;
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
                    return;
                }

                continue;
            }

            newStates[current.Key] = deserialized;
        }

        ReplaceStateFromSnapshot(newStates);
    }

    private void ReplaceStateFromSnapshot(
        IReadOnlyDictionary<string, object> snapshot
    )
    {
        suppressDevToolsUpdates = true;
        try
        {
            ReplaceFeatureStates(snapshot, notifyListeners: true);
        }
        finally
        {
            suppressDevToolsUpdates = false;
        }
    }

    private async Task SendToDevToolsAsync(
        IAction action
    )
    {
        if (!await EnsureConnectedAsync())
        {
            return;
        }

        try
        {
            object actionPayload = CreateActionPayload(action);
            object statePayload = CreateStatePayload();
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

    private sealed record ReservoirDevToolsMessage(
        string? Type,
        JsonElement? Payload,
        string? State
    );
}
