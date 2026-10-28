<!--
title: From documents to answers you can check
-->

<!-- .slide: class="opening-slide" data-workflow="overview" data-stages="none" data-context="The evidence-checking problem" -->
## The answer is in here. Show me the source.

<div class="opening">
<div>
<blockquote>According to Table 2, what is the mean gas resource for the Ural Foredeep Continuous Gas AU, in BCFG?</blockquote>
<p>Two copies of the same fact sheet:<br><strong>embedded text</strong> and a <strong>scanned twin</strong>.</p>
<p class="evidence-label">Public source, not model output</p>
</div>
<img src="assets/narrative/usgs-page-2.png" alt="Actual page 2 of USGS Fact Sheet 2018-3050">
</div>
<p class="source">USGS Fact Sheet 2018-3050 / DOI 10.3133/fs20183050</p>

Note:
Start with the job, not a package list. We want an answer we can check against
the row, unit and page. Do not reveal the number yet. The page shown is actual
source material, not a fabricated answer screenshot. Both frozen PDFs have
two pages. This talk uses composite S0, not the independent S1 API lane.

---

<!-- .slide: data-workflow="overview" data-stages="none" data-context="Two workflows, not extraction per question" -->
## Before the prompt, prepare the evidence

**Read** the content. **Keep** its source.

**Prepare** passages the app can find again.

When a question arrives: **retrieve**, **answer**, **inspect**.

<p class="evidence-label">A repeatable app workflow, not a rule that every PDF needs a vector database</p>

Note:
For one small file and a model that supports it, direct file input can be the
simpler choice. Here the small fixtures let us inspect each job in a reusable
ingestion-and-retrieval design. Native reading and chunking need no chat model.
Transition: where do those responsibilities fit in a .NET app?

---

<!-- .slide: class="map-slide" data-workflow="overview" data-stages="none" data-context="How the .NET responsibilities compose" -->
![The .NET pieces grouped by application, data responsibilities, model interfaces and familiar .NET foundations.](assets/narrative/stack-map.png)

<p class="evidence-label">Architecture map. Documents / extraction are unofficial S0 prototypes.</p>

Note:
The app is the destination. Extraction reads; Documents represents local
content, structure and source references; MEDI prepares content; VectorData
stores/searches records. MEAI has separate embedding and chat interfaces.
This is not a mandatory dependency chain or a shipping API announcement.
Everything does not rest on IChatClient.

---

<!-- .slide: class="map-slide" data-workflow="overview" data-stages="none" data-context="Prepare and answer meet at the same index" -->
![Two workflows: prepare documents into an evidence index, then retrieve and answer when a question arrives.](assets/narrative/two-workflows.png)

<p class="evidence-label">Architecture, not a successful cloud-run recording.</p>

Note:
Trace the top row first: extract, chunk, embed, store. Then the question path:
embed the question, retrieve, supply evidence to chat. They meet at the same
index; we do not re-extract per question. There is no scheduler or incremental
ingestion feature hiding here. Let's build the preparation path.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="extract" data-context="Native / demo-local PdfPig adapter" -->
## 01 / Choose the actual reader

```csharp
using Microsoft.Extensions.DocumentExtraction;
using Standup;

using IDocumentExtractionClient client =
    DemoConfiguration.Provider == "pdfpig"
        ? new PdfPigExtractionClient()
        : DemoConfiguration.CreateExtractor();
```

**`PdfPigExtractionClient` is our demo-local adapter.**

Not a provider supplied by the extraction library.

<p class="source">samples\01-extract.cs, samples\02-ingest.cs / native default; guarded CU factory shown later</p>

Note:
Open the real standalone file. The concrete native construction is visible,
not hidden in CreateExtractor. The other branch preserves startup CU selection
and validation of unknown provider names; it is not automatic fallback.
Standup is this demo's namespace; the contract is the pinned S0 library API.
Next, call the reader we just constructed.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="extract" data-context="Native / digital fixture" -->
## 01 / Open, extract, inspect

```csharp
await using var input = File.OpenRead(file);
using var document = await client.ExtractAsync(
    input, Path.GetFileName(file), "application/pdf");

Console.WriteLine(
    $"source pages={document.SourcePages.Count}; " +
    $"content elements={document.EnumerateContent().Count()}");
```

<p class="run">dotnet run --file samples\01-extract.cs -- digital</p>

**S0 owns a disposable result; the caller owns the input.**

<p class="source">samples\01-extract.cs / selected operations, output line wrapped for display</p>

Note:
file is the selected pinned local PDF, not a remote service. Keep the S0
Document alive while consuming it; this is the actual snapshot's lifetime.
No embedding or chat request occurs. Before showing output, let's look at how
the demo-local adapter turns PdfPig data into that representation.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="extract" data-context="Inside the native adapter / page provenance" -->
## Inside our adapter / keep the real page

