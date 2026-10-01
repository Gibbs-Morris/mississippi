# User-targeted sends create a group name that the default factory rejects

## Source location

- Project: `Aqueduct.Gateway`.
- Source file: [src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs lines 329-331](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs#L329-L331).
- Type: `AqueductHubLifetimeManager<THub>`.
- Member: `SendUserAsync / SendUsersAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

SendUserAsync builds a group name by prefixing the user ID with user:. The default group-key constructor rejects names containing a colon. The standard user-send path therefore throws on its own internally generated name.

## Trigger

Call the standard SignalR Clients.User(userId) or Clients.Users(...) send operation after `AddAqueduct<THub>`() installs the default lifetime manager and factory.

## Potential impact

Clients.User or Clients.Users sends fail with ArgumentException before reaching any group or connection.

## Evidence

- [src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs lines 318-344](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs#L318-L344): SendUserAsync builds `user:<id>`; SendUsersAsync delegates each user to it.
- [src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs lines 261-274](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductHubLifetimeManager.cs#L261-L274): SendGroupAsync resolves GetGroupGrain before sending.
- [src/Aqueduct.Gateway/AqueductGrainFactory.cs lines 71-79](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductGrainFactory.cs#L71-L79): String overload constructs SignalRGroupKey.
- [src/Aqueduct.Abstractions/Keys/SignalRGroupKey.cs lines 34-39](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Abstractions/Keys/SignalRGroupKey.cs#L34-L39), [src/Aqueduct.Abstractions/Keys/SignalRGroupKey.cs lines 96-108](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Abstractions/Keys/SignalRGroupKey.cs#L96-L108): Constructor validates components; separator ':' is rejected.
- [src/Aqueduct.Gateway/AqueductRegistrations.cs lines 43-48](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Gateway/AqueductRegistrations.cs#L43-L48): Default AddAqueduct registers this factory and lifetime manager.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Use the default factory with a lifetime manager and any ordinary nonempty user ID; assert the separator exception occurs before grain dispatch.
- Check both single-user and multi-user sends. The old empty-group account understated the current failure; do not describe this only as dropped delivery.

## Confidence

**High**. The default registered call chain deterministically rejects the internally generated group name.
