# Tutorial: Building Your First Ripples Feature

**Status**: 🔵 Design Document  
**Audience**: Developers using Mississippi + Ripples

---

## Introduction

This tutorial walks you through building a complete real-time feature using the Mississippi framework with Ripples. By the end, you'll have:

- A working channel management UI with real-time updates
- List view with channel summaries
- Detail view with channel information
- Create/rename commands that update the UI automatically

**Time to complete**: ~30 minutes

---

## Prerequisites

- .NET 9 SDK
- VS Code or Visual Studio 2022
- Mississippi project template installed

```powershell
dotnet new install Mississippi.Templates
```

---

## Step 1: Create the Project

```powershell
# Create new solution
dotnet new mississippi-app -n ChatApp
cd ChatApp

# Verify structure
ls -la
# Should show:
# - src/ChatApp.Domain/
# - src/ChatApp.Server/
# - src/ChatApp.Client/
# - src/ChatApp.Shared/
```

The template gives you:

| Project | Purpose |
|---------|---------|
| `ChatApp.Domain` | Aggregates, events, projections, reducers |
| `ChatApp.Server` | Orleans silo + ASP.NET host |
| `ChatApp.Client` | Blazor WASM client |
| `ChatApp.Shared` | Shared DTOs, interfaces |

---

## Step 2: Define the Domain Events

Events are facts that have happened. Start here to design your domain.

```csharp
// src/ChatApp.Domain/Events/ChannelEvents.cs

namespace ChatApp.Domain.Events;

/// <summary>A new channel was created.</summary>
[GenerateSerializer]
public sealed record ChannelCreated(
    [property: Id(0)] string ChannelId,
    [property: Id(1)] string Name,
    [property: Id(2)] string CreatedBy,
    [property: Id(3)] DateTimeOffset CreatedAt);

/// <summary>A channel was renamed.</summary>
[GenerateSerializer]
public sealed record ChannelRenamed(
    [property: Id(0)] string ChannelId,
    [property: Id(1)] string OldName,
    [property: Id(2)] string NewName,
    [property: Id(3)] DateTimeOffset RenamedAt);

/// <summary>A channel was archived.</summary>
[GenerateSerializer]
public sealed record ChannelArchived(
    [property: Id(0)] string ChannelId,
    [property: Id(1)] DateTimeOffset ArchivedAt);
```

---

## Step 3: Define the Aggregate

The aggregate enforces business rules and emits events.

