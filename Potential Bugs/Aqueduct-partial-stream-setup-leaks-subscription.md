# Retrying partial stream setup leaves extra server subscriptions

## Source location

- Project: `Aqueduct.Gateway`.
- Source file: [src/Aqueduct.Gateway/StreamSubscriptionManager.cs lines 115-127](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/StreamSubscriptionManager.cs#L115-L127).
- Type: `StreamSubscriptionManager`.
- Member: `EnsureInitializedAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Setup subscribes to the server stream before subscribing to the broadcast stream, and discards both handles. If the second step fails, the first subscription stays active. A retry repeats that first subscription because setup is still marked incomplete.

## Trigger

The server stream subscribe succeeds, the subsequent broadcast stream subscribe fails transiently, and a later connection or send retries initialization.

## Potential impact

Multiple active callbacks can deliver a server-targeted message more than once to the same connection. Repeated failures can leave more subscriptions. Observable duplication should be checked with the configured provider.

## Evidence

- [src/Aqueduct.Gateway/StreamSubscriptionManager.cs lines 113-127](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/StreamSubscriptionManager.cs#L113-L127): Server SubscribeAsync succeeds first; broadcast SubscribeAsync is awaited next; initialized is set only afterward. No handle or cleanup is retained.
- [src/Aqueduct.Gateway/StreamSubscriptionManager.cs lines 128-131](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/StreamSubscriptionManager.cs#L128-L131), [src/Aqueduct.Gateway/StreamSubscriptionManager.cs lines 74-83](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/StreamSubscriptionManager.cs#L74-L83): Finally releases the init lock only; Dispose does not unsubscribe either handle.
- [src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs lines 156-159](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs#L156-L159), [src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs lines 354-366](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs#L354-L366), [src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs lines 402-409](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs#L402-L409): Initialization is retried whenever IsInitialized is false; each callback directly delivers a server message.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Use an existing stream mock with the first broadcast subscription failing once. Record two server subscription handles and invoke both callbacks after retry.
- Verify Orleans stream semantics and any provider-specific deduplication before asserting duplicate observable messages for a particular deployment.
- This is separate from multi-hub singleton reuse and from failed heartbeat startup.

## Confidence

**High**. An already completed subscribe has no rollback path, and the next successful initialization repeats it.
