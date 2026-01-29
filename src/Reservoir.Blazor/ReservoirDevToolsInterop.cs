using System;
using System.Threading.Tasks;

using Microsoft.JSInterop;


namespace Mississippi.Reservoir.Blazor;

internal sealed class ReservoirDevToolsInterop : IAsyncDisposable
{
    private readonly IJSRuntime jsRuntime;

    private IJSObjectReference? module;

    public ReservoirDevToolsInterop(
        IJSRuntime jsRuntime
    )
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        this.jsRuntime = jsRuntime;
    }

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

    public async ValueTask DisconnectAsync()
    {
        if (module is null)
        {
            return;
        }

        await module.InvokeVoidAsync("disconnect");
    }

    public async ValueTask InitAsync(
        object state
    )
    {
        IJSObjectReference jsModule = await GetModuleAsync();
        await jsModule.InvokeVoidAsync("init", state);
    }

    public async ValueTask SendAsync(
        object action,
        object state
    )
    {
        IJSObjectReference jsModule = await GetModuleAsync();
        await jsModule.InvokeVoidAsync("send", action, state);
    }

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
        module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", modulePath);
        return module;
    }
}
