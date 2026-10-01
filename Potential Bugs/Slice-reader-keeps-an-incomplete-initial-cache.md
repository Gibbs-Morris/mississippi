# Slice reader keeps failing after an incomplete initial read

## Source location

- Project: `Brooks.Runtime`.
- Source file: [src/Brooks.Runtime/Reader/BrookSliceReaderGrain.cs lines 99-110](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Reader/BrookSliceReaderGrain.cs#L99-L110).
- Type: `BrookSliceReaderGrain`.
- Member: `OnActivateAsync / ReadAsync / PopulateCacheFromBrookAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The slice reader loads its events once during activation. A successful storage query that returns fewer events than requested is kept as its cache. A read beyond that cache throws, but the error neither refreshes the cache nor deactivates the reader. Retrying the same range therefore repeats the failure even after storage can return all of its events.

## Trigger

A reader first requests a particular saved range while its storage query temporarily returns only part of that range. This could occur with a provider that briefly exposes a saved cursor before all queried events are visible. Storage later returns the complete range, and the caller retries exactly the same range.

## Potential impact

Reads for that cached range can continue failing until the slice activation is replaced or explicitly deactivated. A short-lived storage visibility delay can become a lasting read failure. The evidence does not show lost durable events or establish that every default Cosmos deployment has this timing.

## Evidence

- [src/Brooks.Runtime/Reader/BrookSliceReaderGrain.cs lines 74-81](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Reader/BrookSliceReaderGrain.cs#L74-L81): Activation calls PopulateCacheFromBrookAsync once. There is no later cache-loading path in ReadAsync.
- [src/Brooks.Runtime/Reader/BrookSliceReaderGrain.cs lines 99-110](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Reader/BrookSliceReaderGrain.cs#L99-L110): The cached length determines its final position. A later request beyond that position throws without refreshing or deactivating the reader.
- [src/Brooks.Runtime/Reader/BrookSliceReaderGrain.cs lines 156-167](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Reader/BrookSliceReaderGrain.cs#L156-L167): A normally completed short or empty query is assigned to Cache without checking that it covers the requested range.
- [src/Brooks.Runtime/Reader/BrookReaderGrain.cs lines 149-157](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Reader/BrookReaderGrain.cs#L149-L157), [src/Brooks.Runtime/Reader/BrookReaderGrain.cs lines 171-177](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Runtime/Reader/BrookReaderGrain.cs#L171-L177): The public reader accepts an explicit end position and obtains its slice grain from the exact start/count range. A retry of the same range reuses that grain.

## Verification notes

Full source inspection. No provider visibility fault-injection or runtime reproduction was run.

- Use a storage reader that completes normally with part of a range on activation, then returns the full range. Retry the same ReadBatchAsync request on the same slice and check whether storage is queried again.
- Confirm a supported provider or caller can expose the incomplete first read. A provider guaranteeing the entire committed range is visible before a cursor can be read avoids this trigger.
- Keep the same range key in the reproduction. A different end position can select another slice grain and conceal the retained cache.

## Confidence

**Medium**. The retained-cache failure is clear in the implementation. A realistic incomplete first read depends on the selected storage provider's visibility and caller contract, which need independent verification.
