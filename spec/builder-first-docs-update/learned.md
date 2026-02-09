# Learned

## Repository facts
- UNVERIFIED: Docusaurus docs under docs/Docusaurus/docs/client-state-management reference IServiceCollection-based Reservoir registrations.
- UNVERIFIED: Docusaurus event-sourcing-sagas doc references AddInletSilo via IServiceCollection.
- UNVERIFIED: Reservoir registration APIs are now builder-first via IMississippiClientBuilder and IReservoirBuilder.

## Files to verify
- docs/Docusaurus/docs/client-state-management/*.md
- docs/Docusaurus/docs/event-sourcing-sagas.md
- src/Reservoir/ReservoirBuilderExtensions.cs
- src/Reservoir/Builders/ReservoirBuilder.cs
- src/Reservoir/Builders/ReservoirFeatureBuilder.cs
- src/Reservoir.Blazor/ReservoirDevToolsRegistrations.cs
