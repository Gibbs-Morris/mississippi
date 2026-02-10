# Learned

- UNVERIFIED: Builder contracts include feature-level methods that return to parent builders (for example `IReservoirFeatureBuilder<TState>.Done()`).
- UNVERIFIED: Builder entry points live under Sdk.Client/Sdk.Server/Sdk.Silo extensions and Reservoir/Aqueduct/Inlet registrations.
- UNVERIFIED: There are generator outputs and tests asserting specific registration chaining behavior.

## Verification targets
- Builder contracts under src/**.Abstractions/Builders.
- Builder implementations under src/**/Builders and registration extension methods.
- Tests and samples that chain builder APIs.
- Generator outputs that embed registration snippets.
