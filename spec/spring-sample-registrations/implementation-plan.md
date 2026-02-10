# Implementation Plan

## Detailed Plan
1. Confirm decision on using PendingSourceGenerator in Spring samples.
2. Add client registration class:
	- File: samples/Spring/Spring.Client/Registrations/SpringDomainClientRegistrations.cs (name TBD).
	- Extension on IReservoirBuilder (or IMississippiClientBuilder) named AddSpringDomain.
	- Calls generated AddBankAccountAggregateFeature, AddMoneyTransferSagaFeature, AddProjectionsFeature.
3. Add server registration class:
	- File: samples/Spring/Spring.Server/Registrations/SpringDomainServerRegistrations.cs (name TBD).
	- Extension on IMississippiServerBuilder or IServiceCollection named AddSpringDomain.
	- Calls generated AddBankAccountAggregateMappers and Add*ProjectionMappers.
4. Add silo registration class:
	- File: samples/Spring/Spring.Silo/Registrations/SpringDomainSiloRegistrations.cs (name TBD).
	- Extension on IMississippiSiloBuilder named AddSpringDomain.
	- Calls generated Add*Aggregate, Add*Projection, Add*Saga methods.
5. Tag all three registration classes with [PendingSourceGenerator("...")].
6. Update Spring program wiring:
	- samples/Spring/Spring.Client/Program.cs: replace manual Add*Feature list with AddSpringDomain.
	- samples/Spring/Spring.Server/Program.cs: replace manual Add*Mapper list with AddSpringDomain.
	- samples/Spring/Spring.Silo/Program.cs: replace manual Add*Aggregate/Projection/Saga list with AddSpringDomain.
7. Tests:
	- Add minimal L0 tests in Spring.Domain.L0Tests (or Spring.Client/Server/Silo L0 tests) to assert
	  AddSpringDomain registers expected services (use DI to resolve one generated type per category).
8. Docs:
	- If Spring sample guidance mentions manual lists, update to reference AddSpringDomain.

## Data Model / Config Changes
- None.

## API / Contract Changes
- Sample-only extension methods; no framework public API changes.

## Observability
- None.

## Rollout / Backout
- Replace Program.cs calls; revert to manual lists if needed.

## Validation Checklist
- Build/tests deferred per current request.
- Ensure AddSpringDomain uses ConfigureServices-only pattern.
- Verify generated methods exist for each registration call.
