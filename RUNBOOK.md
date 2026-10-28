# Run the Standup demo

This is a PowerShell 7-based, unofficial API-review prototype, not a
shipping API announcement. Source, fixtures and local build inputs are pinned;
no sibling checkout, private feed or published prototype package is needed.
Preparation restores public dependencies; it makes no Azure model/CU requests.

## Codespaces / Linux quick start

The standard `.devcontainer` uses Ubuntu 24.04 with .NET SDK **10.0.403**,
Node **24.22.1**, PowerShell 7, Git and curl. Its post-create command runs the
same shared bootstrap used locally:

```text
pwsh -NoLogo -NoProfile -File scripts/bootstrap.ps1
```

It prepares the eight frozen libraries, builds the direct web app and three
file apps, runs local contracts, restores Reveal from its lock and builds the
deck. It starts **no server**, asks for no Azure credentials, and performs no
live indexing/chat/CU. The optional Aspire apphost is not needed for this path.
Initial setup needs internet access to public images/packages and can take
several minutes. Creating/running a Codespace uses the visitor's GitHub
allowance/billing; repository setup does not create one automatically.

After setup, from the repository root in a Bash terminal:

```bash
unset STANDUP_LIVE STANDUP_BUDGET_PATH
export STANDUP_PROVIDER=pdfpig
dotnet run --file samples/01-extract.cs --no-build -- digital
dotnet run --file samples/01-extract.cs --no-build -- scanned
dotnet run --file samples/02-ingest.cs --no-build -- digital
dotnet run --file samples/02-ingest.cs --no-build -- scanned
PORT=8187 node serve.mjs
```

Use the **Ports** tab to open 8187. In another terminal, with the same offline
environment:

```bash
dotnet run --project hero/hero.Web/hero.Web.csproj --no-build --no-launch-profile --urls http://127.0.0.1:5187
```

Open port 5187 for the native/offline web view. The two configured forwarded
ports use Codespaces' **private-by-default** visibility; do not make them public.
No token needs printing/copying. The servers bind to loopback and are started
only by your explicit command. Stop only your own server when finished.

The shared bootstrap/native path was exercised on local Ubuntu 24.04 x64 with
the exact SDK/Node versions. This is **not** a claim of a completed hosted
Codespaces lifecycle: Docker Desktop's engine was unavailable in the preparation
environment, and no paid Codespace was created. A first actual container build,
post-create lifecycle and private forwarded browser/Blazor check remain remote
validation steps. See [agent guidance](docs/AI-AGENTS.md) for optional tools;
none are installed automatically.

## Manual Windows preparation

Use .NET SDK **10.0.403** and Node.js (recorded version **24.22.1**).
From the repository root:

```powershell
$env:STANDUP_PROVIDER = 'pdfpig'
Remove-Item Env:\STANDUP_LIVE -ErrorAction SilentlyContinue
Remove-Item Env:\STANDUP_BUDGET_PATH -ErrorAction SilentlyContinue
.\scripts\prepare.ps1
dotnet build hero\hero.sln -v:q
dotnet build samples\03-retrieve.cs
dotnet run --file checks\verify.cs
npm ci --ignore-scripts
node.exe build.mjs
```

Preparation verifies/extracts `vendor\s0-source.zip` into `.build\s0`, then
builds eight **net10-only unofficial** local packages at
`10.8.0-standup.s0.0ff84e7.1`. Nothing is published. NuGet source mapping
resolves these exact package IDs from `.build\feed`, not the old development
feed. Root build properties isolate the build from parent-directory settings.
Do not parallelize builds sharing project outputs.

Composite identity is the original manifest hash, exact 26-file Documents
overlay receipt, and exact 920-file export receipt, not one Git commit.
See `vendor\NOTICE.md`, `vendor\s0-provenance.json` and
`vendor\source-manifest.json`. Nuspec repository metadata identifies only the
Extraction baseline. The original final `.1` package/consumption receipts
are under `evidence\public`; a fresh local preparation writes its own ignored
logs/receipt under `evidence`, without changing those historical recordings.

The same preparation script works under PowerShell 7 on Windows and Linux.
It normalizes frozen Windows manifest paths only for filesystem access; it does
not rewrite the archive or source bytes. Upstream code-style enforcement is
disabled **only while packing frozen libraries**, because their retained CRLF
bytes otherwise trigger host-specific IDE0055 formatting errors on Linux.
Compilation and other analyzers remain enabled.