```csharp
// src/ChatApp.Domain/Aggregates/ChannelAggregate.cs

namespace ChatApp.Domain.Aggregates;

/// <summary>
/// Channel aggregate state.
/// </summary>
[GenerateSerializer]
public sealed record ChannelState
{
    [Id(0)] public string Id { get; init; } = "";
    [Id(1)] public string Name { get; init; } = "";
    [Id(2)] public string CreatedBy { get; init; } = "";
    [Id(3)] public bool IsArchived { get; init; }
    [Id(4)] public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Channel aggregate grain interface.
/// </summary>
[UxAggregate(Route = "channels")]
public interface IChannelGrain : IAggregateGrain
{
    [CommandRoute("create")]
    Task<OperationResult> CreateAsync(CreateChannelCommand command);
    
    [CommandRoute("rename")]
    Task<OperationResult> RenameAsync(RenameChannelCommand command);
    
    [CommandRoute("archive")]
    Task<OperationResult> ArchiveAsync();
}

/// <summary>
/// Channel aggregate implementation.
/// </summary>
public sealed class ChannelGrain : AggregateGrain<ChannelState>, IChannelGrain
{
    public Task<OperationResult> CreateAsync(CreateChannelCommand command)
    {
        if (State.Id != default)
            return Task.FromResult(OperationResult.Failure("Channel already exists"));
        
        if (string.IsNullOrWhiteSpace(command.Name))
            return Task.FromResult(OperationResult.Failure("Name is required"));
        
        var @event = new ChannelCreated(
            ChannelId: this.GetPrimaryKeyString(),
            Name: command.Name,
            CreatedBy: command.CreatedBy,
            CreatedAt: DateTimeOffset.UtcNow);
        
        Apply(@event);
        return Task.FromResult(OperationResult.Success());
    }
    
    public Task<OperationResult> RenameAsync(RenameChannelCommand command)
    {
        if (State.Id == default)
            return Task.FromResult(OperationResult.Failure("Channel not found"));
        
        if (State.IsArchived)
            return Task.FromResult(OperationResult.Failure("Cannot rename archived channel"));
        
        if (State.Name == command.NewName)
            return Task.FromResult(OperationResult.Success()); // No-op
        
        Apply(new ChannelRenamed(
            ChannelId: State.Id,
            OldName: State.Name,
            NewName: command.NewName,
            RenamedAt: DateTimeOffset.UtcNow));
        
        return Task.FromResult(OperationResult.Success());
    }
    
    public Task<OperationResult> ArchiveAsync()
    {
        if (State.Id == default)
            return Task.FromResult(OperationResult.Failure("Channel not found"));
        
        if (State.IsArchived)
            return Task.FromResult(OperationResult.Success()); // Already archived
        
        Apply(new ChannelArchived(
            ChannelId: State.Id,
            ArchivedAt: DateTimeOffset.UtcNow));
        
        return Task.FromResult(OperationResult.Success());
    }
    
    protected override ChannelState Apply(ChannelState state, object @event) => @event switch
    {
        ChannelCreated e => state with 
        { 
            Id = e.ChannelId, 
            Name = e.Name, 
            CreatedBy = e.CreatedBy,
            CreatedAt = e.CreatedAt 
        },
        ChannelRenamed e => state with { Name = e.NewName },
        ChannelArchived => state with { IsArchived = true },
        _ => state
    };
}
```

Commands are simple records:

```csharp
// src/ChatApp.Domain/Commands/ChannelCommands.cs

namespace ChatApp.Domain.Commands;

[GenerateSerializer]
public sealed record CreateChannelCommand(
    [property: Id(0)] string Name,
    [property: Id(1)] string CreatedBy);

[GenerateSerializer]
public sealed record RenameChannelCommand(
    [property: Id(0)] string NewName);
```

---

## Step 4: Define the Projections

Projections are read models optimized for the UI.

```csharp
// src/ChatApp.Domain/Projections/ChannelProjection.cs

namespace ChatApp.Domain.Projections;

/// <summary>
/// Single channel detail projection.
/// </summary>
[UxProjection(Route = "channels")]
[GenerateSerializer]
public sealed record ChannelProjection
{
    [Id(0)] public required string Id { get; init; }
    [Id(1)] public required string Name { get; init; }
    [Id(2)] public required string CreatedBy { get; init; }
    [Id(3)] public required DateTimeOffset CreatedAt { get; init; }
    [Id(4)] public required bool IsArchived { get; init; }
}

/// <summary>
/// List of channels for a user.
/// </summary>
[UxProjection(Route = "channel-list")]
[GenerateSerializer]
public sealed record ChannelListProjection
{
    [Id(0)] public required ImmutableList<string> ChannelIds { get; init; }
}
```

---

## Step 5: Write the Reducers

Reducers transform events into projections.

```csharp
// src/ChatApp.Domain/Projections/ChannelProjectionReducer.cs

namespace ChatApp.Domain.Projections;

/// <summary>
/// Reduces events into <see cref="ChannelProjection"/>.
/// </summary>
public sealed class ChannelProjectionReducer : IReducer<ChannelProjection>
{
    public ChannelProjection? Reduce(ChannelProjection? state, object @event) => @event switch
    {
        ChannelCreated e => new ChannelProjection
        {
            Id = e.ChannelId,
            Name = e.Name,
            CreatedBy = e.CreatedBy,
            CreatedAt = e.CreatedAt,
            IsArchived = false,
        },
        
        ChannelRenamed e => state! with { Name = e.NewName },
        
        ChannelArchived => state! with { IsArchived = true },
        
        _ => state
    };
}
```