```csharp
var page = pdf.GetPage(number);
var source = new DocumentSourcePage(
    $"page-{number}", number,
    new DocumentPageDimensions(
        (double)page.Width, (double)page.Height),
    DocumentCoordinateUnit.Point,
    DocumentCoordinateOrigin.BottomLeft, output);
result.AddSourcePage(source);
```

`pdf` is the opened PdfPig PDF; `output` identifies PdfPig 0.1.15.

<p class="source">src\Standup.Core\PdfPigExtractionClient.cs / inside the actual page loop</p>

Note:
The adapter opens PdfDocument from the buffered input, checks the bounded page
count, and registers its DocumentSourceOutput before this loop. This is native
page identity, dimensions and coordinate frame, not model-invented geometry.
The source is attached to content in the next excerpt.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="extract" data-context="Inside the native adapter / local content and source" -->
## Inside our adapter / text plus source

```csharp
var content = new TextContent(MarkdownLiteral.Escape(block.Text));
var element = new DocumentTextElement(content, block.Text);
var box = block.BoundingBox;
element.Sources.Add(new(source,
[
    new(box.Left, box.Bottom), new(box.Right, box.Bottom),
    new(box.Right, box.Top), new(box.Left, box.Top)
]));
section.Elements.Add(element);
```

Blocks come from `DefaultPageSegmenter.Instance.GetBlocks(page.GetWords())`.

<p class="source">src\Standup.Core\PdfPigExtractionClient.cs / demo-local PdfPig-to-Documents mapping</p>

Note:
The blank-block guard surrounds this excerpt. MarkdownLiteral escapes literal
text for Markdown; original block.Text is retained as plain text. The polygon
comes from the actual PdfPig block. Sections are added to the result per page.
This adapter recovers text blocks, not an inferred typed table grid.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="extract" data-context="Native / recorded digital extraction" -->
## Real output / the digital file

<p class="evidence-label verified">Recorded native run / final .1 packages / no model output</p>

```text
S0 | provider=pdfpig | fixture=digital.pdf
source pages=2; content elements=2
source=page-1; page=1; unit=Point; origin=BottomLeft
source=page-2; page=2; unit=Point; origin=BottomLeft
DocumentTextElement | sources=[1] | National and Global Petroleum Assessment
Assessment of Continuous Oil and Gas Resources of the Timan-Pechora
Basin Province, Russia, 2018
```

Text **and** page associations are inspectable before any model call.

<p class="source">evidence\public\01-extract-digital.txt / first seven lines</p>

Note:
These are actual contiguous output lines, not cleaned-up prose. The complete
log includes all recovered content and the S0 Markdown projection. Native
reading order is imperfect in this dense two-column layout; counts do not
prove layout fidelity. Next change only the fixture.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="extract" data-context="Native / scanned comparison / not OCR" -->
## Same reader. Scanned input. No usable text.

<p class="run">dotnet run --file samples\01-extract.cs -- scanned</p>

```text
S0 | provider=pdfpig | fixture=scanned.pdf
source pages=2; content elements=0
```

```text
NO USABLE TEXT. PdfPig is not OCR. There is no automatic cloud fallback.
```

<p class="evidence-label verified">Recorded native output / page-detail lines between excerpts omitted</p>
<p class="source">evidence\public\01-extract-scanned.txt</p>

Note:
This is a useful limitation, not failure-shaped success. Better prompting
downstream cannot recover text this reader did not extract. Choose an
OCR-capable provider explicitly if that is the required capability.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="extract" data-context="CU / same preparation pipeline, different reader" -->
## An OCR-capable reader is an explicit choice

```csharp
var budget = new LiveBudget();
return new ContentUnderstandingExtractionClient(
    new(budget.Resource("contentUnderstanding", "endpoint")),
    new DefaultAzureCredential(), budget);
```

Inside that demo-local adapter:

```csharp
var options = new ContentUnderstandingClientOptions();
options.Retry.MaxRetries = 0;
```

<p class="evidence-label verified">Live verified on both fixtures / 3 bounded analyses / final .1</p>
<p class="source">DemoConfiguration.CreateCu / ContentUnderstandingExtractionClient constructor</p>

Note:
This is the guarded factory used by the earlier non-native branch. The
adapter reserves before prebuilt-layout analysis and persists a halt on
failure; it does not invent page 1 or silently switch providers. It maps
paragraphs/table cells with actual page spans where available. Do not run
this while halted. The digital native file already gives us usable content.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="extract" data-context="CU / recorded OCR-capable extraction, same pipeline" -->
## The scanned twin now has real content

<p class="evidence-label verified">Recorded live CU output / selected verbatim lines</p>

```text
S0 | provider=cu | fixture=scanned.pdf
source pages=2; content elements=71
DocumentTextElement | sources=[1] | USGS science for a changing world
```

The full result contains **2 typed tables**.

