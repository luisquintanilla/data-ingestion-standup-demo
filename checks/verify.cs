#!/usr/bin/env dotnet
#:project ../src/Standup.Core/Standup.Core.csproj
#:property PublishAot=false
#:property NoWarn=EXTEXP0019;MEAI001;MEAI002;MEAI003;MEDE0001

using System.ClientModel.Primitives;
using System.Text.Json.Nodes;
using Azure.AI.ContentUnderstanding;
using CommunityToolkit.VectorData.SqliteVec;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Extensions.Documents;
using Standup;

int assertions = 0;
void Require(bool condition, string message)
{
    assertions++;
    if (!condition) throw new InvalidOperationException(message);
}
Console.WriteLine("OFFLINE CONTRACT CHECKS. Synthetic CU payloads / vectors below are not live service evidence.");
using var native = new PdfPigExtractionClient();
foreach (var fixture in new[] { "digital", "scanned" })
{
    await using var input = File.OpenRead(DemoConfiguration.Fixture(fixture));
    using var document = await native.ExtractAsync(input, fixture + ".pdf", "application/pdf",
        new() { CaptureRawContent = true });
    Require(document.SourcePages.Count == 2, $"{fixture}: expected two real source pages.");
    Require(input.CanRead, "Extractor disposed caller input.");
    Require(document.SourceOutputs.Single().ContentStatus == DocumentRawContentStatus.Unavailable,
        "Native parser manufactured raw response content.");
    var chunks = await DocumentIngestion.ChunkAsync(document);
    if (fixture == "scanned")
    {
        Require(document.EnumerateContent().Count() == 0 && chunks.Count == 0, "Scanned native path invented usable text.");
    }
    else
    {
        Require(chunks.Count == 12, "Pinned digital fixture/chunk policy changed.");
        Require(chunks.Any(chunk => chunk.Pages.SequenceEqual([2]) && chunk.Text.Contains("35,511")),
            "Known Table 2 answer lost its genuine page-2 association.");
        foreach (var chunk in chunks)
        {
            Require(DocumentIngestion.CreateTokenizer().CountTokens(chunk.Text) <= 384, "Chunk exceeds exact token maximum.");
            Require(chunk.Pages.Length == 1 && chunk.Pages[0] is 1 or 2, "Native chunk lost page source.");
        }
        Require(document.Content is null && document.Sections.All(section => section.Content is null),
            "Native provider stored ancestor aggregate content.");
    }
}
await using (var input = File.OpenRead(DemoConfiguration.Fixture("digital")))
{
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    try { using var unexpected = await native.ExtractAsync(input, "digital.pdf", "application/pdf", cancellationToken: cancellation.Token); Require(false, "Cancellation ignored."); }
    catch (OperationCanceledException) { Require(input.CanRead, "Cancellation disposed caller stream."); }
}

var bytes = BinaryData.FromString("""
{
  "analyzerId":"prebuilt-layout",
  "contents":[{
    "kind":"document","startPageNumber":1,"endPageNumber":2,"markdown":"First.\nSecond.",
    "pages":[
      {"pageNumber":1,"width":8.5,"height":11,"spans":[{"offset":0,"length":6}]},
      {"pageNumber":2,"width":8.5,"height":11,"spans":[{"offset":7,"length":7}]}
    ],
    "paragraphs":[
      {"content":"First.","span":{"offset":0,"length":6}},
      {"content":"Second.","span":{"offset":7,"length":7}}
    ]
  }]
}
""");
var analysis = ModelReaderWriter.Read<AnalysisResult>(bytes)!;
Stream retained;
using (var document = ContentUnderstandingExtractionClient.Map(analysis, "digital.pdf", true, bytes))
{
    Require(document.SourcePages.Count == 2, "CU mapping dropped source frames.");
    Require(document.EnumerateContent().Select(element => element.Text).SequenceEqual(["First.", "Second."]),
        "CU mapping replayed aggregate Markdown or lost local text.");
    Require(document.EnumerateContent().Last().Sources.Single().Page.PageNumber == 2, "CU mapping invented first-page provenance.");
    Require(document.SourcePages.All(page => page.Dimensions is null && page.CoordinateUnit is null),
        "CU mapping invented geometry instead of leaving unknown frame details absent.");
    retained = document.SourceOutputs.Single().GetRawContent()!.Content;
    using var copy = new MemoryStream();
    retained.CopyTo(copy);
    Require(copy.ToArray().SequenceEqual(bytes.ToArray()), "Raw capture did not retain original response bytes.");
}
Require(!retained.CanRead, "S0 document did not release registered raw-content stream.");
var emptyAnalysis = ModelReaderWriter.Read<AnalysisResult>(BinaryData.FromString("""{"contents":[]}"""))!;
using (var document = ContentUnderstandingExtractionClient.Map(emptyAnalysis, "digital.pdf", true, BinaryData.FromBytes([])))
{
    Require(document.SourceOutputs.Single().ContentStatus == DocumentRawContentStatus.Retained &&
        document.SourceOutputs.Single().GetRawContent()!.Content.Length == 0, "Zero-byte original response capture lost its retained state.");
}
using (var document = ContentUnderstandingExtractionClient.Map(emptyAnalysis, "digital.pdf", true))
    Require(document.SourceOutputs.Single().ContentStatus == DocumentRawContentStatus.Unavailable, "Unavailable raw content confused with disabled capture.");