```csharp
// src/ChatApp.Domain/Projections/ChannelListProjectionReducer.cs

namespace ChatApp.Domain.Projections;

/// <summary>
/// Reduces events into <see cref="ChannelListProjection"/>.
/// </summary>
public sealed class ChannelListProjectionReducer : IReducer<ChannelListProjection>
{
    public ChannelListProjection? Reduce(ChannelListProjection? state, object @event) => @event switch
    {
        ChannelCreated e => (state ?? new ChannelListProjection 
        { 
            ChannelIds = ImmutableList<string>.Empty 
        }) with 
        { 
            ChannelIds = state?.ChannelIds.Add(e.ChannelId) 
                ?? ImmutableList.Create(e.ChannelId) 
        },
        
        ChannelArchived e => state! with 
        { 
            ChannelIds = state.ChannelIds.Remove(e.ChannelId) 
        },
        
        _ => state
    };
}
```

---

## Step 6: Build the UI Components

### Atoms: Basic Building Blocks

```razor
@* src/ChatApp.Client/Components/Atoms/Skeleton.razor *@

<div class="skeleton @Class" style="width: @Width; height: @Height;"></div>

@code {
    [Parameter] public string Width { get; set; } = "100%";
    [Parameter] public string Height { get; set; } = "1em";
    [Parameter] public string Class { get; set; } = "";
}
```

```razor
@* src/ChatApp.Client/Components/Atoms/Button.razor *@

<button class="btn btn-@Variant @Class" 
        disabled="@Disabled" 
        @onclick="OnClick">
    @ChildContent
</button>

@code {
    [Parameter] public string Variant { get; set; } = "primary";
    [Parameter] public string Class { get; set; } = "";
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

### Molecules: Channel Row

```razor
@* src/ChatApp.Client/Components/Molecules/ChannelRow.razor *@
@inherits RippleComponent

<div class="channel-row @(IsSelected ? "selected" : "")" @onclick="HandleClick">
    <RippleView For="Channel">
        <Loading>
            <Skeleton Width="200px" Height="1.5em" />
        </Loading>
        
        <Error Context="ex">
            <span class="error">⚠️ @ex.Message</span>
        </Error>
        
        <Content>
            <span class="channel-icon">#</span>
            <span class="channel-name">@Channel.Current!.Name</span>
            @if (Channel.Current.IsArchived)
            {
                <span class="badge archived">Archived</span>
            }
        </Content>
    </RippleView>
</div>

@code {
    [Inject] private IRipplePool<ChannelProjection> Pool { get; set; } = null!;
    
    [Parameter] public string ChannelId { get; set; } = "";
    [Parameter] public bool IsSelected { get; set; }
    [Parameter] public EventCallback<string> OnSelect { get; set; }
    
    private IRipple<ChannelProjection> Channel { get; set; } = null!;
    
    protected override async Task OnParametersSetAsync()
    {
        Channel = Pool.Get(ChannelId);
        UseRipple(Channel);
        Pool.MarkVisible(ChannelId);
        await Channel.SubscribeAsync(ChannelId);
    }
    
    private async Task HandleClick()
    {
        await OnSelect.InvokeAsync(ChannelId);
    }
    
    public override void Dispose()
    {
        Pool.MarkHidden(ChannelId);
        base.Dispose();
    }
}
```

### Organisms: Channel List

```razor
@* src/ChatApp.Client/Components/Organisms/ChannelList.razor *@
@inherits RippleComponent