Same 384/48 policy: **83 CU chunks per PDF**, versus native **12 / 0**.

<p class="source">evidence\public\cu-scanned-extraction.txt and cu-retrieval.txt / separate real analyses</p>

Note:
This is actual scanned-document service output, not synthetic wire evidence.
CU extracted 71 content elements including two typed tables. CU sample03 then
analyzed both fixtures, producing 83 chunks each. Source-local paragraph/table
units differ from PdfPig's blocks, so chunk counts are not a quality ranking.
No provider fallback or model-default mutation was needed.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="chunk" data-context="Shared Documents-to-MEDI projection / native walkthrough" -->
## 02 / Documents and MEDI are different models

Selected content in our `ReadUnits` bridge:

```csharp
var text = element is DocumentTableElement
    ? element.ToMarkdown()
    : element.Text ?? (element.Content as TextContent)?.Text;
if (!string.IsNullOrWhiteSpace(text))
    yield return Unit(text, Pages(element), $"element-{index++}");
```

Traverse local content; do **not** replay ancestor aggregates.

<p class="source">src\Standup.Core\DocumentIngestion.cs / ReadUnits, Visit non-section branch</p>

Note:
ReadUnits is our adapter, not a library promise that the two models are
interchangeable. Root/section-local text is visited separately, then children.
Tables project to Markdown, while their typed representation remains in
Documents. Pages includes associated table/cell sources when present.
Now show the concrete MEDI unit this bridge creates.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="chunk" data-context="Shared source-association bridge / native walkthrough" -->
## The bridge constructs an ingestion unit

```csharp
var ingestion = new IngestionDocument(document.Identifier);
var section = new IngestionDocumentSection();
section.Elements.Add(new IngestionDocumentParagraph(text)
{
    Text = text,
    PageNumber = pages.Length == 1 ? pages[0] : null
});
ingestion.Sections.Add(section);
return (ingestion, pages, identity);
```

Each unit retains its **page set** and **element identity**, separately.

<p class="source">src\Standup.Core\DocumentIngestion.cs / local Unit function inside ReadUnits</p>

Note:
Only a single known page becomes MEDI PageNumber. Unknown or multi-page
associations are not guessed into page 1. The tuple carries the original
page set even when PageNumber is null. Each unit is chunked independently.
Now configure the actual chunker rather than hiding it in a factory.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="chunk" data-context="Local tokenizer / 384 tokens with 48 overlap" -->
## Name the tokenizer, options, and chunker

```csharp
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.ML.Tokenizers;

var tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
var options = new IngestionChunkerOptions(tokenizer)
{
    MaxTokensPerChunk = 384,
    OverlapTokens = 48
};
var chunker = new DocumentTokenChunker(options);
```

<p class="evidence-label">Local tokenizer vocabulary. No embedding request.</p>
<p class="source">samples\02-ingest.cs / actual pinned options: Microsoft.Extensions.DataIngestion.IngestionChunkerOptions</p>

Note:
The named options type was resolved by reflection from the compiled final .1
dependency, not guessed from a file name or current main. Evidence:
resolved-chunker-api.log. 384/48 is an inspectable demo policy, not a universal
best setting. The helper used elsewhere now spells out this same setup.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="chunk" data-context="Native walkthrough / sources stay attached" -->
## Use the chunker we just constructed

```csharp
foreach (var unit in DocumentIngestion.ReadUnits(document))
{
    await foreach (var chunk in chunker.ProcessAsync(unit.Document))
    {
        count++;
        Console.WriteLine(chunk.Content);
    }
}
```

<p class="run">dotnet run --file samples\02-ingest.cs -- digital</p>

`ReadUnits` is **our bridge shown on the previous slides**.

<p class="source">samples\02-ingest.cs / metadata-print line omitted; document stays alive through consumption</p>

Note:
The real loop also prints the count, element, pages and token count with the
same explicit tokenizer. The chunker is not IDisposable in S0. There are no
embedding or chat calls in this standalone sample. Let's inspect actual text,
not just a count.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="chunk" data-context="Native / recorded chunk content and source" -->
## Real chunk output / text and source together

<p class="evidence-label verified">Recorded native run / first seven contiguous lines</p>

```text
S0 | provider=pdfpig | fixture=digital.pdf
MEDI token policy=384/48; no embedding/chat calls
chunk=1; element=element-0; pages=[1]; tokens=384
National and Global Petroleum Assessment
Assessment of Continuous Oil and Gas Resources of the Timan-Pechora
Basin Province, Russia, 2018
Using a geology-based assessment methodology, the U.S. Geological Survey estimated undiscovered, technically recoverable mean resources
```

<p class="source">evidence\public\02-ingest-digital.txt / long output line wraps on screen</p>

