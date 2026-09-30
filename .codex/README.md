# Optional Astra and Luna routing

This directory contains an optional CLI profile recipe. The example file is
not loaded automatically as repository configuration.

Use a client whose installed help supports file-based profiles and whose
configuration reference supports the `[agents]` fields in the example. Older
clients can keep using the repository without loading this profile. Current
field definitions are in the [Codex configuration reference](https://learn.chatgpt.com/docs/config-file/config-reference).

## Check support before selection

Inspect the installed client's version, help and configuration reference. Check
its current model catalog and account availability for both `gpt-6-astra` with
`xhigh` reasoning and `gpt-5.6-luna` with `max` reasoning. A bundled catalog lists
known identifiers; it does not prove account access or successful execution.

If either model, reasoning setting or agent field is unsupported, leave the
profile unselected and report the limitation. An explicit request for these
settings does not authorize a different model or reasoning level.

After selection, inspect effective primary and worker settings. Custom role
settings can override explicit spawn settings, which override agent defaults;
defaults override inherited parent settings. Current precedence is documented
under [Codex subagents](https://learn.chatgpt.com/docs/agent-configuration/subagents).
Do not claim the configured 16-worker ceiling is active without observing the
host's available slots and actual execution.

## Select the profile

After completing those checks, copy
`astra-luna.config.example.toml` to your configured Codex home as
`astra-luna.config.toml`. Select it for a new CLI session with:

```text
codex --profile astra-luna
```

This repository example does not change existing desktop chats or workers.
Other hosts require their own supported model selection and effective-setting
checks. The profile's concurrency value is a ceiling; available host slots and
resource pressure can require a smaller worker set.
