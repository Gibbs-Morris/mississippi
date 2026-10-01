# Default client routes discard an application's URL base path

## Source location

- Project: `Inlet.Client`, `Inlet.Client.Generators`.
- Source file: [src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs lines 32](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs#L32).
- Type: `AutoProjectionFetcher / HubConnectionProvider / generated command effects`.
- Member: `Default route construction`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

Default projection, command, and hub paths begin with /. Resolving such a path uses the origin root and discards a base path such as /bank/ from the client's configured address.

## Trigger

Deploy the client and gateway API/hub under a path base such as `https://example.test/bank/`, configure their base addresses accordingly, and use the standard default Inlet generated client paths.

## Potential impact

An application served under /bank/ can send requests to /api/... or /hubs/inlet instead of /bank/api/... or /bank/hubs/inlet. Those requests can return 404 or reach another application on the same origin. An intentionally separate gateway at the origin root avoids this trigger.

## Evidence

- [src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs lines 32](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs#L32), [src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs lines 52](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs#L52), [src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs lines 79](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs#L79), [src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs lines 103](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs#L103), [src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs lines 114-118](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs#L114-L118): The default slash-prefixed URL is passed to HttpClient without combining with the application path.
- [src/Inlet.Client/ActionEffects/HubConnectionProvider.cs lines 44-47](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs#L44-L47): HubPath is resolved with NavigationManager.ToAbsoluteUri.
- [src/Inlet.Client/ActionEffects/InletSignalRActionEffectOptions.cs lines 9-15](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffectOptions.cs#L9-L15): The default hub path is /hubs/inlet.
- [src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs lines 134-137](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs#L134-L137): Generated commands hard-code a slash-prefixed /api/aggregates/... prefix.
- [src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs lines 149-151](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs#L149-L151), [src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs lines 208-211](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs#L208-L211): That generated prefix is used directly in the HttpClient request.
- [src/Inlet.Client/InletBlazorSignalRBuilder.cs lines 95-125](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/InletBlazorSignalRBuilder.cs#L95-L125): Hub and projection paths can be overridden manually; those workarounds do not change generated command defaults.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Capture actual request URIs with BaseAddress `https://example.test/bank/` and a navigation base ending in /bank/. Compare with the gateway's actual PathBase mapping.
- This applies only when API and hub belong under the base path. A separately hosted origin-root gateway avoids the trigger. Generator custom aggregate-prefix mismatch is a separate report owned by the generator reviewer.

## Confidence

**Medium**. Leading-slash resolution is deterministic, but deployment endpoint placement and intended base-address contract must be verified independently.