<div class="channel-list">
    <div class="channel-list-header">
        <h2>Channels</h2>
        <Button Variant="ghost" OnClick="ShowCreateDialog">+ New</Button>
    </div>
    
    <RippleView For="List">
        <Loading>
            @for (int i = 0; i < 5; i++)
            {
                <div class="channel-row-skeleton">
                    <Skeleton Width="80%" Height="1.5em" />
                </div>
            }
        </Loading>
        
        <Empty>
            <div class="empty-state">
                <p>No channels yet</p>
                <Button OnClick="ShowCreateDialog">Create your first channel</Button>
            </div>
        </Empty>
        
        <Error Context="ex">
            <div class="error-state">
                <p>Failed to load channels</p>
                <Button Variant="secondary" OnClick="Retry">Retry</Button>
            </div>
        </Error>
        
        <Content>
            @foreach (var channelId in List.Current!.ChannelIds)
            {
                <ChannelRow 
                    ChannelId="@channelId" 
                    IsSelected="@(channelId == SelectedChannelId)"
                    OnSelect="SelectChannel" />
            }
        </Content>
    </RippleView>
</div>

@if (showCreateDialog)
{
    <CreateChannelDialog OnClose="HideCreateDialog" OnCreated="HandleChannelCreated" />
}

@code {
    [Inject] private IRipple<ChannelListProjection> List { get; set; } = null!;
    [Inject] private IRipplePool<ChannelProjection> Pool { get; set; } = null!;
    
    [Parameter] public string UserId { get; set; } = "";
    [Parameter] public string? SelectedChannelId { get; set; }
    [Parameter] public EventCallback<string> OnChannelSelected { get; set; }
    
    private bool showCreateDialog;
    
    protected override async Task OnInitializedAsync()
    {
        UseRipple(List);
        await List.SubscribeAsync(UserId);
        
        // Prefetch visible channels
        if (List.Current?.ChannelIds is { } ids)
        {
            await Pool.PrefetchAsync(ids.Take(10));
        }
    }
    
    private async Task SelectChannel(string channelId)
    {
        await OnChannelSelected.InvokeAsync(channelId);
    }
    
    private void ShowCreateDialog() => showCreateDialog = true;
    private void HideCreateDialog() => showCreateDialog = false;
    
    private async Task HandleChannelCreated(string channelId)
    {
        HideCreateDialog();
        await SelectChannel(channelId);
    }
    
    private async Task Retry()
    {
        await List.RefreshAsync();
    }
}
```

### Organisms: Create Channel Dialog

```razor
@* src/ChatApp.Client/Components/Organisms/CreateChannelDialog.razor *@
@inherits RippleComponent

<div class="dialog-overlay" @onclick="Close">
    <div class="dialog" @onclick:stopPropagation="true">
        <h2>Create Channel</h2>
        
        <form @onsubmit="HandleSubmit">
            <div class="form-group">
                <label for="name">Channel Name</label>
                <input id="name" 
                       type="text" 
                       @bind="name" 
                       placeholder="e.g., general" 
                       required />
            </div>
            
            @if (error is not null)
            {
                <div class="error-message">@error</div>
            }
            
            <div class="dialog-actions">
                <Button Variant="secondary" OnClick="Close" Disabled="@isSubmitting">
                    Cancel
                </Button>
                <Button Variant="primary" Disabled="@isSubmitting">
                    @(isSubmitting ? "Creating..." : "Create")
                </Button>
            </div>
        </form>
    </div>
</div>