Note:
The displayed token count describes the complete chunk, not just this short
prefix. Nothing was generated by a model. The complete log retains the rest
of this passage and all other chunks. Page sets are adapter-owned source
associations, not newly inferred exact token positions.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="chunk" data-context="Native / digital versus scanned, identical policy" -->
## Same policy / 12 digital, 0 scanned

Digital run, final line:

```text
actual chunks=12; source document remains alive through consumption
```

Scanned run, selected lines:

```text
S0 | provider=pdfpig | fixture=scanned.pdf
MEDI token policy=384/48; no embedding/chat calls
actual chunks=0; source document remains alive through consumption
NO CHUNKS. This is a visible extraction limitation, not fallback success.
```

<p class="source">evidence\public\02-ingest-{digital,scanned}.txt / recorded native output</p>

Note:
The native scan has nothing to chunk, and no model call fixes that implicitly.
Both fixtures use the same chunk policy. These outputs exactly match the
previous final .1 records after the setup was made explicit.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="index" data-context="Source-linked record / shared schema" -->
## Keep the source connection in the record

```csharp
public sealed record EvidenceChunk(
    string Text, string DocumentId, int[] Pages, string Element);
```

Inside the app's `ChunkAsync`:

```csharp
foreach (var unit in ReadUnits(document))
    await foreach (var chunk in chunker.ProcessAsync(unit.Document, ct))
        result.Add(new(chunk.Content, document.Identifier,
            unit.Pages, unit.Element));
```

<p class="source">src\Standup.Core\DocumentIngestion.cs / record + selected ChunkAsync loop</p>

Note:
ChunkAsync reuses our now-explicit CreateChunker helper and the bridge already
shown. Carrying these fields is our code, not a claim that arbitrary MEDI
transforms preserve metadata. The page set belongs to the unit, not an
invented precise location for every token. Next: finding passages for a question.

---

<!-- .slide: class="code-slide" data-workflow="shared" data-stages="embed-documents embed-question answer" data-context="SDK setup reused by both workflows" -->
## 03 / First, construct the model client

```csharp
var endpoint = new Uri(budget.Resource("models", "endpoint"));
var credential = new DefaultAzureCredential();
var options = new AzureOpenAIClientOptions
{
    RetryPolicy = new ClientRetryPolicy(0)
};
return new AzureOpenAIClient(endpoint, credential, options);
```

`budget` reads the approved private receipt. No keys in the demo.

<p class="evidence-label verified">Actual runtime setup / successful final .1 live paths / no keys</p>
<p class="source">src\Standup.Core\ModelClients.cs / private Create method; retry count is zero</p>

Note:
Show actual SDK setup before the app factory. This private method only
constructs the client; it is not an unguarded execution path. The public
factories validate model identity and return bounded wrappers. Resource
inventory stays in the ignored receipt, not slides or committed configuration.

---

<!-- .slide: class="code-slide" data-workflow="shared" data-stages="embed-documents embed-question" data-context="Guarded embedding client / documents and questions" -->
## Adapt the SDK client, then keep the guard

```csharp
var client = Create(budget);
var deployment = budget.Resource("models", "embeddingDeployment");
IEmbeddingGenerator<string, Embedding<float>> inner =
    client.GetEmbeddingClient(deployment).AsIEmbeddingGenerator();
return new BoundedEmbeddings(inner, budget);
```

`Create` is the **private SDK setup just shown**.

<p class="evidence-label verified">Live verified / actual 1536-dimensional embeddings returned</p>
<p class="source">src\Standup.Core\ModelClients.cs / Embeddings factory, after model-identity check</p>

Note:
AsIEmbeddingGenerator is the actual MEAI adapter. The raw inner client never
escapes this factory: BoundedEmbeddings reserves conservative input tokens,
enforces 1536 dimensions/count, disallows model overrides and persists a halt
on failure. A helper is useful once its implementation has been shown.

---

<!-- .slide: class="code-slide" data-workflow="shared" data-stages="embed-documents embed-question answer" data-context="All model requests remain guarded" -->
## The runtime guard is not optional

Inside `BoundedEmbeddings.GenerateAsync`, before the request:

```csharp
int upperBound = checked(input.Sum(
    text => Encoding.UTF8.GetByteCount(text)));
budget.Reserve("embeddingInputTokens", upperBound,
    "text-embedding-3-small (UTF8 upper bound)");
```

The sample uses that **guarded** factory:

```csharp
var budget = new LiveBudget();
using var generator = ModelClients.Embeddings(budget);
```

<p class="source">ModelClients.cs / samples\03-retrieve.cs / selected runtime excerpts, not an alternative execution path</p>

Note:
LiveBudget checks the opt-in flag and authoritative receipt. SDK retries are
zero. Reservations are retained on failure and the persistent halt blocks
later calls. The bounded phase completed successfully and was halted again
to prevent additional work. Do not execute again under the exhausted phase
allowances. First show the earlier failure; then the newly recorded result.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="embed-documents" data-context="Native / historical failed attempt, not the later result" -->
## Historical first attempt / the guard held

