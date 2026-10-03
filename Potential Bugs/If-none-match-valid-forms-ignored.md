# Conditional projection requests ignore valid matching version headers

## Source location

- Project: `DomainModeling.Gateway`.
- Source file: [src/DomainModeling.Gateway/UxProjectionControllerBase{TProjection,TDto}.cs lines 137-144](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Gateway/UxProjectionControllerBase%7BTProjection%2CTDto%7D.cs#L137-L144).
- Type: `UxProjectionControllerBase<TProjection,TDto>`.
- Member: `GetAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The controller compares the entire If-None-Match header with one exact string. That header tells the server which response versions a client already has. Valid weak tags such as W/"5", a list including "5", and the wildcard * do not match the literal string even when the current version is 5.

## Trigger

A browser, cache, or other standards-compliant client sends a weak validator, a list of stored validators including the current tag, or the wildcard for an existing projection.

## Potential impact

The server fetches and returns a full 200 response where matching GET conditions require a 304 response without the body. This adds work and transfer and breaks the endpoint's advertised conditional request behaviour.

## Evidence

- [src/DomainModeling.Gateway/UxProjectionControllerBase{TProjection,TDto}.cs lines 104-116](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Gateway/UxProjectionControllerBase%7BTProjection%2CTDto%7D.cs#L104-L116): The endpoint advertises HTTP ETag/If-None-Match support and 304 responses for unchanged projections.
- [src/DomainModeling.Gateway/UxProjectionControllerBase{TProjection,TDto}.cs lines 137-144](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Gateway/UxProjectionControllerBase%7BTProjection%2CTDto%7D.cs#L137-L144): The only condition uses Request.Headers.IfNoneMatch.ToString() == currentETag, without parsing tags, recognizing wildcard, or weak comparison.
- [src/DomainModeling.Gateway/UxProjectionControllerBase{TProjection,TDto}.cs lines 147-159](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/DomainModeling.Gateway/UxProjectionControllerBase%7BTProjection%2CTDto%7D.cs#L147-L159): All syntactically valid forms that fail literal equality fall through to projection data retrieval and an Ok response.
- [Framework or protocol reference](https://www.rfc-editor.org/rfc/rfc9110.html#section-13.1.2): RFC 9110 section 13.1.2 defines wildcard or a list of entity tags, requires weak comparison, and requires 304 for GET when the representation matches the condition.

## Verification notes

Static inspection of complete assigned files and supporting contracts. No production/test files changed, and no runtime reproduction or tests executed.

- For current version 5, verify exact "5", weak W/"5", a list containing "5", separate header field values containing a matching tag, and wildcard * against an existing representation.
- The finding concerns valid forms only; malformed combinations of wildcard and tag-list elements are not part of its trigger.
- Keep this protocol parsing defect separate from the independently established version/body race.

## Confidence

**High**. The literal string comparison has deterministic false results for the cited valid matching forms, and the required protocol semantics are explicit.
