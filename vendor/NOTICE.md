# Pinned unofficial source cohort

`s0-source.zip` contains unchanged production source and supporting build inputs
needed for the eight libraries used by this demo. Tests and unrelated library
implementations are not bundled. The archive retains the .NET Foundation MIT
license. `source-manifest.json` records every bundled source hash and the
archive hash.

The original immutable 3,764-file manifest has SHA256
`cb1a996c71b5e7d9162c04cefd12cf65207e5d777fec3539a2818d4007fc3f92`.
`s0-provenance.json` retains that identity and all **26** Documents overlay
path/size/hash entries, including test entries retained as provenance rather
than bundled test code. `source-manifest.json` is the authoritative exact
**920-file** production/build export receipt. Together they identify composite
S0; no single repository commit reproduces this cohort.

Extraction commit: `9112251596986014c3a4baccddf099bd43eb918b`.
Documents baseline: `0aec91c3d355a522bb713e9ec4a4f96fcdb09849`, with the frozen
selected A overlay captured in S0. This source is not the independent S1 API
review candidate and must not be changed to follow it.

`scripts\prepare.ps1` verifies/extracts this archive into this repo's own
`.build\s0`, then builds net10-only local development packages with the unique
version `10.8.0-standup.s0.0ff84e7.1`. Nothing is published. Net10-only packaging
disables upstream shipping-package baseline/minimum-TFM validation; it does not
modify the library source, declare other TFMs supported, or claim API approval.
Source package dependency versions and the Arcade SDK remain pinned by their
original build inputs. Actual local package hashes are emitted into
`evidence\package-identities.json`.

Packaging disables Git discovery/SourceLink and explicitly stamps the pinned
Extraction base repository/commit. This prevents accidentally labeling the
library sources with this demo worktree's Git HEAD. The selected Documents
overlay is additionally identified by the source manifest; it is not claimed
to exist in that base commit. The `.1` suffix distinguishes these corrected
package receipts from the initial local-only preparation attempt.

The old `10.8.0-dev` feed is not consumed. Source mapping in `nuget.config`
routes these eight exact package IDs exclusively to the freshly prepared local
feed. This demo uses actual S0 Documents, Extraction, MEAI and MEDI types, not an
experiment harness, replacement public hierarchy, or another worktree's outputs.

The hero Blazor components, PDF/Markdown viewers, Aspire projects and RevealJS
infrastructure are curated from `luisquintanilla/iocrclient-demo` commit
`fd4bf7edfd5f3330dbb0ba73938ada20e282e111`, under its retained MIT license.
Bundled viewer/font assets retain their own license notices.
