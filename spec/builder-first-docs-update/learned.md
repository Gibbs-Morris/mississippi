# Learned

## Repository facts
- UNVERIFIED: Docusaurus docs under docs/Docusaurus/docs/client-state-management reference IServiceCollection-based Reservoir registrations.
- UNVERIFIED: Docusaurus event-sourcing-sagas doc references AddInletSilo via IServiceCollection.
- UNVERIFIED: Reservoir registration APIs are now builder-first via IMississippiClientBuilder and IReservoirBuilder.

## Suspect doc pages (UNVERIFIED)
- docs/Docusaurus/docs/client-state-management/built-in-navigation.md
- docs/Docusaurus/docs/client-state-management/built-in-lifecycle.md
- docs/Docusaurus/docs/client-state-management/store.md
- docs/Docusaurus/docs/client-state-management/reservoir.md
- docs/Docusaurus/docs/client-state-management/devtools.md
- docs/Docusaurus/docs/client-state-management/effects.md
- docs/Docusaurus/docs/client-state-management/feature-state.md
- docs/Docusaurus/docs/client-state-management/reducers.md
- docs/Docusaurus/docs/event-sourcing-sagas.md

## Files to verify
- docs/Docusaurus/docs/client-state-management/*.md
- docs/Docusaurus/docs/event-sourcing-sagas.md
- src/Reservoir/ReservoirBuilderExtensions.cs
- src/Reservoir/Builders/ReservoirBuilder.cs
- src/Reservoir/Builders/ReservoirFeatureBuilder.cs
- src/Reservoir.Blazor/ReservoirDevToolsRegistrations.cs
