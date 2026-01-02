# Task 1.6: API ETag Support

**Status**: ⬜ Not Started  
**Depends On**: None (can be done in parallel with other Phase 1 tasks)

## Goal

Enhance `UxProjectionControllerBase` in `src/EventSourcing.UxProjections.Api/` to support `ETag` headers and conditional GET requests for efficient client caching.

## Acceptance Criteria

- [ ] GET endpoints return `ETag` header with projection version
- [ ] `If-None-Match` header support returns 304 Not Modified when version matches
- [ ] `If-Match` header support for conditional updates (future-proofing)
- [ ] Works with existing `GetLatestAsync` and `GetAtVersionAsync` endpoints
- [ ] L0 tests verify header handling and 304 responses

## Implementation Details

### Current Endpoint Pattern

Looking at existing `UxProjectionControllerBase`, endpoints likely follow:

```csharp
[HttpGet("{entityId}")]
public async Task<ActionResult<TProjection>> GetAsync(string entityId, CancellationToken ct);

[HttpGet("{entityId}/version")]
public async Task<ActionResult<long>> GetVersionAsync(string entityId, CancellationToken ct);

[HttpGet("{entityId}/at/{version}")]
public async Task<ActionResult<TProjection>> GetAtVersionAsync(string entityId, long version, CancellationToken ct);
```

### Enhanced GET with ETag

```csharp
[HttpGet("{entityId}")]
public async Task<ActionResult<TProjection>> GetAsync(
    [FromRoute] string entityId,
    [FromHeader(Name = "If-None-Match")] string? ifNoneMatch,
    CancellationToken ct)
{
    UxProjectionKey key = CreateProjectionKey(entityId);
    
    // Get current version first
    BrookPosition currentVersion = await GetVersionAsync(key, ct);
    string etag = $"\"{currentVersion.Value}\"";
    
    // Check conditional request
    if (ifNoneMatch is not null && ifNoneMatch == etag)
    {
        return StatusCode(StatusCodes.Status304NotModified);
    }
    
    // Fetch projection
    TProjection? projection = await GetProjectionAsync(key, ct);
    
    if (projection is null)
    {
        return NotFound();
    }
    
    // Set ETag header
    Response.Headers.ETag = etag;
    Response.Headers.CacheControl = "private, must-revalidate";
    
    return Ok(projection);
}
```

### ETag Format

- ETag value: `"{version}"` where version is the `BrookPosition.Value` (long)
- Weak ETags (`W/"..."`) not needed since version is exact
- Example: `ETag: "42"`

### Headers Added

| Header | Direction | Purpose |
| -------- | ----------- | --------- |
| `ETag` | Response | Current projection version |
| `Cache-Control` | Response | `private, must-revalidate` |
| `If-None-Match` | Request | Client's cached version |

## TDD Steps

1. **Red**: Create/update `UxProjectionControllerBaseTests` in `tests/EventSourcing.UxProjections.Api.L0Tests/`
   - Test: `GetAsync_ReturnsETagHeader`
   - Test: `GetAsync_WithMatchingIfNoneMatch_Returns304`
   - Test: `GetAsync_WithNonMatchingIfNoneMatch_ReturnsData`
   - Test: `GetAsync_WithoutIfNoneMatch_ReturnsData`

2. **Green**: Update `UxProjectionControllerBase`
   - Add `If-None-Match` parameter to GET method
   - Add ETag and Cache-Control response headers
   - Add 304 response logic

3. **Refactor**: Extract ETag formatting to helper method; consider extension method for reuse

## Files to Modify

- `src/EventSourcing.UxProjections.Api/UxProjectionControllerBase.cs`

## Files to Create/Update

- `tests/EventSourcing.UxProjections.Api.L0Tests/UxProjectionControllerBaseETagTests.cs`

## Notes

- ETag is version-based, not content hash - simpler and sufficient for event-sourced projections
- 304 response has no body, saving bandwidth
- Client workflow: Store ETag from response → Send in `If-None-Match` on next request
- This pairs with SignalR notifications: notification triggers refetch with ETag for efficient updates
