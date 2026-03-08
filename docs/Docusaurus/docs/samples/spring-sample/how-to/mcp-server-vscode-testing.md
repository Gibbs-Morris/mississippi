---
id: spring-mcp-server-vscode-testing
title: Test Spring MCP Server with VS Code
sidebar_label: MCP in VS Code
sidebar_position: 7
description: Configure VS Code to connect to Spring.Gateway as an MCP server and validate generated MCP tools.
---

# Test Spring MCP Server with VS Code

## Overview

This page shows how to connect VS Code to the Spring sample MCP server so you can test source-generated MCP tools (for example, `open_account`, `deposit_funds`, and projection read tools) from chat.

Spring exposes MCP in `Spring.Gateway` with:

- `builder.Services.AddMcpServer().WithHttpTransport().WithGeneratedMcpTools().WithTools<SpringGatewayPingMcpTools>();`
- `app.MapMcp("/mcp");` (mapped in Development environment only)

Reference: [Spring.Gateway Program.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Gateway/Program.cs)

## Before You Begin

- VS Code with GitHub Copilot Chat enabled
- This repository checked out locally
- .NET SDK installed

## Step 1: Start Spring Locally

From the repository root, run:

```powershell
pwsh ./run-spring.ps1
```

This script builds `samples.slnx` and starts `samples/Spring/Spring.AppHost/Spring.AppHost.csproj`, which launches the Spring services for local development.

When Spring is running from `run-spring.ps1`, Spring uses these default endpoints from launch settings:

- `Spring.Gateway` HTTP: `http://localhost:5101`
- `Spring.Gateway` HTTPS: `https://localhost:7101`
- `Spring.Runtime` HTTP health: `http://localhost:5300/health`
- Aspire dashboard: `https://localhost:17272`

For MCP over HTTP, use `http://localhost:5101/mcp`.

## Step 2: Add the MCP Server to VS Code

Open (or create) `.vscode/mcp.json` in the workspace root and add a server entry:

```json
{
  "servers": {
    "springBank": {
      "type": "http",
      "url": "http://localhost:5101/mcp"
    }
  }
}
```

Notes:

- Keep the server name in camelCase (`springBank`).
- If you change launch settings, update this URL to match your `Spring.Gateway` HTTP port.
- MCP client behavior and UI workflows can change across VS Code releases; use the official MCP documentation linked in [References](#references).

## Verify

1. Open Chat in VS Code.
1. Open the tools picker and ensure tools from `springBank` are enabled.
1. Ask a prompt that should use Spring tools, for example:

```text
Use the springBank tools to open a bank account for Ada Lovelace with an initial deposit of 100.
```

1. Ask a follow-up read prompt, for example:

```text
Use the springBank tools to fetch the balance projection for that account.
```

1. Ask a quick connectivity prompt for the manually registered ping tool:

```text
Use the springBank tools to call spring_gateway_ping.
```

## Troubleshooting

- **Server not listed or not starting**: verify `.vscode/mcp.json` is valid JSON and the URL is reachable.
- **No tools discovered**: confirm Spring is running and that `/mcp` is mapped in `Spring.Gateway`.
- **Tools mismatch after code changes**: restart Spring with `pwsh ./run-spring.ps1` so regenerated tools are loaded.

## Summary

Spring exposes MCP tools at `http://localhost:5101/mcp` in Development, with both generated domain tools and a manual `spring_gateway_ping` tool.

## Next Steps

- Run through [Building an Aggregate](../tutorials/building-an-aggregate.md) and test generated command tools over MCP.
- Run through [Building Projections](../tutorials/building-projections.md) and test projection read tools over MCP.
- Extend your local MCP prompts using the Spring tool names validated in this page.
- [Host Architecture](../concepts/host-applications.md) - Revisit how the MCP surface fits into the Spring gateway host.
- [Key Concepts](../concepts/key-concepts.md) - Revisit the domain-modeling vocabulary behind the generated tools.
- [Domain Registration Generators](../../../archived/reference/domain-registration-generators.md) - Look up how the host registration surface is generated.

## References

- [Use MCP servers in VS Code](https://code.visualstudio.com/docs/copilot/customization/mcp-servers)
- [Spring.Gateway Program.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Gateway/Program.cs)
- [run-spring.ps1](https://github.com/Gibbs-Morris/mississippi/blob/main/run-spring.ps1)
