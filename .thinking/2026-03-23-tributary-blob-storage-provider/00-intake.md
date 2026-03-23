# Intake

## User Request

We need a new provider building just like `Tributary.Runtime.Storage.Cosmos` but this time for azure blob storage.
it must support compression  on the file gzip to start with I think, and we need to be able to select how we serilizxe the data default shoul dbe json - but this shoul dbe pluiggable in itself a bit like the cosmos one. - optios pattern to confuigre everything.

this is because comsos has a limit to the size of file you can store of around 4mb and we want to enable bigger ones on the whole for some cases
so we need to build out  new provider whic supports bigger ones

this should follow the same contracts as Tributary.Runtime.Storage.Cosmos, have full unit testing.

and add L2 tests in Crescent for this provider if posisble too Crescent is where we put our end to end tests.

## Initial Product Owner Analysis

- Outcome sought: a new Tributary storage provider backed by Azure Blob Storage, shaped like the Cosmos provider but optimized for larger persisted payloads.
- Likely capability areas: storage provider implementation, compression pipeline, serializer extensibility, options model, unit tests, and possible Crescent L2 integration coverage.
- Key requirement gaps still open: exact Azure Blob usage model, compatibility expectations with existing contracts and configuration patterns, compression scope and future extensibility, serializer plug-in boundaries, and the level of L2 fidelity expected in Crescent.
- Discovery focus: business value, exact scope boundaries, consumer configuration model, non-functional expectations, and test/deployment constraints.