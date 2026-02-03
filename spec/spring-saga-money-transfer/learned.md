# Learned

## Repository Orientation

- The Spring sample uses a four-project layout (AppHost, Client, Domain, Server, Silo) under samples/Spring/. (VERIFIED: samples/Spring/)
- Spring.Domain contains a BankAccount aggregate with commands for open/deposit/withdraw and generated endpoints. (VERIFIED: samples/Spring/Spring.Domain/Aggregates/BankAccount/)
- Spring.Domain includes a TransactionInvestigationQueue aggregate, indicating existing cross-aggregate workflows. (VERIFIED: samples/Spring/Spring.Domain/Aggregates/TransactionInvestigationQueue/)
- The sample uses Mississippi generators and Orleans serialization attributes throughout the domain aggregate types. (VERIFIED: samples/Spring/Spring.Domain/Aggregates/BankAccount/BankAccountAggregate.cs)

## Open Questions

- Existing Spring.Client UI flow for money movement and whether it already has multi-account transfer UX. (UNVERIFIED)
- Current Spring.Domain.L0Tests coverage and patterns to extend for saga coverage. (UNVERIFIED)
- Existing saga infrastructure usage in samples, if any. (UNVERIFIED)