@code {
    [Inject] private IChannelCommandDispatcher Commands { get; set; } = null!;
    [Inject] private ICurrentUser CurrentUser { get; set; } = null!;
    
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<string> OnCreated { get; set; }
    
    private string name = "";
    private string? error;
    private bool isSubmitting;
    
    private async Task HandleSubmit()
    {
        if (isSubmitting) return;
        
        isSubmitting = true;
        error = null;
        
        try
        {
            var channelId = Guid.NewGuid().ToString();
            var result = await Commands.CreateAsync(channelId, new CreateChannelCommand(
                Name: name,
                CreatedBy: CurrentUser.Id));
            
            if (result.IsSuccess)
            {
                await OnCreated.InvokeAsync(channelId);
            }
            else
            {
                error = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            error = "An unexpected error occurred";
        }
        finally
        {
            isSubmitting = false;
        }
    }
    
    private async Task Close()
    {
        await OnClose.InvokeAsync();
    }
}
```

### Pages: Channels Page

```razor
@* src/ChatApp.Client/Pages/ChannelsPage.razor *@
@page "/channels"
@page "/channels/{ChannelId}"
@inherits RippleComponent

<PageTitle>@(SelectedChannel.Current?.Name ?? "Channels") - ChatApp</PageTitle>

<div class="channels-page">
    <aside class="sidebar">
        <ChannelList 
            UserId="@CurrentUser.Id" 
            SelectedChannelId="@ChannelId"
            OnChannelSelected="NavigateToChannel" />
    </aside>
    
    <main class="channel-content">
        @if (ChannelId is null)
        {
            <div class="no-selection">
                <h2>Welcome to ChatApp!</h2>
                <p>Select a channel to start chatting</p>
            </div>
        }
        else
        {
            <RippleView For="SelectedChannel">
                <Loading>
                    <ChannelHeaderSkeleton />
                </Loading>
                
                <Content>
                    <ChannelHeader Channel="SelectedChannel.Current!" />
                    <MessageList ChannelId="@ChannelId" />
                    <MessageComposer ChannelId="@ChannelId" />
                </Content>
            </RippleView>
        }
    </main>
</div>

@code {
    [Inject] private IRipple<ChannelProjection> SelectedChannel { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private ICurrentUser CurrentUser { get; set; } = null!;
    
    [Parameter] public string? ChannelId { get; set; }
    
    protected override async Task OnParametersSetAsync()
    {
        if (ChannelId is not null)
        {
            UseRipple(SelectedChannel);
            await SelectedChannel.SubscribeAsync(ChannelId);
        }
    }
    
    private void NavigateToChannel(string channelId)
    {
        Nav.NavigateTo($"/channels/{channelId}");
    }
}
```

---

## Step 7: Wire It Up

### Server Program.cs

```csharp
// src/ChatApp.Server/Program.cs

var builder = WebApplication.CreateBuilder(args);

// Add Orleans
builder.Host.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.AddMississippi();  // Adds event sourcing support
});

// Add Ripples server support
builder.Services
    .AddMississippiProjections()
    .AddGeneratedRipples(RippleHostingMode.Server)
    .AddGeneratedCommandDispatchers();

// Add controllers (generated + manual)
builder.Services.AddControllers();

// Add SignalR for real-time updates
builder.Services.AddSignalR();

var app = builder.Build();

app.UseRouting();
app.MapControllers();
app.MapHub<RipplesHub>("/hubs/ripples");
app.MapFallbackToFile("index.html");

app.Run();
```

### Client Program.cs

```csharp
// src/ChatApp.Client/Program.cs

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// Add Ripples client support
builder.Services
    .AddGeneratedRipples(RippleHostingMode.Client)
    .AddGeneratedCommandDispatchers()
    .AddRipplesSignalR(options =>
    {
        options.HubUrl = builder.HostEnvironment.BaseAddress + "hubs/ripples";
    });

// Add HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

await builder.Build().RunAsync();
```

---

## Step 8: Run and Test

```powershell
# Start the server
cd src/ChatApp.Server
dotnet run

# Open browser to https://localhost:5001
# - Create a channel
# - See it appear in real-time
# - Open another browser tab
# - Both tabs update together!
```

---

## What Just Happened?

You built a complete real-time feature with:

| Layer | What You Wrote | What Was Generated |
|-------|----------------|-------------------|
| Events | 3 simple records | Event type registry |
| Aggregate | Business logic only | Command controller |
| Projections | 2 projection records | Projection controllers |
| Reducers | Pure mapping functions | Route registry |
| Components | UI only | DI registrations |
| Wiring | 10 lines in Program.cs | Command dispatchers |

**Total handwritten code**: ~400 lines  
**Generated code**: ~800 lines  
**Time saved**: ~2 hours

---

## Next Steps

- Add message functionality (same pattern!)
- Add user presence (another projection)
- Add typing indicators (ephemeral state)
- Deploy to Azure (Orleans + Azure SignalR)

See the [Advanced Patterns Guide](./12-advanced-patterns.md) for more complex scenarios.
