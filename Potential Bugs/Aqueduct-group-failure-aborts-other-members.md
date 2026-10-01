# One group member failure stops sends to the remaining members

## Source location

- Project: `Aqueduct.Runtime`.
- Source file: [src/Aqueduct.Runtime/Grains/SignalRGroupGrain.cs lines 167-171](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Runtime/Grains/SignalRGroupGrain.cs#L167-L171).
- Type: `SignalRGroupGrain`.
- Member: `SendMessageAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

A group sends to its members one at a time. If one send throws, the loop exits before trying later members. The public operation describes sending to every connection in the group, although its intended per-member failure policy needs confirmation.

## Trigger

A group has members routed through different healthy/unhealthy runtime or gateway paths, and a member encountered early in the snapshot throws an Orleans/stream publication exception while other member paths remain usable.

## Potential impact

The caller sees an error, and later healthy members receive no delivery attempt for that call. This does not assume durable delivery or guaranteed browser receipt.

## Evidence

- [src/Aqueduct.Runtime/Grains/SignalRGroupGrain.cs lines 162-175](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Runtime/Grains/SignalRGroupGrain.cs#L162-L175): One snapshot is enumerated; each awaited call can terminate the loop before remaining IDs are resolved/sent.
- [src/Aqueduct.Runtime/Grains/SignalRClientGrain.cs lines 224-235](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Runtime/Grains/SignalRClientGrain.cs#L224-L235): Client sends await stream.OnNextAsync and do not absorb publication failures.
- [src/Aqueduct.Abstractions/Grains/ISignalRGroupGrain.cs lines 58-68](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Aqueduct.Abstractions/Grains/ISignalRGroupGrain.cs#L58-L68): The method's fanout surface is all connections in the group.
- [docs/Docusaurus/docs/aqueduct/operations/operations.md lines 32-35](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/aqueduct/operations/operations.md#L32-L35): Durability, replay and zero-loss are not promised; the report is limited to skipping otherwise viable member attempts after one failure.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Control the membership enumeration or identify the first member, fail just that client's SendMessageAsync, and verify later healthy client sends were not invoked.
- Do not equate a successful publish with durable browser receipt. Independently confirm whether fail-fast fanout is an intended API contract before remediation.

## Confidence

**Medium**. The short-circuit is deterministic, but best-effort delivery does not itself require every recipient to succeed; the intended per-recipient failure boundary merits confirmation.
