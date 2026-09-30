# Optional Astra and Luna routing

This directory contains an optional CLI profile recipe. The example file is
not loaded automatically as repository configuration.

Use a client whose installed help supports file-based profiles and whose
configuration reference supports the `[agents]` fields in the example. Older
clients can keep using the repository without loading this profile. Current
field definitions are in the [Codex configuration reference](https://learn.chatgpt.com/docs/config-file/config-reference).

After checking the client and requested models, copy
`astra-luna.config.example.toml` to your configured Codex home as
`astra-luna.config.toml`. Select it for a new CLI session with:

```text
codex --profile astra-luna
```

This repository example does not change existing desktop chats or workers.
Other hosts require their own supported model selection and effective-setting
checks. The profile's concurrency value is a ceiling; available host slots and
resource pressure can require a smaller worker set.
