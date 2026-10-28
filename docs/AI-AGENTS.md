# AI agents: small, repository-aware defaults

Start with [AGENTS.md](../AGENTS.md), the actual source and the pinned S0
receipts. That context is more useful here than installing a large collection
of generic instructions. The optional [dev container](../.devcontainer/devcontainer.json)
installs shared build tools, **not agent plugins**, accounts or Azure resources.

## What is already in this repository

- Portable root `AGENTS.md`: scope, source map, exact API/SDK identities,
  small offline commands, generated paths, presentation/evidence constraints.
- `.github/copilot-instructions.md`: a short Copilot entry point referencing
  that same source of truth, rather than duplicating a second policy manual.
- `.github/skills/standup-offline/SKILL.md`: one first-party repeatable workflow
  for offline preparation, native/SQLite checks and honest rendered evidence.
  It does not run models, provision a Codespace or spawn hosted workflows.

Project skill support is host-dependent. GitHub documents project skills
including `.github/skills`; compatible hosts may use `.agents/skills` or
`.claude/skills`. Do not duplicate this skill into every folder by default.
If your host does not discover it, read and execute its steps explicitly.
See [GitHub's agent-skills documentation](https://docs.github.com/en/copilot/concepts/agents/about-agent-skills).
`SKILL.md`, Copilot `.agent.md` handoffs and other hosts' native agents are
different formats, not interchangeable installation instructions.

## Optional upstream tooling -- select, don't auto-install

Reviewed upstream revisions are linked below so recommendations are
reproducible, not guesses from package names. Use each repository's current
host-specific acquisition instructions; don't paste a plugin command intended
for another host or edit global settings on somebody else's behalf.

| Source | Useful scope here | Boundaries |
|---|---|---|
| [dotnet/skills foundation `dotnet` plugin](https://github.com/dotnet/skills/tree/586f13f8d898a60583a3364f146a3b6f5a763164) | `csharp-expert`, `csharp-refactoring`, `msbuild`; `setup-local-sdk` if the exact required SDK is missing | Its setup examples can target .NET 11/preview. Override that default with **10.0.403**, stable, no roll-forward. Host-specific LSP setup is optional. |
| [dotnet/skills test tooling](https://github.com/dotnet/skills/tree/586f13f8d898a60583a3364f146a3b6f5a763164) | Optional `dotnet-test` / `run-tests` when maintaining an actual framework test suite | This repo's `checks/verify.cs` is a file app. Run it with `dotnet run --file`, not `dotnet test`; don't scaffold a new framework just to rename it. |
| [Matt Pocock: `tdd`](https://github.com/mattpocock/skills/tree/59db609eb1e18aaf59b63284d0e52255aff0c63c) | Public-behavior seams and test-first vertical slices, when the user has agreed on the test seams | Optional development workflow, not a mandatory ritual for every docs edit. Do not import TypeScript/React tooling or examples into this C#/Reveal demo. |
| [Matt Pocock: `diagnosing-bugs`](https://github.com/mattpocock/skills/tree/59db609eb1e18aaf59b63284d0e52255aff0c63c) | Deterministic CLI/browser reproductions that can fail before the fix; redacted diagnostics | A local repro is not permission for token/model calls, private telemetry or an expensive hosted workflow. |

These recommendations were informed by the individual `csharp-expert`,
`setup-local-sdk`, `tdd` and `diagnosing-bugs` skill instructions, not just
catalog descriptions. No third-party skill trees are copied here.

Do **not** default-install upgrade/.NET 11, EF, MAUI or broad platform plugins.
`dotnet-ai` technology-selection guidance is not a reason to replace the
existing MEAI/MEDI architecture or frozen prototype APIs. No global plugin
configuration, personal preferences, CLI authentication or hosted-agent
automation is performed by repository setup.

## Keep verification honest

The deck's recorded native/CU/model outputs have provenance and caveats.
New native runs need no secrets; they do not prove new live CU/model behavior.
Preserve the distinction between synthetic contract checks, historical
recordings and fresh execution. Never use an agent's confident API guess in
place of inspecting the frozen source, and never enable live calls to work
around an offline failure.
