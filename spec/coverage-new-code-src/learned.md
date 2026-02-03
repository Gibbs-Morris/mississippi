# Learned

## Verified
- Coverage target: >=95% for changed code under src, with small exceptions if infeasible (per user request).

- Changed src files (from `git diff main...HEAD -- src`):
	- EventSourcing.Sagas.Abstractions/*.cs, EventSourcing.Sagas/*.cs, Inlet.Client.Generators/*.cs,
		Inlet.Server.Generators/SagaControllerGenerator.cs, Inlet.Silo.Generators/SagaSiloRegistrationGenerator.cs,
		Inlet.Generators.Abstractions/GenerateSagaEndpointsAttribute.cs, plus related csproj changes.

- Current coverage baselines (test-project-quality, SkipMutation):
	- EventSourcing.Sagas.L0Tests: 13.74%
	- Inlet.Client.Generators.L0Tests: 69.64%
	- Inlet.Server.Generators.L0Tests: 56.59%
	- Inlet.Silo.Generators.L0Tests: 53.16%
	- Inlet.Generators.Core.L0Tests: 95.14% (not directly tied to changed generator files but provides context)

- Per-file line-rate (EventSourcing.Sagas + Abstractions) shows several files at 0% or NO_DATA
	(e.g., SagaOrchestrationEffect.cs, StepResult.cs, CompensationResult.cs, SagaStepAttribute.cs).
- Per-file line-rate (Inlet.Client/Server/Silo generators) shows 0% for new generator files.

## UNVERIFIED
- Which specific generator branches remain uncovered after adding tests (to be verified via coverage reruns).
