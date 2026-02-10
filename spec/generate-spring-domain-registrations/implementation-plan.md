# Implementation Plan

## Detailed Plan
1. Identify Spring manual registration wrappers and the generated methods they call.
	- Files: samples/Spring/Spring.Client/Registrations/SpringDomainClientRegistrations.cs
				samples/Spring/Spring.Server/Registrations/SpringDomainServerRegistrations.cs
				samples/Spring/Spring.Silo/Registrations/SpringDomainSiloRegistrations.cs
2. Add generator output for AddSpringDomain wrappers:
	- Client: new generic generator in src/Inlet.Client.Generators (e.g., DomainClientRegistrationGenerator).
	- Server: new generic generator in src/Inlet.Server.Generators (e.g., DomainServerRegistrationGenerator).
	- Silo: new generic generator in src/Inlet.Silo.Generators (e.g., DomainSiloRegistrationGenerator).
3. Scope generation to Spring projects by target root namespace:
	- Use TargetNamespaceResolver.RootNamespaceProperty/AssemblyNameProperty.
	- Generate only when target root namespace ends with .Client, .Server, or .Silo.
	- Derive product name from target root namespace.
4. Emit wrappers to Spring.*.Registrations namespace with AddSpringDomain method.
	- Client wrapper calls Add{Aggregate}AggregateFeature, Add{Saga}SagaFeature, AddProjectionsFeature.
	- Server wrapper calls Add{Aggregate}AggregateMappers and Add{Projection}ProjectionMappers.
	- Silo wrapper calls Add{Aggregate}Aggregate, Add{Projection}Projection, Add{Saga}Saga.
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
