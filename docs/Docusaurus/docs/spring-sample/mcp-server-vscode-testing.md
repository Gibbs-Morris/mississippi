---
id: spring-mcp-server-vscode-testing
title: Test Spring MCP Server with VS Code
sidebar_label: MCP in VS Code
sidebar_position: 7
description: Configure VS Code to connect to Spring.Server as an MCP server and validate generated MCP tools.
---

# Test Spring MCP Server with VS Code

## Overview

Focus: Public API / Developer Experience.

This page shows how to connect VS Code to the Spring sample MCP server so you can test source-generated MCP tools (for example, `open_account`, `deposit_funds`, and projection read tools) from chat.

Spring exposes MCP in `Spring.Server` with:

- `builder.Services.AddMcpServer().WithHttpTransport().WithGeneratedMcpTools().WithTools<SpringServerPingMcpTools>();`
- `app.MapMcp("/mcp");` (mapped in Development environment only)

Reference: [Spring.Server Program.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Server/Program.cs)

## Prerequisites

- VS Code with GitHub Copilot Chat enabled
- This repository checked out locally
- .NET SDK installed

## Start Spring Locally

From the repository root, run:

```powershell
pwsh ./run-spring.ps1
```

This script builds `samples.slnx` and starts `samples/Spring/Spring.AppHost/Spring.AppHost.csproj`, which launches the Spring services for local development.

When Spring is running from `run-spring.ps1`, Spring uses these default endpoints from launch settings:

- `Spring.Server` HTTP: `http://localhost:5101`
- `Spring.Server` HTTPS: `https://localhost:7101`
- `Spring.Silo` HTTP health: `http://localhost:5300/health`
- Aspire dashboard: `https://localhost:17272`

For MCP over HTTP, use `http://localhost:5101/mcp`.

## Add the MCP Server to VS Code

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
- If you change launch settings, update this URL to match your `Spring.Server` HTTP port.
- MCP client behavior and UI workflows can change across VS Code releases; use the official MCP documentation linked in [References](#references).

## Validate Tool Discovery in Chat

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
Use the springBank tools to call spring_server_ping.
```

## Troubleshooting

- **Server not listed or not starting**: verify `.vscode/mcp.json` is valid JSON and the URL is reachable.
- **No tools discovered**: confirm Spring is running and that `/mcp` is mapped in `Spring.Server`.
- **Tools mismatch after code changes**: restart Spring with `pwsh ./run-spring.ps1` so regenerated tools are loaded.

## See Also

- [Host Applications](./host-applications.md)
- [Key Concepts](./key-concepts.md)
- [Domain Registration Generators](../domain-registration-generators.md)

## Summary

Spring exposes MCP tools at `http://localhost:5101/mcp` in Development, with both generated domain tools and a manual `spring_server_ping` tool.

## Next Steps

- Run through [Building an Aggregate](./building-an-aggregate.md) and test generated command tools over MCP.
- Run through [Building Projections](./building-projections.md) and test projection read tools over MCP.
- Extend your local MCP prompts using the Spring tool names validated in this page.

## References

- [Use MCP servers in VS Code](https://code.visualstudio.com/docs/copilot/customization/mcp-servers)
- [Spring.Server Program.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Server/Program.cs)
- [run-spring.ps1](https://github.com/Gibbs-Morris/mississippi/blob/main/run-spring.ps1)