<p class="evidence-label blocked">Earlier final .1 snapshot / not the outcome of the resumed rehearsal</p>

```text
digital: 12 actual chunks
scanned: 0 actual chunks
```

```text
Azure.Identity.CredentialUnavailableException:
DefaultAzureCredential failed to retrieve a token from the included credentials.
```

**Then: halted before a successful embedding response.**

<p class="source">evidence\public\historical-auth-failure.txt / selected lines, exception prefix split for readability</p>

Note:
This is not a successful embedding/retrieval result. The guard retained its
conservative reservation and halted further calls. The initial local credential
discovery problem was later resolved without changing the credential
implementation. Private account, environment and approval diagnostics are not
part of the public evidence. The next slide is a separate successful run,
not this old failure relabeled as success.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="embed-documents" data-context="Native / actual learned document vector, final .1" -->
## Now: real embeddings, not placeholder arrays

<p class="evidence-label verified">Recorded resumed live run / native sample 03 / final .1</p>

```text
digital: 12 actual chunks
scanned: 0 actual chunks
Actual learned vector[0]: dimensions=1536; first8=[-0.03765869,0.024429321,0.037353516,0.061035156,-0.019195557,-0.025390625,-0.0019350052,0.027694702]
```

Provider-reported input usage: **4,119 chunk tokens**, **26 query tokens**.

<p class="source">evidence\public\native-retrieval.txt / selected verbatim lines; full index was retained privately</p>

Note:
The first eight components are actual returned vector data, not illustrative
numbers. The guarded generator checked every vector's 1536 dimensions. The
new run stored all 12 records and queried them through actual VectorData.
The old failed reservation was not refunded. Usage counts are separate from
the conservative UTF-8 reservation ledger.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="index" data-context="Shared record schema / content, vector and provenance" -->
## Store evidence, not just a vector

Selected properties of `IndexedEvidence`:

```csharp
[VectorStoreData]
public string DocumentId { get; set; } = "";
[VectorStoreData]
public string Text { get; set; } = "";
[VectorStoreData]
public string Pages { get; set; } = "";
[VectorStoreData]
public string Element { get; set; } = "";
```

<p class="evidence-label verified">Live populated schema / text and source fields accompany the embedding</p>
<p class="source">src\Standup.Core\EvidenceIndex.cs / IndexedEvidence; key/vector on the next slide</p>

Note:
Document ID, text, page set and element identity survive as explicit record
fields. Pages is a serialized page set, not a model-produced citation. Now
make the vector field and concrete store visible too.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="index" data-context="Shared 1536-dimensional vector schema" -->
## Match the embedding schema

```csharp
[VectorStoreKey]
public string Key { get; set; } = "";

[VectorStoreVector(1536,
    DistanceFunction = DistanceFunction.CosineDistance)]
public ReadOnlyMemory<float> Vector { get; set; }
```

**1536 dimensions** on both the embedding request and stored vector.

<p class="evidence-label verified">Actual typed schema / real returned vectors, 1536 dimensions</p>
<p class="source">src\Standup.Core\EvidenceIndex.cs / IndexedEvidence</p>

Note:
The offline schema roundtrip used explicitly synthetic vectors and is not
learned retrieval evidence. There are no float arrays, similarity scores or
made-up hits on these slides. The runtime validates count/dimension alignment.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="index" data-context="Explicit SQLite collection / provider-specific index" -->
## Construct the actual SQLite collection

```csharp
Name = DemoConfiguration.IndexName(provider);
var directory = Path.Combine(
    DemoConfiguration.Root, ".build", "indexes");
Directory.CreateDirectory(directory);
var databasePath = Path.Combine(directory, "standup.db");
Store = new SqliteVectorStore($"Data Source={databasePath}");
Collection = Store.GetCollection<string, IndexedEvidence>(Name);
```

`IndexName` is app-specific: provider + model config + fixture hashes + policy.

<p class="evidence-label verified">Live used / concrete VectorData SQLite implementation</p>
<p class="source">src\Standup.Core\EvidenceIndex.cs / ready-marker path assignment omitted</p>

Note:
This is actual SqliteVectorStore, not an unspecified database helper.
DemoConfiguration computes this demo's fingerprint; it is not a VectorData
API. Different providers/configurations cannot silently share a ready index.
The current native index was never made ready because authentication failed.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="embed-documents" data-context="Document embeddings / not question-time embeddings" -->
## Prepare: embed the passage text

```csharp
var embeddings = await generator.GenerateAsync(
    chunks.Select(chunk => chunk.Text),
    new() { Dimensions = 1536 });
index.Invalidate();
await index.Collection.EnsureCollectionDeletedAsync();
await index.Collection.EnsureCollectionExistsAsync();
```

`generator` is the **guarded factory already unpacked**.

