# Data ingestion Standup demo

One-off preparation for the .NET AI Community Standup on data ingestion:
small .NET 10 file-based apps, followed by a composed Chat-template application.

The demo uses a pinned, unofficial API-review prototype. It is not a statement
that the document extraction APIs have shipped or completed review.

The recorded final `.1` live rehearsal succeeded: real native/CU extraction,
1536-dimensional embeddings and VectorData retrieval, then one grounded
Chat-template answer per provider. The 42-slide deck includes labeled actual
outputs. Both answers return 35,511 BCFG; page-1 prose also supports the value,
so this is not proof of table-grid understanding.

**Offline by default.** Native extraction, chunking and deck preview need no
Azure credentials. A clone contains no live endpoints, credentials, ready
indexes or authorized budget ledger. Live operations fail closed unless you
explicitly enable them and supply your own separately authorized private
configuration. The original rehearsal ledger remains private and halted.

Start with [RUNBOOK.md](RUNBOOK.md) for preparation, the three exact file-app
commands, app/deck preview, reset, and labeled fallback evidence. See
[recorded evidence](evidence/public/README.md) for actual results and limitations,
[vendor provenance](vendor/NOTICE.md) for the frozen composite source identity,
and [third-party notices](THIRD-PARTY-NOTICES.md) for redistribution licenses.

```powershell
# From this repository's root, in PowerShell:
$env:STANDUP_PROVIDER = 'pdfpig'
Remove-Item Env:\STANDUP_LIVE -ErrorAction SilentlyContinue
Remove-Item Env:\STANDUP_BUDGET_PATH -ErrorAction SilentlyContinue
.\scripts\prepare.ps1
dotnet run --file samples\01-extract.cs -- digital
dotnet run --file samples\02-ingest.cs -- digital
```

Requires .NET SDK **10.0.403**; preparation restores public NuGet dependencies
and builds the eight pinned unofficial packages locally. To preview the deck,
also install Node.js, then run `npm ci --ignore-scripts`, `node.exe build.mjs`
and `node.exe serve.mjs`. The preview binds to loopback, not a public interface.
No GitHub Pages site, release, npm package or NuGet publication is configured.
