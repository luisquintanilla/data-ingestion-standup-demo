# Recorded evidence, not an automatic live replay

These are curated copies or explicitly selected excerpts from genuine local
runs. They contain public fixture content, real returned values and sanitized
browser captures, not credentials, endpoints, account identifiers, approvals,
raw authentication diagnostics or an authorization ledger.
Original private records remain outside the proposed public tree.

## What was observed

The standalone native extraction/chunking recordings use final
`10.8.0-standup.s0.0ff84e7.1` dependencies: digital **2 pages / 2 content
elements / 12 chunks**, scanned **2 pages / 0 text elements / 0 chunks**.
PdfPig is not OCR. The policy is 384 tokens with 48 overlap.

The later bounded final `.1` live rehearsal completed in this order:
native retrieval sample, native hero answer, scanned CU extraction sample,
CU retrieval sample for both PDFs, CU hero answer reusing its prepared index.
The scanned CU extraction returned **71 elements including 2 typed tables**.
CU chunking produced **83 digital + 83 scanned** records.
Actual returned embedding vectors had **1536 dimensions**; logs expose a
verbatim prefix, not invented vectors or a shipped ready index.

Both real hero answers report **35,511 BCFG**. Native retrieval ranked page-1
prose first. CU also retrieved the actual page-2 table row, but its leading
hits were page-1 prose too. The number occurs in that prose. Neither the answer
nor the model's "According to Table 2" wording proves table-grid reasoning.
Scores are raw search scores, not confidence probabilities.

## Files and interpretation

| Evidence | Meaning |
|---|---|
| `01-extract-*.txt`, `02-ingest-*.txt` | Actual final `.1` native extraction/chunking recordings, not model output |
| `native-retrieval.txt`, `cu-retrieval.txt`, `cu-scanned-extraction.txt` | Actual final `.1` sample output; public fixture text, vector prefixes, scores and usage |
| `*-hero-answer.json`, `*-hero-answer.png` | Actual browser responses and record-derived citations, not a live replay |
| `native-source-targets.json`, `cu-source-target.json`, `*-source-page2.png` | Real physical source checks; the final page is fully visible at a document-end scroll clamp |
| `reported-usage.json` | Successful provider-reported token usage only, not a bill or conservative reservation ledger |
| `package-identities.json`, `final-consumption.json` | Original eight final `.1` package identities and actual consumption receipts, not reproducibility claims based on one commit |
| `historical-auth-failure.txt` | Safe selected lines from the earlier failed final `.1` attempt, not the later successful run |
| `rendered-validation-before-workflow-rail.json` | Historical 42-slide checks before the persistent workflow rail was added |
| `workflow-rail/README.md` | Current 42-slide semantic orientation, four-viewport rendered checks and screenshots |

The original authentication failure is deliberately distinguished from the
successful resumed run. Earlier initial-version `.0` checks and the old
10/36-slide decks are private historical artifacts, not relabeled current proof.
The full learned-vector SQLite indexes and private request ledgers are not
distributed. A new clone starts without ready indexes.

The original live phase is complete and halted. The default clone makes no
model/CU request; sample 03 and live hero actions require a separately authorized
private configuration. No new request is needed to read these recordings.

The source-viewer screenshots establish which page was displayed; inspecting a
source is an application evidence check, not an automatic correctness guarantee.
Aspire startup and optional invoice/VLM reasoning are outside this proof.
