# Learned

## Repository facts
- UNVERIFIED: Docusaurus docs under docs/Docusaurus/docs/client-state-management reference IServiceCollection-based Reservoir registrations.
- UNVERIFIED: Docusaurus event-sourcing-sagas doc references AddInletSilo via IServiceCollection.
- UNVERIFIED: Reservoir registration APIs are now builder-first via IMississippiClientBuilder and IReservoirBuilder.

## Verified facts
- Reservoir builder registration lives in src/Reservoir/ReservoirBuilderExtensions.cs (AddReservoir).
- Reservoir feature registration is via src/Reservoir/Builders/ReservoirBuilder.cs (AddFeature/AddMiddleware) and src/Reservoir/Builders/ReservoirFeatureBuilder.cs (AddReducer/AddActionEffect).
- DevTools registration is via src/Reservoir.Blazor/ReservoirDevToolsRegistrations.cs (AddReservoirDevTools).
- Docs updated to builder-first examples under docs/Docusaurus/docs/client-state-management/*.md and docs/Docusaurus/docs/event-sourcing-sagas.md.

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
