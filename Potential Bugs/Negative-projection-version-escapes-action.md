# Negative projection version throws instead of returning the documented response

## Source location

- Project: `DomainModeling.Gateway`.
- Source file: [src/DomainModeling.Gateway/UxProjectionControllerBase{TProjection,TDto}.cs lines 172-186](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Gateway/UxProjectionControllerBase%7BTProjection%2CTDto%7D.cs#L172-L186).
- Type: `UxProjectionControllerBase<TProjection,TDto>`.
- Member: `GetAtVersionAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The historical endpoint accepts a signed long number in its route. It immediately constructs BrookPosition without checking the lower bound. A value such as -2 throws before the action can return its documented NotFound result.

## Trigger

Request a configured historical projection endpoint ending in /at/-2 with an ordinary entity ID.

## Potential impact

Simple invalid client input makes the action throw. A host without a special exception handler can return a server error. A host-level handler may change that status, but the action still does not produce its documented result.

## Evidence

- [src/DomainModeling.Gateway/UxProjectionControllerBase{TProjection,TDto}.cs lines 168-186](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Gateway/UxProjectionControllerBase%7BTProjection%2CTDto%7D.cs#L168-L186): The return documentation says invalid versions produce NotFound. The route uses version:long at 172, and new BrookPosition(version) at 181 precedes the only NotFound branch.
- [src/Brooks.Abstractions/BrookPosition.cs lines 22-31](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Brooks.Abstractions/BrookPosition.cs#L22-L31): The BrookPosition constructor throws ArgumentOutOfRangeException when value < -1.
- [src/DomainModeling.Runtime/UxProjectionGrain.cs lines 137-147](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionGrain.cs#L137-L147): The runtime handles the allowed NotSet value (-1) by returning default, so -1 reaches the action's NotFound branch while lower values throw before any grain method call.
- [Framework or protocol reference](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-10.0#route-constraints): The official route-constraint table lists signed negative integers as valid long matches. A long constraint supplies no lower bound.

## Verification notes

Static inspection of complete assigned files and supporting contracts. No production/test files changed, and no runtime reproduction or tests executed.

- Call the action with -2 and inspect an HTTP request through a standard derived/generated controller; also keep the existing -1 NotSet behavior distinct.
- Inspect application-wide exception filters/middleware before claiming an exact HTTP status in a specific consumer host. No matching argument-exception mapping was found in the consulted gateway source.
- The expected result is taken from the method's existing invalid-version contract, not from an invented API requirement.

## Confidence

**High**. The route accepts the value and the locally constructed value object deterministically throws; only the final host HTTP status is conditional.
