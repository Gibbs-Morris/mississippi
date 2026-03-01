# CoV Review: Platform Engineer

- **Claims / hypotheses**: Platform engineers need robust telemetry. The builder doesn't obstruct existing logging via [LoggerMessage].
- **Verification questions**: Do the wrappers obscure the DI exceptions if resolution fails?
- **Evidence**: DI is delegated immediately to IServiceCollection.
- **Triangulation**: Wrappers don't queue or defer intents. Exceptions bubble up at startup just as native DI does.
- **Conclusion + confidence**: High.
- **Impact**: No loss of operability.

## Issues Identified
- **Issue**: Missing diagnostic markers about Obsolete mapping application during CI.
- **Why it matters**: We need logging that indicates when deprecated behaviors run, but the plan addresses compile-time via [Obsolete].
- **Proposed change**: None necessary, just keep it source-level.
- **Evidence**: "add in adding an obsulrated attribute to all the 'old' ways"
- **Confidence**: High.