using (var document = ContentUnderstandingExtractionClient.Map(emptyAnalysis, "digital.pdf"))
    Require(document.SourceOutputs.Single().ContentStatus == DocumentRawContentStatus.Disabled, "Disabled raw capture state lost.");

var unlocated = ModelReaderWriter.Read<AnalysisResult>(BinaryData.FromString("""
{"contents":[{"kind":"document","startPageNumber":1,"endPageNumber":1,"markdown":"Unlocated document-local content."}]}
"""))!;
using (var document = ContentUnderstandingExtractionClient.Map(unlocated, "digital.pdf"))
{
    Require(document.SourcePages.Count == 0, "Unlocated aggregate invented page 1.");
    Require(document.EnumerateContent().Single().Sources.Count == 0, "Unlocated content received false page attribution.");
}
var tablePayload = ModelReaderWriter.Read<AnalysisResult>(BinaryData.FromString("""
{"contents":[{"kind":"document","startPageNumber":2,"endPageNumber":2,"markdown":"A B C D",
"pages":[{"pageNumber":2,"spans":[{"offset":0,"length":7}]}],
"paragraphs":[{"content":"A B C D","span":{"offset":0,"length":7}}],
"tables":[{"rowCount":2,"columnCount":2,"span":{"offset":0,"length":7},"cells":[
{"rowIndex":0,"columnIndex":0,"content":"A","kind":"columnHeader","span":{"offset":0,"length":1}},
{"rowIndex":0,"columnIndex":1,"content":"B","kind":"columnHeader","span":{"offset":2,"length":1}},
{"rowIndex":1,"columnIndex":0,"content":"C","span":{"offset":4,"length":1}},
{"rowIndex":1,"columnIndex":1,"content":"D","span":{"offset":6,"length":1}}]}]}]}
"""))!;
using (var document = ContentUnderstandingExtractionClient.Map(tablePayload, "digital.pdf"))
{
    var table = (DocumentTableElement)document.EnumerateContent().Single();
    Require(table.Cells.RowCount == 2 && table.Cells.ColumnCount == 2, "CU typed table grid lost.");
    Require(table.Cells[1, 1]?.Content?.Text == "D", "CU cell addressing changed.");
    Require(table.Sources.Single().Page.PageNumber == 2, "CU table page provenance lost.");
    Require(document.ToMarkdown().Split('D').Length == 2, "CU table content was replayed.");
}
var withoutParagraphs = JsonNode.Parse(ModelReaderWriter.Write(tablePayload).ToString())!;
withoutParagraphs["contents"]![0]!.AsObject().Remove("paragraphs");
var tableOnlyPayload = ModelReaderWriter.Read<AnalysisResult>(BinaryData.FromString(withoutParagraphs.ToJsonString()))!;
using (var document = ContentUnderstandingExtractionClient.Map(tableOnlyPayload, "digital.pdf"))
{
    Require(document.EnumerateContent().Single() is DocumentTableElement, "CU table-only response was flattened into aggregate Markdown.");
    var chunks = await DocumentIngestion.ChunkAsync(document);
    Require(chunks.Count == 1 && chunks[0].Pages.SequenceEqual([2]), "CU table-only chunk lost page-2 evidence.");
}

