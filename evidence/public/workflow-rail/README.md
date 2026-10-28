# Persistent workflow orientation -- offline verification

The actual served deck remains **42 slides / 42 speaker notes**, with **25px
logical code**. `rendered-validation.json` records **168** per-slide checks:
every slide at 1920x1080, the reported 754x876 canvas, 541x876 and 390x844.
There were no page errors, pre/code overflow, checked text/image/frame overflow,
broken images, wrapped workflow lanes or active-label overflow.

The rail is outside the logical 1280x720 slide frame, not overlaid on content.
Its height was at most 154 screen pixels in these cases. Both workflows remain
visible: document embedding belongs to Prepare; question embedding belongs to
Answer. The same provider-specific prepared index connects them. Native and CU
are readers, not separate workflows. Current stages use a double border,
bold/underlined label and `aria-current="step"`; they do not depend on color.
The report includes default-theme text contrast ratios.

`slide-stages.json` is the exact semantic assignment captured from the rendered
slides. The authoritative editable metadata is each Reveal `.slide` comment
in `slides.md`: `data-workflow`, `data-stages`, `data-context`.
`index.html` validates that metadata and updates on initialization/slide changes;
`css\workflow.css` reserves/reflows the rail. No positional/title guessing is used.
The existing deck stays in slide mode on narrow displays rather than silently
activating Reveal's separate scrolling presentation mode.

## Reported regression and correction

Reveal Markdown omits empty-valued slide attributes. Overview slides originally
used `data-stages=""`, so the rendered sections lacked that attribute.
They now explicitly declare **`data-stages="none"`**. This is a declared overview
state, not a fallback for malformed metadata. Missing/invalid metadata still
reports an error; validation starts only after Reveal initializes.

The workflows use fixed grid lanes rather than wrapping the final Answer node
onto another lane. The actual unqueried root URL, a fresh direct `#/10` link,
next/previous, and a real reload were checked. `754-01-root.png` captures the
fixed reported-width root case. The parent-owned 8187 process was not restarted.

The offline headless harness also caught and corrected a test-only mistake:
a `check-width` URL parameter was interpreted by Reveal as a `width` config
override. Final checks use the real root URL and assert the unchanged
1280x720 logical frame and the requested actual slide index.
Screenshots wait for the real transition rather than capturing faded text.

## Representative actual screenshots

| File | Current state |
|---|---|
| `1920-09-native-extraction.png` | Native extraction with actual output |
| `1920-11-cu-return.png`, `1920-12-cu-result.png` | Same preparation path, explicit CU reader and real comparison |
| `1920-15-chunker.png` | Explicit tokenizer/options/chunker |
| `1920-24-document-embedding.png` | Actual recorded document vector |
| `1920-27-index.png` | Explicit index collection |
| `1920-30-question-retrieval.png` | Question embedding and retrieval from the prepared index |
| `1920-31-provider-comparison.png` | Actual native/CU retrieval records |
| `1920-36-cu-answer.png` | Actual recorded CU-index answer |
| `1920-39-source-check.png` | Source inspection, not a correctness guarantee |
| `754-01-root.png`, `754-11-cu-return.png`, `754-30-question-retrieval.png` | Reported-width root and representative transitions |

`print-verification.json` records the existing Reveal print-mode check:
42 PDF pages, each with a per-slide workflow label. The rail itself is hidden
in print; its labels occupy the page margin rather than covering code.
The diagnostic PDF remains private and is not a new export/publishing system.

These are UI checks only. They made **no token, model or CU requests**.
The original completed live phase/ledger, source fixtures, providers, package
inputs and actual recorded output were unchanged. Page-1/Table-2 and optional
invoice/not-VLM caveats remain in the slide bodies/notes.
