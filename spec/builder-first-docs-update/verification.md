# Verification

## Claim List
1. Docs no longer reference IServiceCollection-based Reservoir registrations.
2. Reservoir registration examples use IMississippiClientBuilder and IReservoirBuilder.
3. Reducer/effect examples use IReservoirFeatureBuilder via AddFeature.
4. DevTools examples use IReservoirBuilder.AddReservoirDevTools.
5. Sagas doc uses builder-based AddInletSilo and saga registrations.
6. Links in docs point to current builder extension implementations.

## Questions
- Q1: Which docs still reference services.AddReservoir or services.AddReducer?
- Q2: Which docs still reference AddFeatureState or AddRootReducer directly?
- Q3: Do Reservoir docs point to ReservoirBuilderExtensions and ReservoirFeatureBuilder (no ReservoirRegistrations links)?
- Q4: Are DevTools examples using reservoir.AddReservoirDevTools with updated link targets?
- Q5: Do reducers/effects/feature-state docs show AddFeature-based registration patterns?
- Q6: Does event-sourcing-sagas use mississippi.AddInletSilo in the example?
- Q7: Are any docs still pointing to deleted ReservoirRegistrations APIs?
- Q8: Do Store docs describe AddReservoir via Mississippi client builder?

## Answers
- A1: No references remain; `grep_search services.AddReservoir|services.AddReducer|services.AddActionEffect` returned no matches under docs/Docusaurus/docs.
- A2: No references remain; `grep_search AddFeatureState|AddRootReducer` returned no matches under docs/Docusaurus/docs.
- A3: Reservoir docs link to builder APIs, e.g. docs/Docusaurus/docs/client-state-management/reservoir.md lines 156-158.
- A4: DevTools examples use reservoir.AddReservoirDevTools with updated link targets in docs/Docusaurus/docs/client-state-management/devtools.md lines 26 and 32.
- A5: Reducers/effects/feature-state show AddFeature-based registration in docs/Docusaurus/docs/client-state-management/reducers.md lines 47-72, effects.md lines 165-167, feature-state.md lines 79-88.
- A6: event-sourcing-sagas uses mississippi.AddInletSilo in docs/Docusaurus/docs/event-sourcing-sagas.md lines 116-121.
- A7: `grep_search ReservoirRegistrations` returned no matches under docs/Docusaurus/docs.
- A8: Store registration uses Mississippi client builder in docs/Docusaurus/docs/client-state-management/store.md line 52.