Fresh local nupkgs do not necessarily have the original Windows recording's
content hashes. Preparation derives nine ignored `.build/locks` files from the
committed lock templates, replacing **only the eight exact S0 content hashes**
with the freshly packed hashes. All versions, public package hashes and other
dependency entries remain locked. Tracked templates and vendor receipts are not
modified; no broad `--force-evaluate` dependency refresh is used. NuGet uses
`.build/packages`; preparation invalidates only these eight rebuilt version
directories there, never a shared global package cache. Stop your own .NET
builds/apps before re-preparing.

## Three standalone files

Native extraction and deterministic chunking run without Azure credentials.
The manual PowerShell snippets below use Windows paths; Linux/Codespaces
commands use `/` as in the quick start above.

```powershell
$env:STANDUP_PROVIDER = 'pdfpig'
Remove-Item Env:\STANDUP_LIVE -ErrorAction SilentlyContinue
Remove-Item Env:\STANDUP_BUDGET_PATH -ErrorAction SilentlyContinue
dotnet run --file samples\01-extract.cs -- digital
dotnet run --file samples\01-extract.cs -- scanned
dotnet run --file samples\02-ingest.cs -- digital
dotnet run --file samples\02-ingest.cs -- scanned
```

Digital: two genuine source pages, two native content elements, **12 MEDI
chunks**. Scanned: two source pages, **zero native text/chunks**.
PdfPig is a demo-local adapter, not an installed extraction-library provider;
it is **not OCR** and never silently falls back to the cloud.

Sample 02 constructs the real pinned types visibly:

```csharp
var tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
var options = new IngestionChunkerOptions(tokenizer)
{
    MaxTokensPerChunk = 384,
    OverlapTokens = 48
};
var chunker = new DocumentTokenChunker(options);
```

The options type is
`Microsoft.Extensions.DataIngestion.IngestionChunkerOptions`, resolved against
the compiled final `.1` dependencies. Tokenization is local, not a model call.
The Documents-to-MEDI bridge chunks source-associated local units and explicitly
attaches their known source sets; it does not claim arbitrary metadata
propagation. Keep the S0 `Document` alive until chunk consumption finishes,
then dispose it; the input stream remains caller-owned.

After the initial run, presentation-cache execution is:

```powershell
dotnet run --file samples\01-extract.cs --no-build -- digital
dotnet run --file samples\02-ingest.cs --no-build -- digital
```

Each file is a .NET 10 file app with a supported `#:project` directive, outside
the web project's source cone. Always use `--file`. Sample 03 contains the
real MEAI embeddings and VectorData SQLite upsert/search pipeline. With live
disabled, this command **intentionally fails before any Azure request**:

```powershell
dotnet run --file samples\03-retrieve.cs --no-build
```

That refusal and the offline synthetic-vector schema check are not live
retrieval results. Actual prior learned-vector evidence is clearly labeled
under `evidence\public`.

## Chat-template payoff

Keep the native/offline environment above, then:

```powershell
dotnet run --project hero\hero.Web\hero.Web.csproj --no-build --no-launch-profile --urls http://127.0.0.1:5187
```

Open `http://127.0.0.1:5187`. Native inspection needs no model calls. The page
does not ingest on startup or silently rebuild an index when asked a question.
Building a learned index and answering require separate explicit live
authorization/configuration. A clone has no ready index.

Provider selection is startup-only (`pdfpig` or `cu`), not a hot switch. CU has
no hidden native fallback; opening a CU page does not analyze a document.
Indexes are provider/configuration/fixture/policy-specific, with actual
1536-dimensional embeddings and record-derived citations. Chat searches only
the ready current index and sends one guarded request without tool turns or
automatic suggestions. Sensitive AI telemetry is absent; general telemetry is
opt-in and disabled by default.

The preserved Aspire apphost can be built with the solution and started with:

```powershell
dotnet run --project hero\hero.AppHost\hero.AppHost.csproj
```

Apphost compilation was verified; Aspire/dashboard startup was not part of the
recorded direct-web browser check. No Docker/vector service is required.
The retained optional apphost lock template includes the Windows dashboard
package. Linux Codespaces bootstrap intentionally builds `hero.Web` directly,
not the optional apphost/solution. Linux Aspire dashboard startup/its
platform-specific lock is not part of the supported offline quick-start path.

