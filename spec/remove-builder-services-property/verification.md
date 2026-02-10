# Verification

## Claim List
1. Builder interfaces no longer expose `IServiceCollection Services`.
2. Builder implementations no longer expose public Services properties.
3. All internal registrations use `ConfigureServices` instead of `.Services` access.
4. Public extension methods remain functional without `.Services` access.
5. Docs and samples do not reference `.Services` for builder registration.

## Questions
- Q1: Which public builder interfaces expose `IServiceCollection Services` today?
- Q2: Which builder implementations expose public `Services` properties and where are they used internally?
- Q3: Which extension methods create child builders by accessing `.Services`?
- Q4: Where do registration helpers call `.Services` on IMississippi* builders or IAqueductServerBuilder?
- Q5: Do any tests depend on `.Services` for assertions or service provider construction?
- Q6: Do any generator outputs or samples reference `.Services` on Mississippi builders?
- Q7: Can all direct `.Services` usages be replaced by `ConfigureServices` without losing functionality?
- Q8: What documentation references `.Services` on builder types?
- Q9: Which namespaces/projects will need updates due to the public contract change?
- Q10: Are there any Orleans-specific builder uses that require special handling (ISiloBuilder.Services)?