var vector = new float[1536];
vector[0] = 1;
using (var store = new SqliteVectorStore($"Data Source={Path.Combine(DemoConfiguration.Root, ".build", "offline-schema-check.db")}"))
using (var collection = store.GetCollection<string, IndexedEvidence>("offline_schema_only"))
{
    await collection.EnsureCollectionDeletedAsync();
    await collection.EnsureCollectionExistsAsync();
    await collection.UpsertAsync(new IndexedEvidence
    {
        Key = "synthetic-schema-only", DocumentId = "digital.pdf", Pages = "2",
        Text = "Synthetic vector schema check; not learned retrieval evidence.", Vector = vector
    });
    var results = new List<IndexedEvidence>();
    await foreach (var hit in collection.SearchAsync((ReadOnlyMemory<float>)vector, top: 1)) results.Add(hit.Record);
    Require(results.Count == 1 && results[0].Key == "synthetic-schema-only", "Actual SQLite VectorData roundtrip failed.");
    Require(results[0].Citation == "/Data/digital.pdf#page=2", "Page-2 citation target incorrect.");
    Require(IndexedEvidence.CitationFor("digital.pdf", "1,2") == "/Data/digital.pdf", "Multi-page unit invented a single-page target.");
    await collection.EnsureCollectionDeletedAsync();
}
Require(DemoConfiguration.IndexName("pdfpig") != DemoConfiguration.IndexName("cu"), "Providers share an index identity.");
Require(Environment.GetEnvironmentVariable("STANDUP_LIVE") != "1", "Offline checks must not enable live calls.");
try { _ = new LiveBudget(); Require(false, "Live gate default was not enforced."); }
catch (InvalidOperationException exception) when (exception.Message.StartsWith("Live calls are disabled.")) { assertions++; }

// A synthetic ledger with NO resource endpoints exercises accounting only.
// The authoritative private receipt is never opened for writing or replaced.
var syntheticReceipt = Path.Combine(DemoConfiguration.Root, ".build", "synthetic-budget-check.json");
File.WriteAllText(syntheticReceipt, """
{"authorized":true,"maximums":{"contentUnderstandingAnalyses":100,"chatModelRequestsIncludingToolTurns":100,"embeddingInputTokens":100000},
"used":{"contentUnderstandingAnalyses":0,"chatModelRequestsIncludingToolTurns":0,"embeddingInputTokens":0},"attempts":[]}
""");
var previousPath = Environment.GetEnvironmentVariable("STANDUP_BUDGET_PATH");
try
{
    Environment.SetEnvironmentVariable("STANDUP_LIVE", "1");
    Environment.SetEnvironmentVariable("STANDUP_BUDGET_PATH", syntheticReceipt);
    var budget = new LiveBudget();
    budget.Reserve("embeddingInputTokens", 17, "OFFLINE SYNTHETIC accounting check, no request");
    Require(JsonNode.Parse(File.ReadAllText(syntheticReceipt))!["used"]!["embeddingInputTokens"]!.GetValue<int>() == 17,
        "Reservation was not persisted before a request.");
    try { budget.Reserve("chatModelRequestsIncludingToolTurns", 7, "OFFLINE SYNTHETIC cap check"); Require(false, "Hard chat cap exceeded."); }
    catch (InvalidOperationException exception) when (exception.Message.StartsWith("Budget exhausted")) { assertions++; }
    Require(JsonNode.Parse(File.ReadAllText(syntheticReceipt))!["used"]!["chatModelRequestsIncludingToolTurns"]!.GetValue<int>() == 0,
        "Rejected reservation changed usage.");
    budget.HaltAfterFailure(new IOException("Synthetic failure; no request made"), "OFFLINE SYNTHETIC halt check");
    try { budget.Reserve("contentUnderstandingAnalyses", 1, "OFFLINE SYNTHETIC halted check"); Require(false, "Persistent halt was ignored."); }
    catch (InvalidOperationException exception) when (exception.Message.StartsWith("Live work is halted")) { assertions++; }
}
finally
{
    Environment.SetEnvironmentVariable("STANDUP_LIVE", null);
    Environment.SetEnvironmentVariable("STANDUP_BUDGET_PATH", previousPath);
    File.Delete(syntheticReceipt);
    File.Delete(syntheticReceipt + ".lock");
}
Console.WriteLine($"PASS: {assertions} offline assertions; actual PdfPig/MEDI/S0 types and SQLite schema exercised; zero model/CU calls.");
