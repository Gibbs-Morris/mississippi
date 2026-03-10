# Review 09: Developer Experience Reviewer

- Issue: Entry-agent naming should be obvious in both file names and display names. Why it matters: the user-facing picker experience matters more than internal file elegance. Proposed change: require display names like `VFE Plan`, `VFE Build`, and `VFE Review` even if the file names remain `vfe-entry-*.agent.md`. Evidence: inference from the human-entry-point requirement. Confidence: High.
- Issue: `argument-hint` should be used sparingly but intentionally on entry agents. Why it matters: good hints will reduce misuse, while missing or generic hints waste a supported VS Code affordance. Proposed change: include `argument-hint` on each entry agent with a one-line instruction tailored to that workflow. Evidence: VS Code docs document the field as a chat input hint and GitHub ignores it safely. Confidence: High.
- Issue: The manifest should include one or two example prompts for each entry agent. Why it matters: this reduces onboarding friction and makes the three entry points self-explanatory. Proposed change: add short `Example asks` under each Start here entry in the manifest. Evidence: inference from current repo lacking an agent index and the user's desire for clear human entry points. Confidence: Medium.

## CoV

- Claim: naming and hints are material to usability here. Evidence: only three user entry points exist, so each must be unmistakable. Confidence: High.