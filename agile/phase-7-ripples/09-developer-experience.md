# Ripples Developer Experience Guide

**Status**: 🔵 Design Document  
**Goal**: Make building event-sourced UIs as easy as building CRUD apps

## Executive Summary

This document outlines the complete developer experience for building UIs with Mississippi + Ripples. Our goal is **zero boilerplate** where possible, using source generators and conventions to eliminate repetitive code.

## Developer Journey Overview

```mermaid
flowchart LR
    subgraph Domain["1. Domain Layer"]
        agg["Define Aggregate"]
        events["Define Events"]
        commands["Define Commands"]
    end
    
    subgraph Projection["2. Projection Layer"]
        proj["Define Projection"]
        reducer["Write Reducer"]
    end
    
    subgraph UI["3. UI Layer"]
        component["Write Component"]
        style["Add Styling"]
    end
    
    subgraph Generated["✨ Auto-Generated"]
        controller["Controllers"]
        routes["Route Registry"]
        ripples["Ripple Registrations"]
        skeletons["Component Skeletons"]
    end
    
    agg --> events --> commands
    commands --> proj --> reducer
    reducer --> component --> style
    
    agg -.->|"Source Gen"| controller
    proj -.->|"Source Gen"| routes
    proj -.->|"Source Gen"| ripples
    proj -.->|"Source Gen"| skeletons
```

## What Developers Write vs What's Generated

| Developer Writes | Source Gen Creates |
|------------------|-------------------|
| Aggregate state record | - |
| Domain events | Event type registry |
| Command records | Command DTOs (if needed) |
| Command handlers | - |
| Projection record | Projection controller |
| Reducer logic | Route registry entry |
| Blazor component | Ripple registration |
| - | Loading/error wrappers |
| - | Component skeletons |

## The "10-Minute UX" Goal

A developer should be able to go from "I need a new feature" to "working UI with real-time updates" in ~10 minutes:

1. **2 min**: Define the projection record
2. **3 min**: Write the reducer
3. **5 min**: Create the Blazor component

Everything else is generated.

---

## Step-by-Step: Building a Channel List Feature

### Step 1: Define the Projection (2 minutes)

```csharp
// Cascade.Domain/Projections/ChannelListProjection.cs

/// <summary>
/// List of channels visible to a user.
/// </summary>
[UxProjection]  // ← Triggers source generation
[GenerateSerializer]
public sealed record ChannelListProjection
{
    [Id(0)] public required ImmutableList<string> ChannelIds { get; init; }
    [Id(1)] public required int TotalCount { get; init; }
}

/// <summary>
/// Detail projection for a single channel.
/// </summary>
[UxProjection(Route = "channels")]  // ← Custom route
[GenerateSerializer]
public sealed record ChannelProjection
{
    [Id(0)] public required string Id { get; init; }
    [Id(1)] public required string Name { get; init; }
    [Id(2)] public required string Description { get; init; }
    [Id(3)] public required int MemberCount { get; init; }
    [Id(4)] public required DateTimeOffset CreatedAt { get; init; }
}
```

**What gets generated:**

```csharp
// ChannelListProjectionController.g.cs (auto-generated)
[Route("api/projections/channel-list/{entityId}")]
public sealed class ChannelListProjectionController 
    : UxProjectionControllerBase<ChannelListProjection> { }

// ChannelProjectionController.g.cs (auto-generated)
[Route("api/projections/channels/{entityId}")]
public sealed class ChannelProjectionController 
    : UxProjectionControllerBase<ChannelProjection>
{
    [HttpPost("batch")]
    public Task<ActionResult<Dictionary<string, ChannelProjection>>> GetBatchAsync(...) { }
}

// ProjectionRouteRegistry.g.cs (auto-generated, accumulated)
public static partial class ProjectionRouteRegistry
{
    // Entries added automatically
}

// RippleServiceRegistrations.g.cs (auto-generated)
public static partial class RippleServiceRegistrations
{
    public static IServiceCollection AddGeneratedRipples(this IServiceCollection services)
    {
        services.AddScoped<IRipple<ChannelListProjection>, ServerRipple<ChannelListProjection>>();
        services.AddScoped<IRipple<ChannelProjection>, ServerRipple<ChannelProjection>>();
        services.AddScoped<IRipplePool<ChannelProjection>, ServerRipplePool<ChannelProjection>>();
        return services;
    }
}
```

### Step 2: Write the Reducer (3 minutes)

