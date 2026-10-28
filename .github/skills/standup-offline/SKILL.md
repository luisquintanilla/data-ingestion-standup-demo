---
name: standup-offline
description: Prepare or verify this Standup demo's native extraction, real MEDI chunks, offline contracts and local deck without Azure credentials or model calls. Use for offline smoke checks or a reproducible demo regression, not live model evaluation.
---

# Standup offline verification

Read `AGENTS.md` and the relevant source first. Execute only the steps needed
for the requested change; this is a bounded local workflow, not a hosted agent
fleet. Do not create a paid Codespace or install optional plugins.

1. Inspect Git status and `global.json`; preserve other work. In the launched
   process, set `STANDUP_PROVIDER=pdfpig` and **unset** `STANDUP_LIVE` and
   `STANDUP_BUDGET_PATH`. Do not inspect credentials or re-arm a real receipt.
   PowerShell: `$env:STANDUP_PROVIDER='pdfpig'; Remove-Item Env:\STANDUP_LIVE,Env:\STANDUP_BUDGET_PATH -ErrorAction SilentlyContinue`.
   Bash: `export STANDUP_PROVIDER=pdfpig; unset STANDUP_LIVE STANDUP_BUDGET_PATH`.
2. If the eight local S0 packages or dependencies are missing, run
   `pwsh -NoLogo -NoProfile -File scripts/bootstrap.ps1` from the root. It uses
   the same frozen-source preparation on both platforms, compiles the samples/
   hero, runs local contracts and builds the deck; it starts no servers.
   Stop on errors instead of upgrading the pinned SDK/API or retrying live.
3. For a prepared tree, use one runner for native smoke checks:

   ```text
   dotnet run --file checks/verify.cs
   dotnet run --file samples/01-extract.cs -- digital
   dotnet run --file samples/01-extract.cs --no-build -- scanned
   dotnet run --file samples/02-ingest.cs -- digital
   dotnet run --file samples/02-ingest.cs --no-build -- scanned
   ```

   Confirm actual digital 2-page/2-element extraction, 12 chunks with page
   association and 384-token maximum/48-token overlap, versus scanned 2-page/
   zero-element/zero-chunk output. Report actual assertion count from the
   runner. Synthetic CU payloads/vectors are local schema/mapping checks,
   never a live service or learned retrieval claim.
4. For deck changes run `node build.mjs`, then inspect an existing preview or
   start a bounded local preview only if needed. Preserve explicit stage
   metadata, notes and 25px code; check all changed slides at desktop/narrow
   widths, root/direct hashes, navigation, images and actual overflow. Do not
   claim the historical 168 checks verify a new revision. For web changes
   build `hero/hero.Web/hero.Web.csproj` and inspect the native/offline page; do not click live
   ingestion/chat actions.
5. Capture only sanitized diagnostics in ignored `evidence`/`.build` paths,
   not in historical `evidence/public`. Report commands, actual results and
   remaining unverified behavior. Stop only helpers you started; preserve
   another session's server. Never print tokens/account data, change real
   budgets or make a provider/model request to "finish" an offline check.
