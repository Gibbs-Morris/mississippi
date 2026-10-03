# Latest-only fetcher data is labelled with the older notification version

## Source location

- Project: `Inlet.Client`.
- Source file: [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 414-421](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L414-L421).
- Type: `InletSignalRActionEffect`.
- Member: `OnProjectionUpdatedAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

A supported fetcher can implement only the latest-read method; the versioned method then falls back to that read. The update handler ignores the returned version and labels its data with the notification's requested version instead.

## Trigger

Register a supported custom IProjectionFetcher implementing only FetchAsync. A notification names version N, but the latest read completes after the projection has advanced to M>N and returns data/version M.

## Potential impact

The store can hold data from version M while advertising version N. Consumers comparing or displaying versions receive inconsistent data and metadata. This differs from the documented possibility of correctly labelled reads finishing out of order.

## Evidence

- [src/Inlet.Client/ActionEffects/IProjectionFetcher.cs lines 45-62](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/IProjectionFetcher.cs#L45-L62): The default versioned API falls back to latest FetchAsync for backward compatibility.
- [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 414-422](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L414-L422): Result.Version is ignored; newVersion is supplied to the updated action.
- [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 230-234](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L230-L234), [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 338-342](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L338-L342), [src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs lines 465-469](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs#L465-L469): The ordinary refresh, initial, and reconnect paths correctly propagate result.Version.
- [src/Inlet.Client/Reducers/ProjectionsReducer.cs lines 128-144](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/Reducers/ProjectionsReducer.cs#L128-L144): The updated action's version and DTO are stored together without correcting the label.
- [src/Inlet.Client/InletBlazorSignalRBuilder.cs lines 57-68](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/InletBlazorSignalRBuilder.cs#L57-L68), [src/Inlet.Client/InletBlazorSignalRBuilder.cs lines 163-166](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/InletBlazorSignalRBuilder.cs#L163-L166): Registering a custom fetcher is a supported public composition path.
- [docs/Docusaurus/docs/inlet/reference/generated-contracts.md lines 120-122](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/inlet/reference/generated-contracts.md#L120-L122): Out-of-order correctly labelled reads are documented; this report concerns mislabelling one returned result.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Use a latest-only fetcher returning ProjectionFetchResult.Create(dtoAtM,M), deliver a notification for N, and inspect the emitted ProjectionUpdatedAction version.
- An exact-version fetcher returning N avoids the trigger. Verify result data and metadata together; no monotonic-update policy is requested.

## Confidence

**High**. The supported interface fallback can return a different version, and the call site deterministically discards that version.
