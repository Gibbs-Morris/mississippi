# Projection response can advertise a different version from its data

## Source location

- Project: `DomainModeling.Gateway`.
- Source file: [src/DomainModeling.Gateway/UxProjectionControllerBase{TProjection,TDto}.cs lines 129-159](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Gateway/UxProjectionControllerBase%7BTProjection%2CTDto%7D.cs#L129-L159).
- Type: `UxProjectionControllerBase<TProjection,TDto>`.
- Member: `GetAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The controller reads the current version to create its ETag, the HTTP header used to identify a response version. It then asks the grain for the latest data, which reads the current version again. If the version advances between these calls, the response contains newer data with the older ETag.

## Trigger

A GET reads projection cursor N. Another request appends an event and the cursor advances to N+1 before the controller's subsequent grain.GetAsync performs its own latest-version lookup.

## Potential impact

Clients and caches can associate the data with the wrong version. They can make incorrect version comparisons or request unnecessary refreshes. The evidence does not establish lost data or an incorrect 304 response on every concurrent update.

## Evidence

- [src/DomainModeling.Gateway/UxProjectionControllerBase{TProjection,TDto}.cs lines 104-116](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Gateway/UxProjectionControllerBase%7BTProjection%2CTDto%7D.cs#L104-L116): The endpoint promises an ETag containing the projection version and conditional validation for unchanged projections.
- [src/DomainModeling.Gateway/UxProjectionControllerBase{TProjection,TDto}.cs lines 129-159](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Gateway/UxProjectionControllerBase%7BTProjection%2CTDto%7D.cs#L129-L159): The first GetLatestVersionAsync result creates currentETag at 137. Data comes from a separate GetAsync call at 148; the first tag is then attached at 156.
- [src/DomainModeling.Runtime/UxProjectionGrain.cs lines 109-134](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionGrain.cs#L109-L134): The projection grain's GetAsync independently obtains latestVersion at 117 and fetches that version with GetAtVersionAsync at 130.
- [src/DomainModeling.Runtime/UxProjectionGrain.cs lines 160-169](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionGrain.cs#L160-L169): Each latest-version lookup obtains the reactive cursor's current position, which can advance between the two grain calls.
- [src/DomainModeling.Runtime/UxProjectionGrain.cs lines 149-155](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Runtime/UxProjectionGrain.cs#L149-L155): The explicit GetAtVersionAsync API already routes to a cache keyed by the supplied version, establishing the available snapshot/version contract.

## Verification notes

Static inspection of complete assigned files and supporting contracts. No production/test files changed, and no runtime reproduction or tests executed.

- Use a controlled cursor sequence N then N+1, and assert that the returned DTO's state corresponds to the ETag version.
- The failure requires a cursor notification between the controller's two awaits; stable mock versions in ordinary endpoint tests do not exercise it.
- No expectation is imposed that a response include the newest possible write; the issue is that its advertised version and actual body must agree.

## Confidence

**High**. Two independent version reads and the unchanged first ETag are visible directly in the controller and grain.