## Live configuration is deliberately not armed

`examples\live-budget.example.json` is an **unauthorized, halted template with
invalid placeholder endpoints**. It contains no credentials or real resources.
Do not use it to replace, reset, refund or re-arm an existing ledger.

The original rehearsal ledger, resource inventory, credentials, approvals,
indexes and raw auth/environment diagnostics are private and not distributed.
The original phase is complete and halted; its historical counters are not an
allowance for a new run.

A separate live run requires the owner's explicit approval, their own existing
keyless resources/account/tenant and a private cumulative ledger. Only then may
they set `STANDUP_BUDGET_PATH` to that private file and `STANDUP_LIVE=1` in the
process they intentionally launch. Neither the README commands nor this repo
logs in, provisions resources, changes defaults/RBAC, obtains a token for an
offline demo, or silently enables live work.

The existing runtime guards remain mandatory: reserve before requests, retain
reservations on failure, stop on a primary failure, no SDK request retries,
known fixture hashes only, fixed embedding dimensions/models, no tool turns.
Do not remove the wrappers for presentation. `DefaultAzureCredential` is used
unchanged. No real endpoint, credential or account setting belongs in Git.

## Slides and persistent orientation

```powershell
node.exe build.mjs
$env:PORT = '8187'
node.exe serve.mjs
```

Open `http://127.0.0.1:8187`. The server binds to loopback. If an existing preview
owns that port, reload it; do not stop/restart another session's server.
There is no Pages/deployment/release workflow.

`slides.md` has **42 slides / 42 speaker notes**, concrete setup and
code/real-result pairs with fixed **25px logical code**. A persistent rail
reserves its own viewport space outside the 1280x720 slide-content box:

- Prepare: read/extract, chunk, **embed documents**, write shared index.
- Answer: question, **embed question**, retrieve shared index, answer, inspect source.

Every slide explicitly declares `data-workflow`, `data-stages` and
`data-context` in its Reveal `.slide` comment. No positional/title guessing is
used. The active stages have a double border, bold/underlined label and
`aria-current`, not color alone. Overview/shared states and native/CU reader
context are explicit. Source inspection is evidence checking, not a guarantee.
`index.html` updates the rail on readiness and slide changes (including direct
hashes); `css\workflow.css` reserves/reflows space. Print uses per-slide
orientation labels rather than repeating one global rail.

Generated `public` includes the relevant font/viewer notices; source remains
the editable deck. See `THIRD-PARTY-NOTICES.md`.

## Interpretation, fallback and optional visual

Question:

> According to Table 2, what is the mean gas resource for the Ural Foredeep
> Continuous Gas AU, in BCFG?

Source answer: **35,511 BCFG**, page 2, Table 2. That number also occurs in
page-1 prose. Both real model answers were correct, but **the number alone is
not proof of table-grid understanding**. Native hits ranked prose first; CU also
retrieved the actual table row. Source links are generated from retrieved
records, not model-supplied pages.

Use `evidence\public\README.md` for real recordings, usage and caveats.
Do not present recorded/synthetic checks as fresh live runs. The historical
failed final `.1` attempt, successful final `.1` rehearsal and earlier
initial-version `.0` checks remain distinct; the latter are not public runs.

The optional invoice PNG under `supplemental\invoice-validation\visual-input`
is an **adapted simplified redraw**, not original Microsoft artwork, attributed
to Microsoft Learn / MicrosoftDocs/power-platform under CC BY 4.0, with no
endorsement. It is **not VLM/live verified**. It is not a replacement PDF
fixture or a third provider. Keep the SVG, source article and expected-answer
JSON in `reference` outside any extraction/retrieval inputs; a future approved
visual proof would receive the PNG only.

## Reset only this demo's current index

Stop active work first and keep the provider/private configuration used to
build the index:

```powershell
dotnet run --file scripts\reset-index.cs -- pdfpig
```

Select `cu` explicitly for its collection. Reset deletes only the current
provider/configuration/fixture-specific collection and ready marker, not the
database, other provider indexes, private ledger or cloud resources. A partial
failed build has no ready marker. Never reset a budget to retry a request.
