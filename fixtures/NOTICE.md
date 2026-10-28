# Public fixture provenance

`digital.pdf` is the USGS fact sheet **Assessment of Continuous Oil and Gas
Resources of the Timan-Pechora Basin Province, Russia, 2018**, Fact Sheet
2018-3050, https://doi.org/10.3133/fs20183050
(https://pubs.usgs.gov/fs/2018/3050/fs20183050.pdf).

`scanned.pdf` is the image-only twin distributed with the pinned
`luisquintanilla/iocrclient-demo` source at
`fd4bf7edfd5f3330dbb0ba73938ada20e282e111`. Both fixtures are copied from that
repository's `hero\hero.Web\wwwroot\Data` directory; the digital source and
scanned twin have two pages each. USGS-authored government material is public
domain; retain the publication's original attributions and notices.

| File | SHA256 |
|---|---|
| digital.pdf | `8b9c6f80f2c957ec8ae8d7074e70fe40e9c682fc0d9a9bc70571a2e5b16f7306` |
| scanned.pdf | `90a4f734f3001f39f8907ec024ec84a011648a8cacfe8f2099d3438c0367fa66` |

Known-answer question: **According to Table 2, what is the mean gas resource for
the Ural Foredeep Continuous Gas AU, in BCFG?**

Verified native source: page 2, Table 2, Ural Foredeep Continuous Gas AU row,
Gas (BCFG), Mean column: **35,511**.

This establishes the source answer, not the quality of an unrun embedding
retrieval or chat response. The dense two-column native layout has imperfect
reading order. The native client does not infer a table grid or OCR the scan.
