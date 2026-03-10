# Amendment 3 Review — Marketing & Contracts

## Persona

Marketing & Contracts reviewer — public naming clarity, contract discoverability, package naming consistency, changelog/migration communication quality.

## Findings

### 1. FLAW — "Verification-first enterprise" is never spelled out for end users

- **Issue**: The acronym `vfe` is defined once in the Executive Summary as "verification-first enterprise" but the manifest requirements (section 15) don't require that the manifest explains what "verification-first" means in practice to someone encountering the family for the first time. The manifest lists sections but doesn't require a "philosophy" or "what verification-first means" paragraph.
- **Why it matters**: A new user seeing `VFE Plan` in the picker with the description "Start here for planning and clarifying enterprise delivery work" gets no signal about the verification-first aspect. The differentiator from `flow Planner` or `epic Planner` is unclear.
- **Proposed change**: Add a requirement to the manifest that section 1 ("Family name and purpose") must include a 2-3 sentence plain-language explanation of what "verification-first" concretely means (specialist rounds, CoV, adjudication) and how it differs from simpler families.
- **Evidence**: Manifest Requirements section lists 15 sections but section 1 says only "Family name and purpose" with no depth requirement.
- **Confidence**: High.

### 2. GAP — No migration/adoption path documented for existing family users

- **Issue**: The plan says "Prefer existing families when they are a better fit" (the When To Use section) but doesn't require the manifest to include migration or transition guidance for teams already using `flow` or `epic` or `CoV` families.
- **Why it matters**: Discoverability is only half the battle. Users comparing families need to know when to switch, what they gain, and what ceremony they accept.
- **Proposed change**: Add a manifest requirement (or expand section 13 "Default engineering baseline") to include a short comparison table or decision matrix: `flow` vs `epic` vs `CoV` vs `vfe` — when each is best.
- **Evidence**: "When To Use This Family" section exists but only in PLAN.md, not required in the manifest. The manifest section list doesn't include a comparison or decision-aid section.
- **Confidence**: High.

### 3. MINOR — Entry-agent descriptions are functional but not differentiating

- **Issue**: The recommended descriptions ("Start here for planning and clarifying enterprise delivery work") are clear but generic. They don't mention the verification-first philosophy, specialist rounds, or working-directory durability — the key differentiators.
- **Why it matters**: In a picker with `flow Planner`, `epic Planner`, and `VFE Plan`, the description is the user's primary decision aid.
- **Proposed change**: Require descriptions to mention verification and specialist coverage, e.g., "Start here for verification-first planning with specialist review rounds and durable artifact tracking."
- **Evidence**: Frontmatter Policy → Recommended description pattern.
- **Confidence**: Medium.

### 4. MINOR — No changelog or release-notes requirement

- **Issue**: The plan doesn't mention how the family's introduction should be communicated (changelog entry, repo-level announcement, docs page).
- **Why it matters**: Without communication, the family may go unnoticed.
- **Proposed change**: Add a note in the handoff section or as a verification checklist item that the builder should add a brief entry to any relevant changelog or docs index.
- **Evidence**: No mention of changelog or announcement anywhere in PLAN.md.
- **Confidence**: Medium.