```csharp
// Cascade.Domain/Projections/ChannelProjectionReducer.cs

public sealed class ChannelProjectionReducer : IReducer<ChannelProjection>
{
    public ChannelProjection? Reduce(ChannelProjection? state, object @event) => @event switch
    {
        ChannelCreated e => new ChannelProjection
        {
            Id = e.ChannelId,
            Name = e.Name,
            Description = e.Description,
            MemberCount = 0,
            CreatedAt = e.Timestamp,
        },
        
        ChannelRenamed e => state! with { Name = e.NewName },
        
        MemberJoined => state! with { MemberCount = state.MemberCount + 1 },
        
        MemberLeft => state! with { MemberCount = state.MemberCount - 1 },
        
        _ => state
    };
}
```

### Step 3: Create the Component (5 minutes)

```razor
@* Components/Organisms/ChannelList.razor *@
@inherits RippleComponent

<div class="channel-list">
    <RippleView For="ChannelList">
        <Loading>
            <Skeleton Lines="5" />
        </Loading>
        
        <Error Context="ex">
            <Alert Type="danger">Failed to load channels: @ex.Message</Alert>
        </Error>
        
        <Content>
            @foreach (var channelId in ChannelList.Current!.ChannelIds)
            {
                <ChannelRow ChannelId="@channelId" />
            }
        </Content>
    </RippleView>
</div>

@code {
    [Inject] private IRipple<ChannelListProjection> ChannelList { get; set; } = null!;
    
    [Parameter] public string UserId { get; set; } = "";
    
    protected override async Task OnInitializedAsync()
    {
        UseRipple(ChannelList);
        await ChannelList.SubscribeAsync(UserId);
    }
}
```

```razor
@* Components/Molecules/ChannelRow.razor *@
@inherits RippleComponent

<div class="channel-row">
    <RippleView For="Channel">
        <Loading><Skeleton Width="200px" /></Loading>
        <Content>
            <span class="channel-name">@Channel.Current!.Name</span>
            <span class="member-count">@Channel.Current.MemberCount members</span>
        </Content>
    </RippleView>
</div>

@code {
    [Inject] private IRipplePool<ChannelProjection> Pool { get; set; } = null!;
    
    [Parameter] public string ChannelId { get; set; } = "";
    
    private IRipple<ChannelProjection> Channel { get; set; } = null!;
    
    protected override async Task OnParametersSetAsync()
    {
        Channel = Pool.Get(ChannelId);
        UseRipple(Channel);
        Pool.MarkVisible(ChannelId);
        await Channel.SubscribeAsync(ChannelId);
    }
}
```

**That's it!** Real-time updates, loading states, error handling, and optimized subscriptions - all working.

---

## Source Generator Opportunities

### 1. `[UxProjection]` Attribute (Already Planned)

Generates:
- Controller with all endpoints
- Route registry entry
- Ripple service registration
- Batch endpoint for pools

### 2. `[UxAggregate]` Attribute (NEW)

```csharp
[UxAggregate(Route = "channels")]
public interface IChannelAggregateGrain : IAggregateGrain
{
    Task<OperationResult> CreateAsync(CreateChannelCommand cmd);
    Task<OperationResult> RenameAsync(RenameChannelCommand cmd);
    Task<OperationResult> JoinAsync(JoinChannelCommand cmd);
}
```

Generates:
- Command controller with all endpoints
- Command route registry entry
- Strongly-typed `ICommandDispatcher<TAggregate>` for client

### 3. `[RippleComponent]` Attribute (NEW)

```csharp
// Instead of writing the component manually, generate a skeleton:

[RippleComponent]
[UsesRipple<ChannelProjection>("Channel")]
[UsesRipplePool<ChannelProjection>("Pool")]
public partial class ChannelRow : RippleComponent
{
    [Parameter] public string ChannelId { get; set; } = "";
}
```

Generates:
- Injection properties
- `UseRipple()` calls in `OnInitializedAsync`
- `Pool.MarkVisible/MarkHidden` in lifecycle
- Dispose handling

### 4. `<RippleView>` Component (NEW)

A Blazor component that handles loading/error/content states:

```razor
<RippleView For="Channel">
    <Loading>...</Loading>   <!-- Shown while IsLoading -->
    <Error>...</Error>       <!-- Shown when LastError != null -->
    <Empty>...</Empty>       <!-- Shown when Current is null/empty -->
    <Content>...</Content>   <!-- Shown when data available -->
</RippleView>
```

### 5. Command Dispatch Helper (NEW)

