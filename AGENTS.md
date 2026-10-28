# Working on this demo

This is a one-off .NET/Reveal demonstration, not a shipping API or general AI
platform. Read the relevant source before changing it; do not infer experimental
APIs from current upstream documentation.

## Sources of truth

- `global.json`: exactly .NET SDK **10.0.403**, no roll-forward.
- `.devcontainer`: Ubuntu SDK image and Node **24.22.1**, with PowerShell 7.
- `vendor/NOTICE.md`, `vendor/s0-provenance.json`, `vendor/source-manifest.json`:
  immutable composite S0 archive, exact Documents overlay, eight unofficial
  **10.8.0-standup.s0.0ff84e7.1** packages. The baseline commit alone is not S0.
  Do not edit vendor inputs or replace them with a sibling checkout/newer API.
- Lock files pin public dependencies. Do not upgrade SDKs, packages, models or
  architecture as a side effect of another task.
  Preparation derives ignored `.build/locks` copies, updating only the eight
  locally rebuilt S0 content hashes; committed lock templates stay unchanged.
  NuGet uses this workspace's `.build/packages`, not a shared S0 package cache.

## Map and small offline commands

- `samples/01-extract.cs`, `02-ingest.cs`, `03-retrieve.cs`: standalone .NET 10
  file apps. Use `dotnet run --file`, not an invented test/project command.
- `src/Standup.Core`: demo-local PdfPig/CU adapters, actual MEDI/source bridge,
  vector schema, index identity and mandatory cumulative live guards.
- `hero`: Chat-template payoff; direct `hero.Web` needs no Docker/Aspire server.
- `slides.md`, `index.html`, `css`, `themes`, `assets`: editable deck.
  `public`, `.build`, bin/obj, local packages/indexes and bootstrap logs are
  generated. Do not edit them as source or commit them.
- `evidence/public`: attributed historical recordings, not fresh test output.

Fresh setup on Windows PowerShell 7 or Linux:

```text
pwsh -NoLogo -NoProfile -File scripts/bootstrap.ps1
```

For already-prepared changes, unset `STANDUP_LIVE` and `STANDUP_BUDGET_PATH` in
the launched process, set `STANDUP_PROVIDER=pdfpig`, then choose the smallest
relevant commands (POSIX separators also work in PowerShell):

```text
dotnet run --file checks/verify.cs
dotnet run --file samples/01-extract.cs -- digital
dotnet run --file samples/02-ingest.cs -- digital
dotnet build hero/hero.Web/hero.Web.csproj -v:q
node build.mjs
```

Digital: 2 pages, 2 native elements, 12 chunks at 384/48 tokens. Scanned: 2 pages,
0 native text/chunks; PdfPig is not OCR. The check app exercises real local
types/SQLite plus explicitly synthetic CU payloads/vectors: **not** `dotnet test`
and not a learned retrieval proof. Build sequentially when outputs overlap.

## Deck/evidence discipline

Every slide explicitly declares `data-workflow`, `data-stages`, `data-context`;
overview stages use literal `none`. Preserve 25px logical code and the separate
orientation rail. Build, then inspect actual rendered slides, notes, image/text/
pre overflow and root/direct-link/navigation/reload behavior at desktop and
narrow widths; static generation alone is not rendered validation. Existing
42-slide evidence lives in `evidence/public/workflow-rail`. Do not relabel old
screenshots as verification of changed slides. Preserve native/CU context,
page-1 versus Table-2 provenance, and optional invoice/not-VLM caveats.

## Safety and scope

Default work is offline. No Azure token/login, CU/model calls, provisioning,
live indexing/chat, private budget changes or paid Codespace creation without
separate explicit authorization and its cumulative limits. Never bypass guards,
refund reservations, invent fallback successes or synthesize "live" results.
Synthetic accounting checks must use only their temporary synthetic receipt.

Never commit credentials, real endpoints/identity IDs, private receipts/raw
errors, personal paths, local indexes or unreviewed screenshots. Preserve
ignored private work. Do not stop someone else's preview; ports 8187/5187 are
local/private. No push/publication or destructive Git action unless requested.
Optional agent tooling is documented in `docs/AI-AGENTS.md`, not auto-installed.
