# Design Review: DevOps Engineer Perspective

**Reviewer Role:** DevOps Engineer (focused on scalability, Kubernetes/pod behavior, locks, distributed systems)  
**Date:** 2026-01-24  
**RFC:** Server-Side Event Effects

---

## Overall Assessment: ⚠️ Conditional Approval

The design leverages Orleans' distributed primitives well, but there are operational concerns around observability, failure modes, and pod lifecycle that need addressing.

---

## Scalability Analysis

### StatelessWorker Grain Scaling ✅

**Design Choice:** `EffectDispatcherGrain` is marked `[StatelessWorker]`

**How It Works:**
- Orleans creates multiple activations per silo as load increases
- No affinity to specific silo—work distributes across cluster
- Auto-scales down when load decreases

**Pod/Kubernetes Implications:**
```yaml
# HPA will scale pods based on CPU/memory
# StatelessWorker grains will automatically utilize new pods
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
spec:
  minReplicas: 3
  maxReplicas: 20
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
```

✅ **Verdict:** StatelessWorker is the right choice for effect dispatch scalability.

### No Single Point of Contention ✅

**Design Choice:** Effects are dispatched with `Guid.NewGuid().ToString()` as grain key

```csharp
var dispatcher = GrainFactory.GetGrain<IEffectDispatcherGrain>(Guid.NewGuid().ToString());
```

**Implications:**
- Each dispatch gets a unique grain key
- No hot-spotting on a single grain
- Work distributes evenly across StatelessWorker pool

⚠️ **Minor Concern:** GUID generation is cheap but adds entropy. Consider if sequential patterns would help debugging (e.g., `{aggregateKey}:{timestamp}`).

---

## Pod Lifecycle Concerns

### Concern 1: Pod Termination During Effect Execution 🔴

**Scenario:**
1. Pod A dispatches effect to Pod B
2. Pod B starts executing effect (calling email API)
3. Pod B receives SIGTERM (scaling down or rolling update)
4. Effect is lost mid-execution

**Current Design:** No acknowledgment, no retry, no durability.

**Impact Assessment:**

| Effect Type | Impact of Loss | Acceptable? |
|-------------|----------------|-------------|
| Notification (email) | User doesn't get email | Maybe |
| Audit log | Compliance gap | ❌ No |
| Integration event | Downstream out of sync | ❌ No |
| Saga step | Saga stuck | ❌ No |

**Recommendations:**

1. **For V1:** Document that effects are "best effort, at-most-once"
2. **For critical effects:** Recommend Outbox Pattern separately
3. **Add graceful shutdown handling:**

```csharp
public class EffectDispatcherGrain : Grain, IEffectDispatcherGrain
{
    private CancellationTokenSource? shutdownCts;
    
    public override Task OnActivateAsync(CancellationToken ct)
    {
        shutdownCts = new CancellationTokenSource();
        return base.OnActivateAsync(ct);
    }
    
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        shutdownCts?.Cancel();
        // Give in-flight effects 5s to complete
        await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
        await base.OnDeactivateAsync(reason, ct);
    }
}
```

### Concern 2: Cold Start / Effect Registration 🟡

**Scenario:**
1. New pod joins cluster
2. Immediately receives effect dispatch
3. DI container hasn't fully warmed up
4. `IEffectRegistry.GetEffects()` scans assemblies (slow)

**Impact:** First few effect dispatches may be slow (100-500ms overhead).

**Recommendations:**

1. **Pre-warm on startup:**
```csharp
// In Program.cs
await host.Services.GetRequiredService<IEffectRegistry>()
    .WarmupAsync(knownAggregateTypes);
```

2. **Consider lazy caching in registry:**
```csharp
private readonly ConcurrentDictionary<string, Lazy<IEnumerable<object>>> effectCache = new();
```

---

## Distributed Systems Concerns

### Concern 3: No Idempotency Guarantee 🔴