<p class="evidence-label verified">Live completed / native 12 records; CU 166 records / separate current indexes</p>
<p class="source">samples\03-retrieve.cs / preparation path, not question-time work</p>

Note:
The code first extracts/chunks both fixtures. The historical first run failed
on the first statement here; the newly authorized run completed it. Both
provider-specific indexes were written and marked ready, with actual learned
vectors. The hero reused them without rebuilding or reanalyzing PDFs.

---

<!-- .slide: class="code-slide" data-workflow="prepare" data-stages="index" data-context="Write prepared records / shared workflow boundary" -->
## Keep the passage beside its source and vector

```csharp
await index.Collection.UpsertAsync(new IndexedEvidence
{
    Key = i.ToString(System.Globalization.CultureInfo.InvariantCulture),
    DocumentId = chunk.DocumentId,
    Text = chunk.Text,
    Pages = string.Join(",", chunk.Pages),
    Element = chunk.Element,
    Vector = embeddings[i].Vector
});
```

<p class="evidence-label verified">Live completed / records retain document, page set, element, text and vector</p>
<p class="source">samples\03-retrieve.cs / inside the indexed-chunk loop</p>

Note:
Do not reduce this to an unexplained array. The same record ties text,
document/page associations and vector together. index.MarkReady follows the
completed loop, not a partial write. Next switch to the question-time path.

---

<!-- .slide: class="code-slide" data-workflow="answer" data-stages="embed-question retrieve" data-context="Question vector / search the existing prepared index" -->
## Question time: search the same embedding space

```csharp
var query = await generator.GenerateAsync(
    [question], new() { Dimensions = 1536 });
await foreach (var hit in index.Collection.SearchAsync(
    query[0].Vector, top: 4))
{
    Console.WriteLine(hit.Record.Text);
}
```

The full sample prints each score and **record-derived** source link too.

<p class="evidence-label verified">Live verified / actual hits, raw scores and source targets captured</p>
<p class="source">samples\03-retrieve.cs / selected query/search lines, diagnostic print lines omitted</p>

Note:
Do not re-extract the PDFs here. The app searches only its current ready
index and uses the same embedding space for passages and questions.
A similarity score is not a probability that the answer is correct.
Search produces candidate evidence for inspection, not guaranteed support.

---

<!-- .slide: class="code-slide" data-workflow="answer" data-stages="retrieve" data-context="Native versus CU / actual returned records" -->
## Inspect what retrieval actually returned

Selected native hit:

```text
score=0.29430830478668213; source=/Data/digital.pdf#page=1; element=element-0
```

Selected CU table hit:

```text
score=0.3616464138031006; source=/Data/scanned.pdf#page=2; element=element-48
```

**Both top-ranked contexts contain page-1 prose.**
CU also returned the actual Table 2 row as HTML text, including the AU and 35,511.

<p class="source">evidence\public\{native,cu}-retrieval.txt / selected verbatim score lines; not probabilities</p>

Note:
Native top four hits cover digital pages 1,1,2,2. CU top four cover scanned
page1, digital page1, scanned page2, digital page2. CU's page2 table chunks
contain the actual AU row and number, but their headers can be outside the
chunk. The answer can also be supported by page1 prose. Do not claim that a
correct number proves the model understood the grid.

---

<!-- .slide: class="code-slide" data-workflow="answer" data-stages="answer" data-context="Guarded chat client / shared native and CU composition" -->
## Give chat the same explicit SDK setup

```csharp
var client = Create(budget);
var deployment = budget.Resource("models", "chatDeployment");
IChatClient inner =
    client.GetChatClient(deployment).AsIChatClient();
return new BoundedChat(inner, budget);
```

`Create` is the **AzureOpenAIClient + DefaultAzureCredential setup shown earlier**.

<p class="evidence-label verified">Live verified / exactly 1 native + 1 CU grounded chat request</p>
<p class="source">src\Standup.Core\ModelClients.cs / Chat, after authorized-model validation</p>

Note:
AsIChatClient is the MEAI SDK adapter. The raw client remains inside the
factory. BoundedChat reserves one chat request, rejects model overrides/tools,
does not stream, and persists a halt on failure. No suggestions or hidden tool
turns are added. Now compose that with the retrieved evidence in the web app.

---

<!-- .slide: class="code-slide" data-workflow="answer" data-stages="retrieve answer" data-context="App composition / reuse prepared provider-specific index" -->
## Compose the app around retrieved evidence

```csharp
var sources = await index.SearchAsync(question, embeddings, ct);
```

The actual source prompt is built from those records:

```csharp
var evidence = string.Join("\n\n", sources.Select(source =>
    $"Source {source.DocumentId}; pages={source.Pages}\n{source.Text}"));
```

Ready-index and no-evidence guards run **before** chat.

