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
- Q2: Do the Reservoir docs point to ReservoirBuilderExtensions and ReservoirFeatureBuilder?
- Q3: Are DevTools examples using reservoir.AddReservoirDevTools and updated link targets?
- Q4: Do reducers/effects docs show AddFeature-based registration?
- Q5: Does event-sourcing-sagas use mississippi.AddInletSilo in the example?
- Q6: Are any doc links still pointing to deleted ReservoirRegistrations?
