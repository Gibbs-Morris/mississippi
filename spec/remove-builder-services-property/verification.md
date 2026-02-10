# Verification

## Claim List
1. Builder interfaces no longer expose `IServiceCollection Services`.
2. Builder implementations no longer expose public Services properties.
3. All internal registrations use `ConfigureServices` instead of `.Services` access.
4. Public extension methods remain functional without `.Services` access.
5. Docs and samples do not reference `.Services` for builder registration.

## Questions
- Q1: Which builder interfaces currently expose `Services`?
- Q2: Which builder implementations expose `Services` and how are they used?
- Q3: Where in the repo do extension methods access `.Services` directly?
- Q4: Can every `.Services` usage be replaced with `ConfigureServices`?
- Q5: Do generator outputs or samples rely on `.Services` access?
- Q6: What tests or docs need updates to align with the new pattern?