```csharp
// Instead of manually constructing HTTP requests or grain calls:

[Inject] private ICommandDispatcher<IChannelAggregateGrain> Commands { get; set; }

private async Task CreateChannel()
{
    var result = await Commands.DispatchAsync(
        channelId, 
        grain => grain.CreateAsync(new CreateChannelCommand { Name = "General" }));
    
    if (!result.IsSuccess)
    {
        // Handle error
    }
}
```

### 6. Form Binding (NEW - Future)

```razor
<RippleForm TCommand="CreateChannelCommand" 
            OnSubmit="HandleSubmit"
            OnSuccess="HandleSuccess"
            OnError="HandleError">
    <InputText @bind-Value="context.Name" />
    <InputTextArea @bind-Value="context.Description" />
    <SubmitButton>Create Channel</SubmitButton>
</RippleForm>
```

---

## Atomic Design Integration

### Folder Structure

```
Components/
├── Atoms/                      # Basic building blocks
│   ├── Button.razor
│   ├── Input.razor
│   ├── Avatar.razor
│   ├── Badge.razor
│   ├── Skeleton.razor          # Loading placeholder
│   └── Alert.razor             # Error display
│
├── Molecules/                  # Combinations of atoms
│   ├── ChannelRow.razor        # Uses IRipple<ChannelProjection>
│   ├── UserBadge.razor         # Uses IRipple<UserProjection>
│   ├── MessageBubble.razor
│   └── SearchInput.razor
│
├── Organisms/                  # Complex components with state
│   ├── ChannelList.razor       # Uses IRipple<ChannelListProjection>
│   │                           # + IRipplePool<ChannelProjection>
│   ├── MessageThread.razor
│   ├── UserPresence.razor
│   └── NavigationSidebar.razor
│
├── Templates/                  # Page layouts
│   ├── MainLayout.razor
│   ├── AuthLayout.razor
│   └── SettingsLayout.razor
│
├── Pages/                      # Route endpoints
│   ├── Home.razor
│   ├── ChannelPage.razor
│   └── SettingsPage.razor
│
└── Shared/                     # Framework components
    ├── RippleView.razor        # Loading/error/content wrapper
    ├── RippleProvider.razor    # Cascading state
    └── ErrorBoundary.razor
```

### Component Design Guidelines

#### Atoms: Pure presentation, no Ripples

```razor
@* Atoms/Badge.razor *@
<span class="badge badge-@Type">@ChildContent</span>

@code {
    [Parameter] public string Type { get; set; } = "default";
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

#### Molecules: May use single Ripple

```razor
@* Molecules/ChannelRow.razor *@
@inherits RippleComponent

<div class="channel-row" @onclick="OnClick">
    <RippleView For="Channel">
        <Loading><Skeleton Width="150px" /></Loading>
        <Content>
            <Badge Type="@(Channel.Current!.MemberCount > 10 ? "popular" : "default")">
                @Channel.Current.MemberCount
            </Badge>
            <span>@Channel.Current.Name</span>
        </Content>
    </RippleView>
</div>
```

#### Organisms: Coordinate multiple Ripples

```razor
@* Organisms/ChannelList.razor *@
@inherits RippleComponent

<div class="channel-list">
    <RippleView For="List">
        <Loading><ChannelListSkeleton /></Loading>
        <Empty><EmptyState Message="No channels yet" /></Empty>
        <Content>
            <Virtualize Items="List.Current!.ChannelIds" Context="id">
                <ChannelRow ChannelId="@id" OnClick="() => SelectChannel(id)" />
            </Virtualize>
        </Content>
    </RippleView>
</div>

@code {
    [Inject] private IRipple<ChannelListProjection> List { get; set; } = null!;
    [Inject] private IRipplePool<ChannelProjection> Pool { get; set; } = null!;
    
    [Parameter] public string UserId { get; set; } = "";
    [Parameter] public EventCallback<string> OnChannelSelected { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        UseRipple(List);
        await List.SubscribeAsync(UserId);
        
        if (List.Current?.ChannelIds is { } ids)
        {
            await Pool.PrefetchAsync(ids.Take(20));
        }
    }
    
    private async Task SelectChannel(string channelId)
    {
        await OnChannelSelected.InvokeAsync(channelId);
    }
}
```

#### Pages: Compose organisms, handle routing

```razor
@* Pages/ChannelPage.razor *@
@page "/channels/{ChannelId}"
@inherits RippleComponent

<PageTitle>@(Channel.Current?.Name ?? "Loading...")</PageTitle>

