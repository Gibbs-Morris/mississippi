using System;
using System.Threading.Tasks;

using Microsoft.JSInterop;


namespace Mississippi.Reservoir.Blazor;

/// <summary>
///     Provides JavaScript interop for Redux DevTools communication.
/// </summary>
internal sealed class ReservoirDevToolsInterop : IAsyncDisposable
{
    private IJSRuntime JsRuntime { get; }

    private IJSObjectReference? module;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirDevToolsInterop"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime.</param>
    public ReservoirDevToolsInterop(
        IJSRuntime jsRuntime
    )
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        JsRuntime = jsRuntime;
    }

    /// <summary>
    ///     Connects to the Redux DevTools extension.
    /// </summary>
    /// <param name="options">The DevTools options payload.</param>
    /// <param name="dotNetRef">The .NET object reference for callbacks.</param>
    /// <returns>True if connection succeeded.</returns>
    public async ValueTask<bool> ConnectAsync(
        object options,
        DotNetObjectReference<ReservoirDevToolsStore> dotNetRef
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(dotNetRef);
        IJSObjectReference jsModule = await GetModuleAsync();
        return await jsModule.InvokeAsync<bool>("connect", options, dotNetRef);
    }

    /// <summary>
    ///     Disconnects from the Redux DevTools extension.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask DisconnectAsync()
    {
        if (module is null)
        {
            return;
        }

        await module.InvokeVoidAsync("disconnect");
    }

    /// <summary>
    ///     Initializes the DevTools state.
    /// </summary>
    /// <param name="state">The initial state.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask InitAsync(
        object state
    )
    {
        IJSObjectReference jsModule = await GetModuleAsync();
        await jsModule.InvokeVoidAsync("init", state);
    }

    /// <summary>
    ///     Sends an action and state update to DevTools.
    /// </summary>
    /// <param name="action">The action payload.</param>
    /// <param name="state">The updated state.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask SendAsync(
        object action,
        object state
    )
    {
        IJSObjectReference jsModule = await GetModuleAsync();
        await jsModule.InvokeVoidAsync("send", action, state);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            await module.DisposeAsync();
            module = null;
        }
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        if (module is not null)
        {
            return module;
        }

        string assemblyName = typeof(ReservoirDevToolsInterop).Assembly.GetName().Name ?? "Reservoir.Blazor";
        string modulePath = $"./_content/{assemblyName}/mississippi.reservoir.devtools.js";
        module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", modulePath);
        return module;
    }
}
