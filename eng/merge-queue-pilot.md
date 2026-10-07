# Temporary merge queue pilot

This document belongs to the isolated merge queue investigation tracked in
Gibbs-Morris/mississippi#1030. Its target branch is
`codex/merge-queue/pilot-20261007`; it is not a production change.

The preceding pull request changes only C# documentation. This follower changes
only Markdown. Together they let us observe whether the follower's queue
candidate contains the preceding C# change and runs the required cleanup checks.

The pilot retains every production rule and required check provider. An extra
`Merge Queue Pilot Hold` check prevents candidates from merging during inspection.
The hold must never report success on a queue candidate. Record the event,
checkout, base and candidate SHAs alongside the queue entries before drawing
conclusions about candidate membership.

After preserving the evidence, dequeue and close the temporary probe pull
requests, remove their isolated ruleset, and remove only their owned branches.
Production queue activation and delivery-stack merging remain separate decisions.
