# Mississippi.AspNetCore.Orleans

This project provides production-ready ASP.NET Core integration adapters backed by Orleans for distributed scale-out scenarios.

## Adapters

### 1. OrleansDistributedCache (`IDistributedCache`)

Orleans-backed distributed cache implementation using per-key grains for scalable caching.

**Grain Mapping:**
- `CacheEntryGrain : IGrainWithStringKey` - One grain per cache key
- Key format: `{KeyPrefix}:{cacheKey}` (prefix configurable via options)

**Features:**
- Absolute and sliding expiration support
- Configurable default expiration policies
- Deterministic expiration via TimeProvider injection

**Usage:**
```csharp
services.AddOrleansDistributedCache(options =>
{
    options.KeyPrefix = "cache:";
    options.DefaultAbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
    options.DefaultSlidingExpiration = TimeSpan.FromMinutes(20);
});
```

### 2. OrleansOutputCacheStore (`IOutputCacheStore`)

Orleans-backed output cache storage with support for tag-based eviction.

**Grain Mapping:**
- `OutputCacheEntryGrain : IGrainWithStringKey` - One grain per output cache entry

**Features:**
- ASP.NET Core 9.0 `IOutputCacheStore` implementation
- Tag association for cache entries
- Expiration support

**Usage:**
```csharp
services.AddOrleansOutputCaching(options =>
{
    options.KeyPrefix = "output:";
    options.DefaultDuration = TimeSpan.FromSeconds(60);
});
```

### 3. OrleansTicketStore (`ITicketStore`)

Authentication ticket storage using Orleans for distributed cookie authentication.

**Grain Mapping:**
- `AuthTicketGrain : IGrainWithStringKey` - One grain per authentication ticket

**Features:**
- Secure ticket serialization
- Ticket renewal support
- Configurable expiration

**Usage:**
```csharp
services.AddOrleansTicketStore(options =>
{
    options.KeyPrefix = "ticket:";
    options.DefaultExpiration = TimeSpan.FromDays(14);
});
```

### 4. OrleansHubLifetimeManager<THub> (`HubLifetimeManager<THub>`)

SignalR hub lifetime manager for multi-server scale-out via Orleans.

**Grain Mapping:**
- `ConnectionGrain : IGrainWithStringKey` - One grain per SignalR connection
- Tracks connection metadata (userId, groups)

**Features:**
- Connection lifecycle management
- Group membership tracking
- User and connection-targeted messaging
- Broadcast support

**Usage:**
```csharp
services.AddOrleansSignalRScaleOut<ChatHub>(options =>
{
    options.StreamProviderName = "SignalRStream";
    options.EnableBackplane = true;
});
```

## Architecture

All adapters follow the repository's patterns:
- POCO grain pattern with `IGrainBase`
- Dependency injection via constructor with `private Type Name { get; }` properties
- High-performance logging using `LoggerMessage.Define`
- Options pattern for configuration
- Hierarchical service registration

## Requirements

- .NET 9.0
- Orleans 9.2.1+
- ASP.NET Core 9.0

## Storage Configuration

Each adapter uses Orleans persistent state with configurable storage providers:
- `CacheStorage` - for distributed cache entries
- `OutputCacheStorage` - for output cache entries
- `TicketStorage` - for authentication tickets
- `SignalRStorage` - for SignalR connection metadata

Configure storage providers in your Orleans silo configuration.
