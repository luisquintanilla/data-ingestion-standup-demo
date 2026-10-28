#!/usr/bin/env dotnet
#:project ../src/Standup.Core/Standup.Core.csproj
#:property PublishAot=false
#:property NoWarn=EXTEXP0019;MEAI001;MEAI002;MEAI003;MEDE0001

using Standup;
using Microsoft.Extensions.DocumentExtraction;

var fixture = args.FirstOrDefault() ?? "digital";
var file = DemoConfiguration.Fixture(fixture);
using IDocumentExtractionClient client =
    DemoConfiguration.Provider == "pdfpig"
        ? new PdfPigExtractionClient()
        : DemoConfiguration.CreateExtractor();
await using var input = File.OpenRead(file);
using var document = await client.ExtractAsync(input, Path.GetFileName(file), "application/pdf");

Console.WriteLine($"S0 | provider={DemoConfiguration.Provider} | fixture={Path.GetFileName(file)}");
Console.WriteLine($"source pages={document.SourcePages.Count}; content elements={document.EnumerateContent().Count()}");
foreach (var page in document.SourcePages)
    Console.WriteLine($"source={page.Identifier}; page={page.PageNumber}; unit={page.CoordinateUnit}; origin={page.CoordinateOrigin}");
foreach (var element in document.EnumerateContent())
    Console.WriteLine($"{element.GetType().Name} | sources=[{string.Join(",", element.Sources.Select(s => s.Page.PageNumber))}] | {element.Text}");

if (string.IsNullOrWhiteSpace(document.ToMarkdown()))
    Console.WriteLine("NO USABLE TEXT. PdfPig is not OCR. There is no automatic cloud fallback.");
else
{
    Console.WriteLine("--- actual S0 Markdown projection ---");
    Console.WriteLine(document.ToMarkdown());
}
Console.WriteLine($"caller-owned input still readable={input.CanRead}");
