# Implementation Plan

## Detailed Plan
1. Identify Spring manual registration wrappers and the generated methods they call.
	- Files: samples/Spring/Spring.Client/Registrations/SpringDomainClientRegistrations.cs
				samples/Spring/Spring.Server/Registrations/SpringDomainServerRegistrations.cs
				samples/Spring/Spring.Silo/Registrations/SpringDomainSiloRegistrations.cs
2. Add generator output for AddSpringDomain wrappers:
	- Client: new generator in src/Inlet.Client.Generators (e.g., SpringDomainClientRegistrationGenerator).
	- Server: new generator in src/Inlet.Server.Generators (e.g., SpringDomainServerRegistrationGenerator).
	- Silo: new generator in src/Inlet.Silo.Generators (e.g., SpringDomainSiloRegistrationGenerator).
3. Scope generation to Spring projects by target root namespace:
	- Use TargetNamespaceResolver.RootNamespaceProperty/AssemblyNameProperty.
	- Generate only when target root namespace equals Spring.Client, Spring.Server, or Spring.Silo.
4. Emit wrappers to Spring.*.Registrations namespace with AddSpringDomain method.
	- Client wrapper calls AddBankAccountAggregateFeature, AddMoneyTransferSagaFeature, AddProjectionsFeature.
	- Server wrapper calls AddBankAccountAggregateMappers and Add*ProjectionMappers.
	- Silo wrapper calls Add*Aggregate, Add*Projection, Add*Saga.
5. Remove manual SpringDomain*Registrations classes.
6. Verify Program.cs stays unchanged (still uses AddSpringDomain).
7. Update any instruction/docs if generator-driven registrations change the guidance.

## Files to Change
- src/Inlet.Client.Generators/* (new generator class)
- src/Inlet.Server.Generators/* (new generator class)
- src/Inlet.Silo.Generators/* (new generator class)
- samples/Spring/Spring.Client/Registrations/SpringDomainClientRegistrations.cs (delete)
- samples/Spring/Spring.Server/Registrations/SpringDomainServerRegistrations.cs (delete)
- samples/Spring/Spring.Silo/Registrations/SpringDomainSiloRegistrations.cs (delete)

## Tests
- No new tests planned unless generator regressions are observed (sample scope).

## Rollout / Backout
- Rollout: generate wrappers and delete manual files in same change.
- Backout: restore manual wrappers if generator output fails.

## Risks + Mitigations
- Risk: Generator scope too broad. Mitigation: strict target root namespace gating.
- Risk: Missing generated method. Mitigation: mirror manual wrapper list exactly.
