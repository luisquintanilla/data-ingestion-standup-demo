# Third-party source and assets

The root MIT license applies to this demo's original code. It does not replace
the licenses or public-domain notices for the following inputs.

| Input | Source / pinned identity | License retained here |
|---|---|---|
| Hero components, local viewers, Aspire and Reveal scaffolding | [iocrclient-demo](https://github.com/luisquintanilla/iocrclient-demo), `fd4bf7edfd5f3330dbb0ba73938ada20e282e111` | [Original MIT license](licenses/iocrclient-demo-LICENSE.txt) |
| Composite S0 libraries and build inputs | Public `dotnet/extensions` baselines plus the exact frozen Documents overlay; see [composite provenance](vendor/NOTICE.md) | MIT `LICENSE` and `THIRD-PARTY-NOTICES.TXT` inside the unchanged `vendor/s0-source.zip` |
| PDF.js / pdfjs-dist 4.10.38 | [PDF.js release](https://github.com/mozilla/pdf.js/tree/v4.10.38); bundled file headers retained | [Apache 2.0](licenses/pdfjs-dist-LICENSE.txt) |
| DOMPurify 3.2.4 | [Source release](https://github.com/cure53/DOMPurify/tree/3.2.4); bundled copyright header retained | [Upstream dual-license text](licenses/DOMPurify-LICENSE.txt), Apache 2.0 or MPL 2.0 |
| Marked 15.0.6 | [Source release](https://github.com/markedjs/marked/tree/v15.0.6) | [MIT](licenses/marked-LICENSE.txt) |
| Tailwind CSS preflight 4.0.3 | [Source release](https://github.com/tailwindlabs/tailwindcss/tree/v4.0.3) | [MIT](licenses/tailwindcss-LICENSE.txt) |
| Open Sans v34 Latin fonts | Embedded copyright: Copyright 2020 The Open Sans Project Authors; [source](https://github.com/googlefonts/opensans) | [SIL OFL 1.1](licenses/OpenSans-OFL.txt) |
| Space Grotesk v12 Latin font | Embedded copyright: Copyright 2020 The Space Grotesk Project Authors; [source](https://github.com/floriankarsten/space-grotesk) | [SIL OFL 1.1](licenses/SpaceGrotesk-OFL.txt) |
| Reveal.js 5.2.1 | Restored through the pinned `package-lock.json`; not vendored in Git | MIT license retained in the restored package and copied Reveal distribution |
| USGS fact sheet, scanned twin and source crops | [USGS Fact Sheet 2018-3050](https://doi.org/10.3133/fs20183050); see [fixture hashes](fixtures/NOTICE.md) | USGS-authored U.S. government material is public domain; original source attributions retained |
| Supplemental invoice-validation redraw | [Microsoft Learn workflow](https://learn.microsoft.com/power-platform/architecture/reference-architectures/vendor-invoice-integration), Microsoft Learn / MicrosoftDocs/power-platform | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/); adapted and simplified, not original Microsoft artwork, no endorsement |

Font copyright/license declarations were read from the bundled WOFF2 name tables,
not inferred from a similarly named replacement font. Font files are unchanged.
Viewer and font license texts accompany the generated local deck too.

The composite S0 source is an **unofficial API-review prototype**, including a
frozen working-tree overlay rather than one upstream commit. Public availability
of a baseline does not imply the overlay was merged or the APIs were approved.
All eight locally built packages remain unofficial final `.1` packages; no
packages are distributed as part of this source repository.

NuGet dependencies are restored by their pinned package references/lock files;
their packages retain their upstream license metadata. Compiled assemblies,
PDBs, build caches and local NuGet packages are not proposed Git content.

The optional invoice PNG is the only proposed future visual-model input. Its
editable SVG and source/expected-answer JSON are reference material, not
extraction or retrieval inputs. No invoice-image or VLM run was performed.
