# Store reset can restore a different value from its original initial state

## Source location

- Project: `Reservoir.Core`.
- Source file: [src/Reservoir.Core/Store.cs lines 89-92](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/Store.cs#L89-L92).
- Type: `Store / FeatureStateRegistration<TState>`.
- Member: `DI constructor initial state capture`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The store reads InitialState twice: once for live state and once for its reset snapshot. The standard registration constructs a new state on each read. A generated ID or startup timestamp can therefore differ between the two snapshots.

## Trigger

Register an immutable feature whose initial constructor/property initializer creates a session ID, correlation ID, or startup timestamp. Create the store, observe its initial value, and dispatch ResetToInitialStateAction later.

## Potential impact

Reset can change an identifier or timestamp instead of restoring the value the store originally exposed. Constant initial values hide the mismatch.

## Evidence

- [src/Reservoir.Core/Store.cs lines 88-92](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/Store.cs#L88-L92), [src/Reservoir.Core/Store.cs lines 319-322](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/Store.cs#L319-L322): Two getter calls capture distinct objects; ResetToInitialStateAction applies the separately saved initialFeatureStates.
- [src/Reservoir.Core/State/FeatureStateRegistration.cs lines 29-33](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/State/FeatureStateRegistration.cs#L29-L33): The standard getter returns new TState rather than a cached instance.
- [src/Reservoir.Abstractions/Actions/ResetToInitialStateAction.cs lines 3-10](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Abstractions/Actions/ResetToInitialStateAction.cs#L3-L10): Reset is documented to restore the store's initial feature values.
- [src/Reservoir.Abstractions/State/IFeatureStateRegistration.cs lines 25-28](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Abstractions/State/IFeatureStateRegistration.cs#L25-L28): InitialState is described as the initial instance for the feature.
- [tests/Reservoir.Core.L0Tests/StoreTests.cs lines 587-610](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/tests/Reservoir.Core.L0Tests/StoreTests.cs#L587-L610): Existing reset test verifies a deterministic default counter, which cannot expose the two-construction divergence.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Use a feature with an immutable GUID property initialized at construction; compare the first GetState value with the value immediately after reset.
- Verify whether nondeterministic feature initialization is supported before remediation. Constant/deterministic feature defaults avoid an observable value mismatch, though objects still differ.

## Confidence

**Medium**. Two constructions are certain, but the user-visible divergence requires a feature initializer that produces different valid initial values.
