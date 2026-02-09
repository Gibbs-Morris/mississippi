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
