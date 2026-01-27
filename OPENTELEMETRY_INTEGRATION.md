# OpenTelemetry Integration in Spring Sample

This document describes the OpenTelemetry integration for the Spring sample application.

## Overview

The Spring sample uses .NET Aspire's OpenTelemetry integration to provide comprehensive observability with logging, metrics, and tracing across all backend components.

## Architecture

### Spring.Silo (Orleans Server)
**Location**: `samples/Spring/Spring.Silo/Program.cs`

Configured with:
- **Tracing**:
  - ASP.NET Core instrumentation (incoming HTTP requests)
  - HttpClient instrumentation (outgoing HTTP calls)
  - Orleans Runtime and Application activity sources
  
- **Metrics**:
  - ASP.NET Core instrumentation (HTTP server metrics)
  - HttpClient instrumentation (HTTP client metrics)
  - Runtime instrumentation (.NET runtime metrics)
  - Mississippi framework meters:
    - `Mississippi.EventSourcing.Brooks`
    - `Mississippi.EventSourcing.Aggregates`
    - `Mississippi.EventSourcing.Snapshots`
    - `Mississippi.Storage.Cosmos`
    - `Mississippi.Storage.Snapshots`
    - `Mississippi.Storage.Locking`
  - Orleans meter (`Microsoft.Orleans`)

- **Logging**: Integrated via `.WithLogging()`

- **Export**: OTLP exporter via `.UseOtlpExporter()` to Aspire dashboard

### Spring.Server (API Server / Orleans Client)
**Location**: `samples/Spring/Spring.Server/Program.cs`

Configured with:
- **Tracing**:
  - ASP.NET Core instrumentation (incoming HTTP requests)
  - HttpClient instrumentation (outgoing HTTP calls to Orleans)
  - Orleans Runtime and Application activity sources

- **Metrics**:
  - ASP.NET Core instrumentation (HTTP server metrics)
  - HttpClient instrumentation (HTTP client metrics)
  - Runtime instrumentation (.NET runtime metrics)
  - Orleans meter (`Microsoft.Orleans`)

- **Logging**: Integrated via `.WithLogging()`

- **Export**: OTLP exporter via `.UseOtlpExporter()` to Aspire dashboard

### Spring.Client (Blazor WebAssembly)
**Location**: `samples/Spring/Spring.Client/Program.cs`

**Note**: Spring.Client is a Blazor WebAssembly application that runs in the browser. Traditional OpenTelemetry OTLP exporters over HTTP/2 are not supported in browser WebAssembly environments due to browser sandboxing limitations. While browser-specific telemetry could be collected using OpenTelemetry's Web SDK or specialized browser instrumentation libraries, the current implementation relies on server-side telemetry in Spring.Server, which captures all API calls initiated by the client, providing comprehensive observability for client-server interactions.

## Aspire Integration

The Spring.AppHost (`samples/Spring/Spring.AppHost/Program.cs`) orchestrates the services and automatically configures OpenTelemetry via environment variables:

- `OTEL_EXPORTER_OTLP_ENDPOINT`: Aspire dashboard OTLP endpoint
- `OTEL_SERVICE_NAME`: Service name for telemetry identification
- `OTEL_RESOURCE_ATTRIBUTES`: Additional resource attributes

The `.UseOtlpExporter()` call in each service automatically reads these environment variables, requiring no manual endpoint configuration.

## Dependencies

All required OpenTelemetry packages are referenced in the project files:

**Spring.Silo.csproj** and **Spring.Server.csproj**:
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` - OTLP exporter
- `OpenTelemetry.Extensions.Hosting` - ASP.NET Core integration
- `OpenTelemetry.Instrumentation.AspNetCore` - ASP.NET Core telemetry
- `OpenTelemetry.Instrumentation.Http` - HttpClient telemetry
- `OpenTelemetry.Instrumentation.Runtime` - .NET runtime telemetry

## Viewing Telemetry

When running the Spring application via the AppHost:

1. Start the application: `dotnet run --project samples/Spring/Spring.AppHost`
2. The Aspire dashboard will automatically open
3. Navigate to the Traces, Metrics, or Logs sections to view telemetry

## Best Practices

The implementation follows Aspire OpenTelemetry best practices:

1. **Minimal Configuration**: Uses `.UseOtlpExporter()` without hardcoded endpoints
2. **Environment-Based Config**: Relies on Aspire-injected environment variables
3. **Comprehensive Coverage**: Includes all three signals (traces, metrics, logs)
4. **Framework Integration**: Includes both ASP.NET Core and framework-specific meters
5. **Activity Propagation**: Orleans configured with `.AddActivityPropagation()` for distributed tracing

## Validation

The OpenTelemetry integration can be validated in multiple ways:

### Local Development (Full Interactive Experience)
```powershell
pwsh ./run-spring.ps1
```
Requires Docker Desktop or Podman. Provides full Aspire Dashboard with real-time telemetry visualization.

### Automated Testing
```bash
dotnet test samples/Spring/Spring.L2Tests --configuration Release
```
L2 integration tests validate the complete stack including telemetry export without requiring interactive dashboard access.

**Note**: In CI environments without full DCP (Developer Control Plane) support, L2 tests may fail to start the orchestrator but the underlying OTel configuration remains valid. See `samples/Spring/CI_LIMITATIONS.md` for details.

## Troubleshooting

If telemetry is not appearing in the Aspire dashboard:

1. Verify the AppHost is running and the dashboard is accessible
2. Check that environment variables are set (visible in dashboard's Environment tab)
3. Ensure all services are referenced in the AppHost via `.WithReference()`
4. Check service logs for OpenTelemetry initialization errors
5. **CI/CD environments**: See [CI Limitations](samples/Spring/CI_LIMITATIONS.md) for known issues with DCP availability

## References

- [.NET Aspire Telemetry](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
- [CI Limitations for Spring Sample](samples/Spring/CI_LIMITATIONS.md)