<p class="evidence-label verified">Live verified / existing ready index reused; no hero reanalysis</p>
<p class="source">hero\hero.Web\Services\SemanticSearch.cs / selected retrieval and evidence assembly</p>

Note:
The helper EvidenceIndex.SearchAsync implements the query/search already
shown and checks readiness. SemanticSearch refuses to chat when no evidence
is returned. Existing UI/native-source checks are separate from a successful
learned retrieval and chat answer. Show the real prompt next.

---

<!-- .slide: class="code-slide" data-workflow="answer" data-stages="answer" data-context="Retrieved passages in / actual source records out" -->
## Ask from evidence; return the records too

```csharp
ChatMessage[] messages =
[
    new(ChatRole.System, systemPrompt),
    new(ChatRole.User, $"Question: {question}\n\nRetrieved evidence:\n{evidence}")
];
var options = new ChatOptions { MaxOutputTokens = 700 };
var response = await chat.GetResponseAsync(messages, options, ct);
return new(response.Text, sources);
```

The system prompt: use retrieved excerpts, treat them as untrusted data,
preserve units, state uncertainty, and do not invent citations.

<p class="evidence-label verified">Two actual responses captured / instructions are still not correctness guarantees</p>
<p class="source">hero\hero.Web\Services\SemanticSearch.cs / GroundedAnswer carries text plus retrieved records</p>

Note:
chat is obtained from the bounded factory we just unpacked. systemPrompt is
the actual runtime instruction string summarized in the body; open the file
to see it verbatim. Returning sources exposes the retrieved context, not a
claim that every sentence has been automatically verified or attributed.

---

<!-- .slide: data-workflow="answer" data-stages="answer" data-context="Native index / actual recorded hero response" -->
## The payoff / the native-index answer

<p class="evidence-label verified">Recorded live hero response / native index / final .1</p>

> According to Table 2, the mean gas resource for the Ural Foredeep Continuous Gas AU is 35,511 billion cubic feet of gas (BCFG).

**Retrieved records: digital pages 1, 1, 2, 2.**
The numerical answer is explicit in the page-1 prose.

<p class="source">evidence\public\native-hero-answer.json and .png / actual recorded output, not a replay presented as live</p>

Note:
This is the verbatim model response, not a golden answer supplied to chat.
It used one bounded request after actual retrieval. The phrase "According
to Table2" is model wording, not independent proof of grid reasoning. The app
displayed retrieved records and page-targeted links. Full outputs and the
actual browser screenshot are retained.

---

<!-- .slide: data-workflow="answer" data-stages="answer" data-context="CU index / same answer workflow, different prepared evidence" -->
## The payoff / the CU-index answer

<p class="evidence-label verified">Recorded live hero response / CU index / final .1</p>

> According to Table 2, the mean gas resource for the Ural Foredeep Continuous Gas Assessment Unit (AU) is 35,511 billion cubic feet (BCFG).

**Retrieved: scanned page 1, digital page 1, scanned page 2, digital page 2.**
The page-2 records also contain the actual table row.

<p><a href="assets/hero-demo/watch.html" target="_blank" rel="noopener">Watch the real app / 14.5-second recording</a></p>

<p class="source">evidence\public\cu-hero-answer.json and .png / same question, separate ready index</p>

Note:
This quote is the second and final chat request of the original rehearsal,
not another native run. The video link opens a separate fresh authorized
capture: one query embedding and one chat request against the same ready CU
index, with no extraction or index rebuild. It plays without the hero server.
The fresh capture uses the restored template's suggested question. The actual
returned digital page-2 citation was clicked and its viewer scrolled to Table 2;
the closing shot is that running-app viewer, not an offline cutaway.
Waiting cuts and the cropped answer shot are disclosed. Source-card excerpts
were removed after capture; the video does not re-render later UI or saved answers.
The clip is not a real-time latency benchmark. Its transcript and usage are
in assets/hero-demo/recording.json and usage.json, separate from the original
rehearsal's receipts and counters on the following slide.
Both PDFs were already analyzed and indexed; the hero did not rebuild or
reanalyze them. Page1 prose also supports the value, so do not equate the
correct number with proof of table-grid understanding. Actual digital and
scanned page2 viewer targets were inspected separately.

---

<!-- .slide: data-workflow="overview" data-stages="none" data-context="Recorded usage / native and CU workflows" -->
## What this bounded rehearsal actually used

| Counter | Recorded result |
|---|---:|
| CU analyses / chat requests | **3 / 2** |
| Successful embedding input tokens reported | **19,030** |
| Chat tokens reported, input + output | **2,949 + 68** |

**Actual reported usage, not a currency bill or an authorization to run.**
The original private phase is complete and halted.

<p class="source">evidence\public\reported-usage.json / actual usage lines / zero automatic retries</p>

