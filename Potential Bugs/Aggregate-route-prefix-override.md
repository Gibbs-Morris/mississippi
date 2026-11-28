# Generated aggregate client effects ignore the configured RoutePrefix

## Source location

- Project: `Inlet.Client.Generators`.
- Source file: [src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs lines 76-84](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs#L76-L84).
- Type: `CommandClientActionEffectsGenerator`.
- Member: `GenerateActionEffect`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The client route segment is always derived from the Commands namespace. GenerateAggregateEndpoints.RoutePrefix changes the server route but is never read by the effect generator.

## Trigger

An AccountAggregate in App.Domain.Aggregates.Account has [GenerateAggregateEndpoints(RoutePrefix="special-account")] and a generated Transfer command.

## Potential impact

The generated effect calls /api/aggregates/account/{entityId}/transfer while the controller exposes api/aggregates/special-account/{entityId}/transfer; normal generated client commands receive 404 or reach another matching route.

## Evidence

- [src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs lines 134-142](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs#L134-L142): Emits the derived AggregateRoutePrefix and command Route only.
- [src/Inlet.Generators.Abstractions/GenerateAggregateEndpointsAttribute.cs lines 55-68](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Abstractions/GenerateAggregateEndpointsAttribute.cs#L55-L68): Public RoutePrefix override contract.
- [src/Inlet.Gateway.Generators/AggregateControllerGenerator.cs lines 405-425](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/AggregateControllerGenerator.cs#L405-L425): Reads RoutePrefix from the aggregate marker and falls back only when empty.
- [src/Inlet.Gateway.Generators/AggregateControllerGenerator.cs lines 265-267](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/AggregateControllerGenerator.cs#L265-L267): Emits the server route from AggregateModel.RoutePrefix.
- Isolated probe (`aggregate-route-override`): Valid input has zero input errors. Controller output line 21 uses special-account; effect output line 46 uses account.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Separate from application-base-path leading-slash handling and HTTP verb selection; those reports belong to live_client_review.
- The route probe inspects emitted routes; deliberately omitted client/base-controller dependencies produce unrelated output diagnostics. No hosted HTTP call was executed.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