**Scenario (Duplicate Delivery):**
1. Aggregate grain dispatches effect
2. Network partition or timeout
3. Orleans retries internally (or doesn't—depends on [OneWay])
4. Effect runs twice

**Scenario (Reprocessing):**
1. Event store replays events (rehydration, projection rebuild)
2. Effects fire again for historical events

**Impact:** 
- Duplicate emails sent
- Duplicate API calls
- Duplicate saga steps (especially dangerous)

**Recommendations:**

1. **Document idempotency requirement for effect authors:**
```markdown
## Effect Idempotency

Effects MUST be idempotent. The same event may trigger the same effect 
multiple times due to:
- Network retries
- Pod restarts during processing
- Event replay during rehydration

Use idempotency keys:
```csharp
protected override async Task HandleAsync(AccountOpened e, string key, CancellationToken ct)
{
    var idempotencyKey = $"welcome-email:{key}:{e.EventId}";
    if (await cache.ExistsAsync(idempotencyKey)) return;
    
    await emailService.SendWelcomeEmailAsync(...);
    await cache.SetAsync(idempotencyKey, "sent", TimeSpan.FromDays(7));
}
```
```

2. **Provide idempotency helper:**
```csharp
public abstract class IdempotentEventEffectBase<TEvent, TAggregate> : EventEffectBase<TEvent, TAggregate>
{
    protected IIdempotencyStore Store { get; }
    
    protected sealed override async Task HandleAsync(TEvent e, string key, CancellationToken ct)
    {
        var idempotencyKey = GetIdempotencyKey(e, key);
        if (await Store.TryAcquireAsync(idempotencyKey, ct))
        {
            await HandleIdempotentAsync(e, key, ct);
        }
    }
    
    protected abstract string GetIdempotencyKey(TEvent e, string aggregateKey);
    protected abstract Task HandleIdempotentAsync(TEvent e, string key, CancellationToken ct);
}
```

### Concern 4: Event Serialization Across Grain Boundary 🟡

**Design:**
```csharp
Task DispatchAsync(
    string aggregateTypeName,
    string aggregateKey,
    IReadOnlyList<object> events,  // <-- Serialized across grain boundary
    CancellationToken cancellationToken
);
```

**Concerns:**
- Events must be Orleans-serializable
- Large events = network overhead
- Polymorphic `object` list requires type preservation

**Recommendations:**

1. **Verify Orleans serialization covers all event types** - Add test
2. **Consider event metadata instead of full events:**
```csharp
public sealed record EffectDispatchRequest(
    string AggregateTypeName,
    string AggregateKey,
    IReadOnlyList<EffectEventInfo> Events
);

public sealed record EffectEventInfo(
    string EventTypeName,
    string SerializedEventJson  // Or byte[]
);
```

This makes serialization explicit and debuggable.

---

## Observability Requirements

### Required Metrics 📊

| Metric | Type | Labels | Purpose |
|--------|------|--------|---------|
| `effect.dispatched.total` | Counter | aggregate_type, event_type | Volume |
| `effect.execution.duration` | Histogram | aggregate_type, effect_type | Latency |
| `effect.execution.errors` | Counter | aggregate_type, effect_type, error_type | Error rate |
| `effect.queue.depth` | Gauge | silo_id | Backpressure |
| `effect.dropped.total` | Counter | aggregate_type, reason | Loss visibility |

### Required Logging 📝

```csharp
// Structured log fields (must be consistent)
{
    "event": "effect.dispatching",
    "aggregate_type": "BankAccountAggregate",
    "aggregate_key": "acc-123",
    "event_type": "AccountOpened",
    "event_count": 1,
    "correlation_id": "req-abc-123",
    "silo_id": "silo-1"
}

{
    "event": "effect.completed",
    "effect_type": "AccountOpenedEffect",
    "duration_ms": 145,
    "success": true
}

{
    "event": "effect.failed",
    "effect_type": "AccountOpenedEffect", 
    "error_type": "HttpRequestException",
    "error_message": "Connection refused",
    "will_retry": false
}
```

### Distributed Tracing 🔗

**Requirement:** Effect execution MUST propagate trace context from original command.

```csharp
public async Task DispatchAsync(...)
{
    // Ensure Activity/Trace context flows
    using var activity = ActivitySource.StartActivity("effect.dispatch");
    activity?.SetTag("aggregate.type", aggregateTypeName);
    activity?.SetTag("aggregate.key", aggregateKey);
    
    foreach (var eventData in events)
    {
        using var effectActivity = ActivitySource.StartActivity("effect.execute");
        // ...
    }
}
```

---

## Kubernetes/Helm Considerations

### Resource Limits

```yaml
# Effect dispatcher pods may need more CPU for concurrent effects
resources:
  requests:
    cpu: "500m"
    memory: "512Mi"
  limits:
    cpu: "2000m"  # Effects may be CPU-bound (JSON serialization, etc.)
    memory: "1Gi"
```

### Pod Disruption Budget

```yaml
# Ensure at least N effect dispatchers are always running
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: orleans-silo-pdb
spec:
  minAvailable: 2  # Keep at least 2 silos during updates
  selector:
    matchLabels:
      app: orleans-silo
```

### Graceful Shutdown

```yaml
# Give effects time to complete during pod termination
spec:
  terminationGracePeriodSeconds: 60  # Default 30s may be too short
```

---

## Failure Mode Analysis

| Failure | Detection | Impact | Recovery |
|---------|-----------|--------|----------|
| Effect throws exception | Logged | Effect lost | Manual retry / reprocess |
| External API timeout | Effect timeout | Effect lost | Retry in effect logic |
| Pod crash mid-effect | None (fire-and-forget) | Effect lost | Event replay |
| Network partition | Orleans membership | Delayed effects | Cluster heals |
| DI resolution failure | Exception log | Effect lost | Fix config, restart |

**Key Observation:** Many failures result in "effect lost." This is acceptable for notifications but NOT for critical business operations.

---

## Recommendations Summary

### P0 (Blocker for Production)

1. **Add observability** - Metrics, structured logging, trace propagation
2. **Document at-most-once semantics** - Effects are not guaranteed
3. **Add idempotency guidance** - With code examples

### P1 (Should Have)

4. **Graceful shutdown handling** - Give in-flight effects time to complete
5. **Pre-warm effect registry** - Avoid cold start penalty
6. **Event serialization test** - Verify all events cross grain boundary

### P2 (Nice to Have)

7. **Idempotent effect base class** - Reduce boilerplate
8. **Backpressure/circuit breaker** - For high-volume scenarios
9. **Effect dead letter queue** - Visibility into dropped effects

---

## Verdict

**Conditional Approval** - Ready for development/staging environments.

**Before Production:**
- [ ] Observability implemented (metrics, logging, tracing)
- [ ] At-most-once semantics documented
- [ ] Idempotency patterns documented with examples
- [ ] Graceful shutdown tested with pod termination
- [ ] Load tested with realistic effect volumes

The StatelessWorker + [OneWay] pattern is operationally sound for scalability. The main gap is visibility into failures and clear documentation of reliability guarantees.
