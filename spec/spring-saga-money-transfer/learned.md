# Learned

## Repository Orientation

- The Spring sample uses a four-project layout (AppHost, Client, Domain, Server, Silo) under samples/Spring/. (VERIFIED: samples/Spring/)
- Spring.Domain contains a BankAccount aggregate with commands for open/deposit/withdraw and generated endpoints. (VERIFIED: samples/Spring/Spring.Domain/Aggregates/BankAccount/)
- Spring.Domain includes a TransactionInvestigationQueue aggregate, indicating existing cross-aggregate workflows. (VERIFIED: samples/Spring/Spring.Domain/Aggregates/TransactionInvestigationQueue/)
- The sample uses Mississippi generators and Orleans serialization attributes throughout the domain aggregate types. (VERIFIED: samples/Spring/Spring.Domain/Aggregates/BankAccount/BankAccountAggregate.cs)
- Spring.Client Index page drives open/deposit/withdraw flows and uses projections for balance and ledger; no transfer UI exists yet. (VERIFIED: samples/Spring/Spring.Client/Pages/Index.razor, samples/Spring/Spring.Client/Pages/Index.razor.cs)
- Spring.Silo registers aggregates and projections via generated Add{Aggregate} and Add{Projection} extension methods in Program.cs. (VERIFIED: samples/Spring/Spring.Silo/Program.cs)
- Spring.Server registers projection mappers and aggregate mappers via generated extension methods in Program.cs. (VERIFIED: samples/Spring/Spring.Server/Program.cs)
- No GenerateSagaEndpoints attribute usage exists under samples/Spring, indicating no saga sample yet. (VERIFIED: grep for GenerateSagaEndpoints in samples/)

## Open Questions

- Existing Spring.Domain.L0Tests coverage and patterns to extend for saga coverage. (UNVERIFIED)
- Existing saga infrastructure usage in samples, if any. (UNVERIFIED)
- Coverage gaps across src projects to reach 100% unit test coverage. (UNVERIFIED)
- Current Spring.Domain.L0Tests coverage and patterns to extend for saga coverage. (UNVERIFIED)
- Existing saga infrastructure usage in samples, if any. (UNVERIFIED)
