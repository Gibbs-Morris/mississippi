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

internal sealed class ReservoirDevToolsStore : Store
{
    private readonly ReservoirDevToolsInterop interop;

    private readonly ReservoirDevToolsOptions options;

    private readonly IHostEnvironment? hostEnvironment;

    private readonly SemaphoreSlim devToolsLock = new(1, 1);

    private DotNetObjectReference<ReservoirDevToolsStore>? dotNetRef;

    private bool devToolsConnected;

    private bool suppressDevToolsUpdates;

    private IReadOnlyDictionary<string, object> committedSnapshot;

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

        this.interop = interop;
        this.options = options.Value;
        this.hostEnvironment = hostEnvironment;
        committedSnapshot = GetFeatureStateSnapshot();
    }

    protected override void OnActionDispatched(
        IAction action
    )
    {
        if (suppressDevToolsUpdates)
        {
            return;
        }

        _ = SendToDevToolsAsync(action);
    }

    protected override void Dispose(
        bool disposing
    )
    {
        if (disposing)
        {
            dotNetRef?.Dispose();
            dotNetRef = null;
            _ = interop.DisconnectAsync();
        }

        base.Dispose(disposing);
    }

    [JSInvokable]
    public async Task OnDevToolsMessage(
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
        return options.Enablement switch
        {
            ReservoirDevToolsEnablement.Always => true,
            ReservoirDevToolsEnablement.DevelopmentOnly => hostEnvironment?.IsDevelopment() == true,
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
            bool connected = await interop.ConnectAsync(BuildOptionsPayload(), dotNetRef);
            devToolsConnected = connected;
            if (!connected)
            {
                return false;
            }

            await interop.InitAsync(CreateStatePayload());
            committedSnapshot = GetFeatureStateSnapshot();
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
                ReplaceStateFromSnapshot(GetInitialFeatureStateSnapshot());
                await InitDevToolsAsync();
                break;
            case "ROLLBACK":
                ReplaceStateFromSnapshot(committedSnapshot);
                await InitDevToolsAsync();
                break;
            case "COMMIT":
                committedSnapshot = GetFeatureStateSnapshot();
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

        await interop.InitAsync(CreateStatePayload());
    }

    private object BuildOptionsPayload()
    {
        Dictionary<string, object?> payload = new(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(options.Name))
        {
            payload["name"] = options.Name;
        }

        if (options.MaxAge.HasValue)
        {
            payload["maxAge"] = options.MaxAge.Value;
        }

        if (options.Latency.HasValue)
        {
            payload["latency"] = options.Latency.Value;
        }

        if (options.AutoPause.HasValue)
        {
            payload["autoPause"] = options.AutoPause.Value;
        }

        foreach (KeyValuePair<string, object?> option in options.AdditionalOptions)
        {
            payload[option.Key] = option.Value;
        }

        return payload;
    }

    private object CreateActionPayload(
        IAction action
    )
    {
        object? sanitized = options.ActionSanitizer?.Invoke(action);
        if (sanitized is not null)
        {
            return sanitized;
        }

        return new
        {
            type = action.GetType().Name,
            payload = action,
        };
    }

    private object CreateStatePayload()
    {
        IReadOnlyDictionary<string, object> snapshot = GetFeatureStateSnapshot();
        object? sanitized = options.StateSanitizer?.Invoke(snapshot);
        return sanitized ?? snapshot;
    }

    private ReservoirDevToolsMessage? DeserializeMessage(
        string messageJson
    )
    {
        try
        {
            return JsonSerializer.Deserialize<ReservoirDevToolsMessage>(
                messageJson,
                options.SerializerOptions);
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

        IReadOnlyDictionary<string, object> currentSnapshot = GetFeatureStateSnapshot();
        Dictionary<string, object> newStates = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, object> current in currentSnapshot)
        {
            if (!root.TryGetProperty(current.Key, out JsonElement element))
            {
                continue;
            }

            Type stateType = current.Value.GetType();
            object? deserialized = element.Deserialize(stateType, options.SerializerOptions);
            if (deserialized is null)
            {
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
            await interop.SendAsync(actionPayload, statePayload);
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

    private string? TryExtractImportedStateJson(
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

        JsonElement lastState = computedStates.EnumerateArray().LastOrDefault();
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