<div class="channel-page">
    <aside>
        <ChannelList UserId="@CurrentUserId" OnChannelSelected="NavigateToChannel" />
    </aside>
    
    <main>
        <RippleView For="Channel">
            <Loading><ChannelHeaderSkeleton /></Loading>
            <Content>
                <ChannelHeader Channel="Channel.Current!" />
                <MessageThread ChannelId="@ChannelId" />
                <MessageComposer OnSend="SendMessage" />
            </Content>
        </RippleView>
    </main>
</div>

@code {
    [Inject] private IRipple<ChannelProjection> Channel { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    
    [Parameter] public string ChannelId { get; set; } = "";
    
    private string CurrentUserId => /* from auth */;
    
    protected override async Task OnParametersSetAsync()
    {
        UseRipple(Channel);
        await Channel.SubscribeAsync(ChannelId);
    }
    
    private void NavigateToChannel(string id) => Nav.NavigateTo($"/channels/{id}");
}
```

---

## Complete Source Generator List

| Generator | Input | Output | Benefit |
|-----------|-------|--------|---------|
| `ProjectionControllerGenerator` | `[UxProjection]` on record | Controller class | No manual controllers |
| `ProjectionBatchEndpointGenerator` | `[UxProjection]` | Batch POST endpoint | Pool support |
| `ProjectionRouteRegistryGenerator` | All `[UxProjection]` | Static route lookup | WASM URL building |
| `RippleRegistrationGenerator` | All `[UxProjection]` | DI registrations | No manual wiring |
| `AggregateControllerGenerator` | `[UxAggregate]` on interface | Command controller | No manual controllers |
| `CommandRouteRegistryGenerator` | All `[UxAggregate]` | Static command routes | WASM command dispatch |
| `CommandDispatcherGenerator` | `[UxAggregate]` | `ICommandDispatcher<T>` | Type-safe commands |
| `RippleComponentGenerator` | `[RippleComponent]` | Lifecycle code | Less boilerplate |

---

## Developer Effort Comparison

### Without Ripples (Current)

For a new feature with list + detail:

| Task | Lines of Code | Time |
|------|---------------|------|
| Projection record | 20 | 2 min |
| Reducer | 30 | 5 min |
| Projection grain | 40 | 5 min |
| Controller | 50 | 5 min |
| Service registration | 10 | 2 min |
| Subscription service usage | 30 | 5 min |
| List component | 80 | 10 min |
| Row component | 60 | 8 min |
| Loading states | 40 | 5 min |
| Error handling | 30 | 5 min |
| **Total** | **~390 lines** | **~52 min** |

### With Ripples

| Task | Lines of Code | Time |
|------|---------------|------|
| Projection record + `[UxProjection]` | 20 | 2 min |
| Reducer | 30 | 3 min |
| List component with `<RippleView>` | 30 | 3 min |
| Row component with `<RippleView>` | 25 | 2 min |
| **Total** | **~105 lines** | **~10 min** |

**73% less code, 80% less time.**

---

## Quick Reference Card

```
┌─────────────────────────────────────────────────────────────────┐
│                   RIPPLES QUICK REFERENCE                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  DEFINE PROJECTION                                               │
│  ─────────────────                                               │
│  [UxProjection]                                                  │
│  public record MyProjection { ... }                              │
│                                                                  │
│  USE IN COMPONENT                                                │
│  ────────────────                                                │
│  @inherits RippleComponent                                       │
│  [Inject] IRipple<MyProjection> Data { get; set; }              │
│                                                                  │
│  protected override async Task OnInitializedAsync()              │
│  {                                                               │
│      UseRipple(Data);                                            │
│      await Data.SubscribeAsync(entityId);                        │
│  }                                                               │
│                                                                  │
│  DISPLAY WITH STATES                                             │
│  ───────────────────                                             │
│  <RippleView For="Data">                                         │
│      <Loading>...</Loading>                                      │
│      <Error Context="ex">...</Error>                             │
│      <Content>@Data.Current.Name</Content>                       │
│  </RippleView>                                                   │
│                                                                  │
│  LIST + DETAIL PATTERN                                           │
│  ─────────────────────                                           │
│  [Inject] IRipple<ListProjection> List { get; set; }            │
│  [Inject] IRipplePool<ItemProjection> Pool { get; set; }        │
│                                                                  │
│  var item = Pool.Get(itemId);                                    │
│  Pool.MarkVisible(itemId);                                       │
│  await item.SubscribeAsync(itemId);                              │
│                                                                  │
│  SEND COMMANDS                                                   │
│  ─────────────                                                   │
│  [Inject] ICommandDispatcher<IMyAggregateGrain> Cmd { get; set; }│
│                                                                  │
│  await Cmd.DispatchAsync(entityId, g => g.DoSomethingAsync(...));│
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```
