#!/usr/bin/env dotnet
#:project ../src/Standup.Core/Standup.Core.csproj
#:property PublishAot=false
#:property NoWarn=EXTEXP0019;MEAI001;MEAI002;MEAI003;MEDE0001

using Standup;
using Microsoft.Extensions.AI;

// This sample intentionally makes real embedding calls, never lexical or synthetic retrieval.
// It refuses to run until the separately approved live phase is explicitly enabled.
var budget = new LiveBudget();
using var extractor = DemoConfiguration.CreateExtractor();
using var generator = ModelClients.Embeddings(budget);
using var index = new EvidenceIndex(DemoConfiguration.Provider);
List<EvidenceChunk> chunks = [];
foreach (var fixture in new[] { "digital", "scanned" })
{
    var file = DemoConfiguration.Fixture(fixture);
    await using var input = File.OpenRead(file);
    using var document = await extractor.ExtractAsync(input, Path.GetFileName(file), "application/pdf");
    var extracted = await DocumentIngestion.ChunkAsync(document);
    Console.WriteLine($"{fixture}: {extracted.Count} actual chunks");
    chunks.AddRange(extracted);
}
if (chunks.Count == 0) throw new InvalidOperationException("No usable chunks; no index was built.");

var embeddings = await generator.GenerateAsync(chunks.Select(chunk => chunk.Text), new() { Dimensions = 1536 });
Console.WriteLine("Actual chunk embedding usage: " + System.Text.Json.JsonSerializer.Serialize(embeddings.Usage));
Console.WriteLine($"Actual learned vector[0]: dimensions={embeddings[0].Vector.Length}; first8=" +
    System.Text.Json.JsonSerializer.Serialize(embeddings[0].Vector.Span[..8].ToArray()));
index.Invalidate();
await index.Collection.EnsureCollectionDeletedAsync();
await index.Collection.EnsureCollectionExistsAsync();
for (int i = 0; i < chunks.Count; i++)
{
    var chunk = chunks[i];
    await index.Collection.UpsertAsync(new IndexedEvidence
    {
        Key = i.ToString(System.Globalization.CultureInfo.InvariantCulture),
        DocumentId = chunk.DocumentId, Text = chunk.Text, Pages = string.Join(",", chunk.Pages),
        Element = chunk.Element, Vector = embeddings[i].Vector
    });
}
index.MarkReady(chunks.Count);
var question = args.FirstOrDefault() ?? DemoConfiguration.Question;
var query = await generator.GenerateAsync([question], new() { Dimensions = 1536 });
Console.WriteLine("Actual query embedding usage: " + System.Text.Json.JsonSerializer.Serialize(query.Usage));
Console.WriteLine($"S0 | provider={DemoConfiguration.Provider} | current index={index.Name} | dimensions={query[0].Vector.Length}");
Console.WriteLine(question);
await foreach (var hit in index.Collection.SearchAsync(query[0].Vector, top: 4))
{
    Console.WriteLine($"score={hit.Score}; source={hit.Record.Citation}; element={hit.Record.Element}");
    Console.WriteLine(hit.Record.Text);
}
