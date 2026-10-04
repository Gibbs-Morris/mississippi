---
applyTo: '**/Aspire*/**/*.cs'
---

# Aspire Integration Testing

Governing thought: Use the Linux vNext Cosmos emulator with HTTP mode and SDK workarounds to avoid known connectivity issues.

> Drift check: Check [Aspire Cosmos issues](https://github.com/dotnet/aspire/issues?q=cosmos+emulator) and [Cosmos SDK issues](https://github.com/Azure/azure-cosmos-dotnet-v3/issues) for updates before changing emulator configuration.

## Rules (RFC 2119)

- Cosmos emulator **MUST** use `RunAsEmulator()` with `WithoutHttpsCertificate()` on Aspire 13.6 or later. Why: `RunAsEmulator()` selects the Linux vNext emulator with an HTTP `/ready` endpoint; `RunAsPreviewEmulator()` is obsolete. See [Aspire 13.6 breaking changes](https://aspire.dev/whats-new/aspire-13-6/#breaking-changes).
- `CosmosClientOptions` **MUST** set `LimitToEndpoint = true` when connecting to any emulator. Why: Keeps requests on the configured emulator endpoint instead of discovering other regions. See [the SDK property reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.cosmosclientoptions.limittoendpoint).
- `CosmosClientOptions` **SHOULD** use `ConnectionMode.Gateway` for emulator connections. Why: Gateway mode is more reliable than Direct TCP for local emulators.
- Cosmos document models **MUST** use `[Newtonsoft.Json.JsonProperty("id")]` not `System.Text.Json` attributes. Why: Cosmos SDK v3 uses Newtonsoft.Json by default; STJ attributes are ignored.
- Aspire test projects **SHOULD** use `IAsyncLifetime` fixture pattern to manage AppHost lifecycle. Why: Ensures proper startup/teardown and resource cleanup.

## Scope and Audience

Developers building Aspire-based integration tests with Azure emulators.

## At-a-Glance Quick-Start

AppHost configuration:

```csharp
builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator(emulator =>
    {
        emulator.WithDataExplorer();
#pragma warning disable ASPIRECERTIFICATES001
        emulator.WithoutHttpsCertificate(); // HTTP mode
#pragma warning restore ASPIRECERTIFICATES001
    });
```

`WithDataExplorer()` remains explicit because Data Explorer is opt-in. Aspire 13.6
retires `ASPIRECOSMOSDB001`; the separate certificate API diagnostic remains
scoped to `WithoutHttpsCertificate()` in the sample hosts.

SDK client configuration:

```csharp
CosmosClientOptions options = new()
{
    ConnectionMode = ConnectionMode.Gateway,
    LimitToEndpoint = true, // Use only the configured emulator endpoint
};
```

Document model:

```csharp
public class MyDocument
{
    [Newtonsoft.Json.JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
}
```

## Known Issues Reference

| Issue | Symptom | Fix |
|-------|---------|-----|
| [SDK endpoint discovery](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.cosmosclientoptions.limittoendpoint) | Client can discover other regions | `LimitToEndpoint = true` |
| [Aspire 13.6 migration](https://aspire.dev/whats-new/aspire-13-6/#breaking-changes) | `RunAsPreviewEmulator()` is obsolete | Use `RunAsEmulator()` for vNext |
| Newtonsoft vs STJ | "Document does not contain id" | Use `Newtonsoft.Json.JsonProperty` |

## Core Principles

- Linux vNext emulator with its HTTP readiness probe
- HTTP mode eliminates certificate complexity
- SDK needs explicit single-endpoint mode for emulators

## References

- [Aspire 13.6 Cosmos integration source](https://github.com/microsoft/aspire/blob/v13.6.0/src/Aspire.Hosting.Azure.CosmosDB/AzureCosmosDBExtensions.cs)
- Sample implementations: `samples/Crescent/Crescent.AppHost/`, `samples/Crescent/Crescent.L2Tests/`, and `samples/Spring/Spring.AppHost/`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Testing guidance: `.github/instructions/testing.instructions.md`