Note:
Reservations are conservative UTF-8 upper bounds, not billed token measures.
Provider-reported usage is separate; no currency cost or invoice was queried.
All three analyses and both chat slots are consumed. No defaults, resources,
RBAC or deployments were changed. No VLM or invoice-image upload was added.

---

<!-- .slide: class="code-slide" data-workflow="answer" data-stages="inspect" data-context="Application evidence check / not automatic correctness" -->
## Source links come from records, not guesses

```csharp
var url = "/Data/" + Uri.EscapeDataString(documentId);
return int.TryParse(pages, out int page) && page > 0
    ? url + "#page=" + page
    : url;
```

One known page: open that page. Unknown / multiple pages: open the document.

<p class="evidence-label verified">Actual retrieved links / digital and scanned page-2 targets physically checked</p>
<p class="source">src\Standup.Core\EvidenceIndex.cs / CitationFor, after known-fixture validation</p>

Note:
The app formats source links from retrieved records, not filenames/page
numbers proposed by the model. This final .1 live run displayed actual
retrieved links. Digital page1 and digital/scanned page2 targets were checked
physically. In the narrow source canvas the last page is shorter than the
viewport: it is fully visible at the document-end scroll clamp, not top-zero.

---

<!-- .slide: class="source-slide" data-workflow="answer" data-stages="inspect" data-context="Actual source / page-1 prose also supports the answer" -->
## Show me Table 2.

![Actual Table 2: Ural Foredeep Continuous Gas AU, gas mean 35,511 BCFG. Heading, units and rows retained.](assets/narrative/usgs-table-2.png)

**Known source answer: 35,511 BCFG. Page 2, Table 2.**

<p class="evidence-label">Actual source crop, NOT a generated answer or captured retrieval result</p>
<p class="source">USGS Fact Sheet 2018-3050 / <a href="assets/narrative/usgs-page-2.png">uncropped source page</a></p>

Note:
Pay off the opening question by checking the row and units in the source.
The same number appears in page-1 prose; returning it alone is not evidence
of table-grid understanding. The actual runs returned the correct answer and
mixed page1/page2 evidence; CU also retrieved the real Table2 row. We do not
force page2 citations or assert a reasoning path from the model's wording.

---

<!-- .slide: data-workflow="overview" data-stages="none" data-context="Composition recap / both workflows through the same index" -->
## Two workflows. An answer you can check.

**Prepare:** read, preserve sources, chunk, embed, store.

**When asked:** retrieve evidence, supply it to chat, inspect support.

Native extraction/chunking: **recorded and verified**.

CU / learned retrieval / grounded chat: **live verified within the bounded phase**.

<p class="source">Three standalone .NET 10 files, then the composed Chat-template app. Unofficial composite S0.</p>

Note:
End the main story on the app's job, not a type hierarchy. More prompt
engineering cannot replace missing extraction, and a source link is not
automatic proof of every sentence. The optional example changes the kind
of evidence, not the scope or live status of this baseline.

---

<!-- .slide: class="invoice-slide" data-workflow="overview" data-stages="none" data-context="Optional visual question / not VLM-verified" -->
## Optional: what happens when validation fails?

Is a pending invoice still created in Dynamics 365?

<div class="invoice-focus"><img src="assets/narrative/invoice-validation-workflow.png" alt="Validation branch detail: No leads to log the error, notify finance and move email to the failed folder, not invoice creation."></div>

<p class="source">Detail of adapted redraw / Microsoft Learn, CC BY 4.0 / NOT VLM-verified</p>

Note:
This viewport shows the validation portion of the unchanged full PNG; the
receive-email step and image footer are outside the crop. Follow the arrows,
not just the words. It supplements the USGS story; neither PDF is replaced.
This is source reading, not model output. Full attribution is next.

---

<!-- .slide: data-workflow="overview" data-stages="none" data-context="Optional visual source / adapted example, not a model result" -->
## The answer is in the branch, not just the labels

Source reading: **No**. Log the error, notify finance,
and move the email to the failed folder.

A future vision extraction provider needs a validated representation
and honest source associations. Raw image input is not automatically
searchable through the current text-embedding path.

**No VLM provider is implemented or live verified.**

<p class="source">Adapted simplified redraw, not original Microsoft artwork. <a href="https://learn.microsoft.com/power-platform/architecture/reference-architectures/vendor-invoice-integration">Microsoft Learn workflow</a>, <a href="https://creativecommons.org/licenses/by/4.0/">CC BY 4.0</a>. Other failure paths omitted; no endorsement implied. <a href="assets/narrative/invoice-validation-workflow.png">Full visual</a>.</p>

Note:
The source-backed expected answer is not VLM output. IChatClient can carry
images only when the chosen model/provider support them; it does not add
rasterization or extraction fidelity by itself. A future test must receive
the PNG only: keep notes, source article, SVG and golden-answer JSON out of
extraction/retrieval inputs. No invoice upload or provider expansion is
authorized, and the original budget remains halted.
