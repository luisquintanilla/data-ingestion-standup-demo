using Microsoft.Extensions.AI;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Extensions.Documents;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

namespace Standup;

public sealed class PdfPigExtractionClient : IDocumentExtractionClient
{
    public async Task<Document> ExtractAsync(Stream document, string identifier, string mediaType,
        DocumentExtractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var request = options?.Clone();
        if (mediaType != "application/pdf")
            throw new NotSupportedException("The native client accepts application/pdf only.");
        if (request?.ModelId is not null || request?.AdditionalProperties.Count > 0)
            throw new NotSupportedException("The native client has no model or adapter-specific overrides.");

        using var buffer = new MemoryStream();
        await document.CopyToAsync(buffer, cancellationToken);
        using var pdf = PdfDocument.Open(buffer.ToArray());
        FixturePolicy.CheckPageCount(pdf.NumberOfPages);
        var output = new DocumentSourceOutput("pdfpig-native", providerName: "PdfPig 0.1.15",
            contentCaptureDisabled: request?.CaptureRawContent != true);
        var result = new Document(identifier);
        try
        {
            result.AddSourceOutput(output);
            for (int number = 1; number <= pdf.NumberOfPages; number++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var page = pdf.GetPage(number);
                var source = new DocumentSourcePage($"page-{number}", number,
                    new DocumentPageDimensions((double)page.Width, (double)page.Height),
                    DocumentCoordinateUnit.Point, DocumentCoordinateOrigin.BottomLeft, output);
                result.AddSourcePage(source);
                var section = new DocumentSection();
                section.Sources.Add(new(source));
                foreach (var block in DefaultPageSegmenter.Instance.GetBlocks(page.GetWords()))
                {
                    if (string.IsNullOrWhiteSpace(block.Text)) continue;
                    var content = new TextContent(MarkdownLiteral.Escape(block.Text));
                    var element = new DocumentTextElement(content, block.Text);
                    var box = block.BoundingBox;
                    element.Sources.Add(new(source,
                    [
                        new(box.Left, box.Bottom), new(box.Right, box.Bottom),
                        new(box.Right, box.Top), new(box.Left, box.Top)
                    ]));
                    section.Elements.Add(element);
                }
                result.Sections.Add(section);
            }
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }
}

internal static class MarkdownLiteral
{
    public static string Escape(string text)
    {
        foreach (char character in @"\`*_{}[]<>()#+-.!|")
            text = text.Replace(character.ToString(), "\\" + character, StringComparison.Ordinal);
        return text;
    }
}

public static class FixturePolicy
{
    public static void CheckPageCount(int count)
    {
        if (count is < 1 or > 4)
            throw new InvalidOperationException($"Demo fixtures must have 1-4 pages; found {count}.");
    }

    public static async Task<byte[]> ReadPdfAsync(Stream input, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > 10 * 1024 * 1024)
            throw new InvalidOperationException("This bounded demo accepts PDF fixtures up to 10 MiB.");
        var bytes = buffer.ToArray();
        using var pdf = PdfDocument.Open(bytes);
        CheckPageCount(pdf.NumberOfPages);
        return bytes;
    }
}
