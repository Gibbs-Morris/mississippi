# PR #174 Worklog

This document tracks changes made during the review of PR #174 files.

---

## Review Sessions

### Session 1 - Initial Review (2026-01-02)

Review focused on interfaces and abstraction layer files in dependency order.

---

## Changes Made

### 1. Copyright Header Fixes

**Files Modified (McLaren Applied Ltd. → Gibbs-Morris):**

- `src/EventSourcing.UxProjections.Abstractions/UxProjectionCursorKey.cs`
- `src/EventSourcing.UxProjections.Abstractions/UxProjectionVersionedCacheKey.cs`

**Issue:** Copyright headers incorrectly stated "McLaren Applied Ltd." instead of "Gibbs-Morris".

---

### 2. Copyright Header Fixes (Corsair Software Ltd → Gibbs-Morris)

**Files Modified:**

- `samples/Cascade/Cascade.AppHost/Program.cs`
- `samples/Cascade/Cascade.L2Tests/TestBase.cs`
- `samples/Cascade/Cascade.L2Tests/PlaywrightFixture.cs`
- `samples/Cascade/Cascade.L2Tests/GlobalUsings.cs`
- `samples/Cascade/Cascade.L2Tests/PageObjects/LoginPage.cs`
- `samples/Cascade/Cascade.L2Tests/PageObjects/ChannelListPage.cs`
- `samples/Cascade/Cascade.L2Tests/PageObjects/ChannelViewPage.cs`
- `samples/Cascade/Cascade.L2Tests/Features/LoginTests.cs`
- `samples/Cascade/Cascade.L2Tests/Features/ChannelCreationTests.cs`
- `samples/Cascade/Cascade.L2Tests/Features/MessagingTests.cs`
- `samples/Cascade/Cascade.L2Tests/Features/RealTimeTests.cs`

**Issue:** Copyright headers incorrectly stated "Corsair Software Ltd" instead of "Gibbs-Morris".

---

### 3. Copyright Header Fixes (GMM → Gibbs-Morris)

**Files Modified:**

- `samples/Cascade/Cascade.Server/Program.cs`
- `samples/Cascade/Cascade.Server/Components/App.razor`
- `samples/Cascade/Cascade.Server/Components/_Imports.razor`
- `samples/Cascade/Cascade.Server/Components/Routes.razor`
- `samples/Cascade/Cascade.Server/Components/Pages/Home.razor`
- `samples/Cascade/Cascade.Server/Components/Pages/Error.razor`
- `samples/Cascade/Cascade.Server/Components/Pages/Login.razor`
- `samples/Cascade/Cascade.Server/Components/Layout/MainLayout.razor`
- `samples/Cascade/Cascade.Server/Components/Layout/NavMenu.razor`
- `samples/Cascade/Cascade.Server/Components/Services/IChatService.cs`
- `samples/Cascade/Cascade.Server/Components/Services/ChatService.cs`
- `samples/Cascade/Cascade.Server/Components/Services/ChatOperationException.cs`
- `samples/Cascade/Cascade.Server/Components/Services/IProjectionSubscriber.cs`
- `samples/Cascade/Cascade.Server/Components/Services/IProjectionSubscriberFactory.cs`
- `samples/Cascade/Cascade.Server/Components/Services/ProjectionSubscriber.cs`
- `samples/Cascade/Cascade.Server/Components/Services/ProjectionSubscriberFactory.cs`
- `samples/Cascade/Cascade.Server/Components/Services/ProjectionSubscriberLoggerExtensions.cs`
- `samples/Cascade/Cascade.Server/Components/Services/ProjectionErrorEventArgs.cs`
- `samples/Cascade/Cascade.Server/Components/Services/UserSession.cs`
- `samples/Cascade/Cascade.Server/Components/Services/CascadeServerServiceCollectionExtensions.cs`
- `samples/Cascade/Cascade.Server/wwwroot/css/app.css`

**Issue:** Copyright headers used abbreviation "GMM" instead of full "Gibbs-Morris".

---

### 4. Unit Test Key Format Fixes

**Files Modified:**

- `tests/EventSourcing.UxProjections.L0Tests/UxProjectionVersionedCacheGrainTests.cs`
- `tests/EventSourcing.UxProjections.L0Tests/UxProjectionCursorGrainIntegrationTests.cs`

**Issues Found:**

1. **UxProjectionVersionedCacheGrainTests.cs:**
   - Used 4-part key format `"TestProjection|TEST.MODULE.STREAM|entity-123|42"`
   - Should use 3-part format `"TEST.MODULE.STREAM|entity-123|42"` (brookName|entityId|version)
   - `UxProjectionVersionedCacheKey.Parse()` expects exactly 3 parts

2. **UxProjectionCursorGrainIntegrationTests.cs:**
   - Used `UxProjectionKey.ForGrain<TestProjection, TestGrain>(entityId)` producing 3-part key
   - Should use `UxProjectionCursorKey.FromBrookKey(BrookKey.ForGrain<TestGrain>(entityId))` producing 2-part key
   - `IUxProjectionCursorGrain` is keyed by `UxProjectionCursorKey` (brookName|entityId), not `UxProjectionKey`
   - Also removed obsolete `#pragma warning disable CS0618` directive

**Root Cause:** Tests were using wrong key types/formats that didn't match the grain's expected primary key parsing logic.

**Verification:** All 13 previously failing tests now pass.

---

## Summary

- **Files Reviewed**: 80+
- **Files Modified**: 36
- **Issues Found**: 5 (3 copyright patterns + 2 test key format issues)
- **Issues Fixed**: 5
- **Build Status**: ✅ 0 warnings, 0 errors
- **Unit Tests**: ✅ All L0 tests passed
