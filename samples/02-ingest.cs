#!/usr/bin/env dotnet
#:project ../src/Standup.Core/Standup.Core.csproj
#:property PublishAot=false
#:property NoWarn=EXTEXP0019;MEAI001;MEAI002;MEAI003;MEDE0001

using Standup;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.ML.Tokenizers;

var fixture = args.FirstOrDefault() ?? "digital";
var file = DemoConfiguration.Fixture(fixture);
using IDocumentExtractionClient client =
    DemoConfiguration.Provider == "pdfpig"
        ? new PdfPigExtractionClient()
        : DemoConfiguration.CreateExtractor();
await using var input = File.OpenRead(file);
using var document = await client.ExtractAsync(input, Path.GetFileName(file), "application/pdf");
var tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
var options = new IngestionChunkerOptions(tokenizer)
{
    MaxTokensPerChunk = 384,
    OverlapTokens = 48
};
var chunker = new DocumentTokenChunker(options);
int count = 0;
Console.WriteLine($"S0 | provider={DemoConfiguration.Provider} | fixture={Path.GetFileName(file)}");
Console.WriteLine($"MEDI token policy={options.MaxTokensPerChunk}/{options.OverlapTokens}; no embedding/chat calls");
foreach (var unit in DocumentIngestion.ReadUnits(document))
{
    await foreach (var chunk in chunker.ProcessAsync(unit.Document))
    {
        count++;
        // The adapter explicitly associates this unit's sources, not arbitrary metadata propagation.
        Console.WriteLine($"chunk={count}; element={unit.Element}; pages=[{string.Join(",", unit.Pages)}]; tokens={tokenizer.CountTokens(chunk.Content)}");
        Console.WriteLine(chunk.Content);
    }
}
Console.WriteLine($"actual chunks={count}; source document remains alive through consumption");
if (count == 0) Console.WriteLine("NO CHUNKS. This is a visible extraction limitation, not fallback success.");
