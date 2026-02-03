# Learned

## Verified
- Coverage target: >=95% for changed code under src, with small exceptions if infeasible (per user request).

- Changed src files (from `git diff main...HEAD -- src`):
	- EventSourcing.Sagas.Abstractions/*.cs, EventSourcing.Sagas/*.cs, Inlet.Client.Generators/*.cs,
		Inlet.Server.Generators/SagaControllerGenerator.cs, Inlet.Silo.Generators/SagaSiloRegistrationGenerator.cs,
		Inlet.Generators.Abstractions/GenerateSagaEndpointsAttribute.cs, plus related csproj changes.

- Current coverage baselines (test-project-quality, SkipMutation):
	- EventSourcing.Sagas.L0Tests: 20.39%
	- Inlet.Client.Generators.L0Tests: 84.48%
	- Inlet.Server.Generators.L0Tests: 72.58%
	- Inlet.Silo.Generators.L0Tests: 71.71%

- Per-file line-rate (EventSourcing.Sagas + Abstractions) after latest test additions:
	- 100% for all changed saga/abstraction files except:
		- SagaStateMutator.cs: 85.71% (unreachable null-instance guard lines)
		- SagaStatePropertyMap.cs: 97.05%
	- NO_DATA entries correspond to marker/empty types or interfaces (SagaEvents, SagaInfrastructureReducers,
		ISagaState, ISagaStep, SagaOrchestrationEffectLoggerExtensions).

- Per-file line-rate (Inlet.Client/Server/Silo generators) after latest test additions:
	- All changed generator files at 99.50–100% (most at 100%).
	- GenerateSagaEndpointsAttribute.cs at 100%.

## UNVERIFIED
- None currently; outstanding exception is documented in verification.md.
