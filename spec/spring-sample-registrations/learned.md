# Learned

- Spring sample uses separate projects: Spring.Client, Spring.Server, Spring.Silo, Spring.Domain. Evidence: samples/Spring/*.csproj.
- Spring programs manually call generated registration methods (AddBankAccountAggregate, AddMoneyTransferSagaFeature, Add*ProjectionMappers). Evidence: samples/Spring/Spring.*/*Program.cs.
- Inlet generators are referenced as analyzers in Spring.Client/Server/Silo and emit registration methods for aggregates, projections, and sagas. Evidence: samples/Spring/Spring.*/*.csproj and src/Inlet.*.Generators/*.cs.
- Silo registration namespace derives from NamingConventions.GetSiloRegistrationNamespace and targets .Silo.Registrations. Evidence: src/Inlet.Generators.Core/Naming/NamingConventions.cs.
- Pending source gen attribute exists at Mississippi.Inlet.Generators.Abstractions.PendingSourceGeneratorAttribute with no usages found. Evidence: src/Inlet.Generators.Abstractions/PendingSourceGeneratorAttribute.cs.
- Crescent.L2Tests CounterRegistrations is an example of a multi-registration helper in samples. Evidence: samples/Crescent/Crescent.L2Tests/Domain/Counter/CounterRegistrations.cs.
