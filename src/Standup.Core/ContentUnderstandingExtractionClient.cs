using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.Core;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Extensions.Documents;
using Document = Microsoft.Extensions.Documents.Document;
using SourceTable = Azure.AI.ContentUnderstanding.DocumentTable;

namespace Standup;

public sealed class ContentUnderstandingExtractionClient : IDocumentExtractionClient
{
    private readonly Azure.AI.ContentUnderstanding.ContentUnderstandingClient _client;
    private readonly LiveBudget _budget;

    public ContentUnderstandingExtractionClient(Uri endpoint, TokenCredential credential, LiveBudget budget)
    {
        _budget = budget;
        budget.CheckEndpoint("contentUnderstanding", endpoint);
        var options = new ContentUnderstandingClientOptions();
        options.Retry.MaxRetries = 0;
        _client = new(endpoint, credential, options);
    }

    public async Task<Document> ExtractAsync(Stream document, string identifier, string mediaType,
        DocumentExtractionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var request = options?.Clone();
        if (mediaType != "application/pdf")
            throw new NotSupportedException("This bounded CU demo accepts application/pdf only.");
        if (request?.ModelId is not null and not "prebuilt-layout" || request?.AdditionalProperties.Count > 0)
            throw new NotSupportedException("This demo allows prebuilt-layout only, without adapter overrides.");
        var bytes = await FixturePolicy.ReadPdfAsync(document, cancellationToken);
        _budget.CheckFixture(bytes);
        _budget.Reserve("contentUnderstandingAnalyses", 1, "CU prebuilt-layout");
        // No automatic fallback or retries: analyzer/configuration failures remain visible.
        try
        {
            Operation<AnalysisResult> operation = await _client.AnalyzeBinaryAsync(
                WaitUntil.Completed, "prebuilt-layout", BinaryData.FromBytes(bytes),
                contentRange: null, contentType: mediaType, processingLocation: null,
                cancellationToken: cancellationToken);
            using var response = operation.GetRawResponse();
            return Map(operation.Value, identifier, request?.CaptureRawContent == true,
                request?.CaptureRawContent == true ? response.Content : null);
        }
        catch (Exception error)
        {
            _budget.HaltAfterFailure(error, "CU analysis/mapping failed or was cancelled. Stop and inspect the original error; no configuration changes or automatic retry.");
            throw;
        }
    }

    public static Document Map(AnalysisResult analysis, string identifier, bool captureRawContent = false,
        BinaryData? originalResponse = null)
    {
        var result = new Document(identifier);
        try
        {
            var raw = captureRawContent && originalResponse is not null
                ? new DocumentArtifact("application/json", new MemoryStream(originalResponse.ToArray(), writable: false))
                : null;
            var output = new DocumentSourceOutput("cu-layout", analysis, raw,
                contentCaptureDisabled: !captureRawContent,
                providerName: "Azure Content Understanding", modelId: "prebuilt-layout");
            result.AddSourceOutput(output);
            int contentIndex = 0;
            foreach (var content in analysis.Contents)
            {
                if (content is not DocumentContent native) continue;
                var pages = new List<(Azure.AI.ContentUnderstanding.DocumentPage Native, DocumentSourcePage Source)>();
                foreach (var page in native.Pages)
                {
                    var source = new DocumentSourcePage($"content-{contentIndex}-page-{page.PageNumber}", page.PageNumber,
                        sourceOutput: output);
                    result.AddSourcePage(source);
                    pages.Add((page, source));
                }

                var section = new Microsoft.Extensions.Documents.DocumentSection();
                // Paragraph/table spans refer to this content's markdown. No ancestor aggregate is replayed.
                if (native.Paragraphs.Count > 0 || native.Tables.Count > 0)
                {
                    var elements = new List<(int Order, DocumentElement Element)>();
                    if (native.Tables.Any(table => table.Span is null) &&
                        (native.Paragraphs.Count > 0 || !string.IsNullOrWhiteSpace(native.Markdown)))
                        throw new InvalidDataException("CU tables lack spans needed to separate local text from aggregate Markdown without duplication.");
                    foreach (var paragraph in native.Paragraphs)
                    {
                        if (native.Tables.Count > 0 && paragraph.Span is null)
                            throw new InvalidDataException("CU paragraph/table partition requires explicit paragraph spans.");
                        var overlaps = native.Tables.Where(table => Overlaps(table.Span, paragraph.Span)).ToArray();
                        if (overlaps.Length > 0)
                        {
                            if (overlaps.Any(table => table.Span!.Offset <= paragraph.Span!.Offset &&
                                (long)table.Span.Offset + table.Span.Length >= (long)paragraph.Span.Offset + paragraph.Span.Length))
                                continue;
                            throw new InvalidDataException("CU paragraph/table spans partially overlap; local text cannot be partitioned safely.");
                        }
                        if (string.IsNullOrWhiteSpace(paragraph.Content)) continue;
                        var element = new DocumentTextElement(new TextContent(MarkdownLiteral.Escape(paragraph.Content)),
                            paragraph.Content, paragraph.Role?.ToString());
                        Associate(element, paragraph.Span, pages);
                        elements.Add((paragraph.Span?.Offset ?? int.MaxValue, element));
                    }
                    foreach (var table in native.Tables)
                    {
                        var element = MapTable(table, pages);
                        elements.Add((table.Span?.Offset ?? int.MaxValue, element));
                    }
                    if (native.Paragraphs.Count == 0 && !string.IsNullOrWhiteSpace(native.Markdown))
                    {
                        int previousEnd = 0;
                        foreach (var table in native.Tables.OrderBy(table => table.Span!.Offset))
                        {
                            var span = table.Span!;
                            if (span.Offset < previousEnd || span.Offset + span.Length > native.Markdown.Length)
                                throw new InvalidDataException("CU returned overlapping or out-of-range table spans.");
                            AddResidual(previousEnd, span.Offset - previousEnd);
                            previousEnd = span.Offset + span.Length;
                        }
                        AddResidual(previousEnd, native.Markdown.Length - previousEnd);

                        void AddResidual(int offset, int length)
                        {
                            var markdown = native.Markdown.Substring(offset, length);
                            if (string.IsNullOrWhiteSpace(markdown)) return;
                            var element = new DocumentTextElement(new TextContent(markdown), markdown);
                            Associate(element, offset, length, pages);
                            elements.Add((offset, element));
                        }
                    }
                    foreach (var item in elements.OrderBy(item => item.Order))
                        section.Elements.Add(item.Element);
                }
                else
                {
                    // If structured paragraphs are absent, page spans may still provide genuine page-local Markdown.
                    var markdown = native.Markdown ?? "";
                    var spans = pages.SelectMany(page => page.Native.Spans.Select(span => (page.Source, Span: span)))
                        .OrderBy(item => item.Span.Offset).ToArray();
                    if (spans.Length > 0)
                    {
                        int previousEnd = 0;
                        foreach (var item in spans)
                        {
                            if (item.Span.Offset < previousEnd || item.Span.Offset + item.Span.Length > markdown.Length)
                                throw new InvalidDataException("CU returned overlapping or out-of-range page spans.");
                            AddText(markdown[previousEnd..item.Span.Offset], null);
                            AddText(markdown.Substring(item.Span.Offset, item.Span.Length), item.Source);
                            previousEnd = item.Span.Offset + item.Span.Length;
                        }
                        AddText(markdown[previousEnd..], null);
                    }
                    else AddText(markdown, null);
                }
                if (section.Elements.Count > 0) result.Sections.Add(section);
                contentIndex++;

                void AddText(string markdown, DocumentSourcePage? page)
                {
                    if (string.IsNullOrWhiteSpace(markdown)) return;
                    var element = new DocumentTextElement(new TextContent(markdown), markdown);
                    if (page is not null) element.Sources.Add(new(page));
                    section.Elements.Add(element);
                }
            }
            if (contentIndex == 0 && analysis.Contents.Count > 0)
                throw new InvalidDataException("CU returned no supported document content.");
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    private static DocumentTableElement MapTable(SourceTable table,
        List<(Azure.AI.ContentUnderstanding.DocumentPage Native, DocumentSourcePage Source)> pages)
    {
        var result = new DocumentTableElement(null, table.RowCount, table.ColumnCount);
        Associate(result, table.Span, pages);
        foreach (var cell in table.Cells)
        {
            var text = new DocumentTextElement(new TextContent(MarkdownLiteral.Escape(cell.Content ?? "")), cell.Content);
            Associate(text, cell.Span, pages);
            result.Cells[cell.RowIndex, cell.ColumnIndex] = new Microsoft.Extensions.Documents.DocumentTableCell(
                text, cell.RowSpan ?? 1, cell.ColumnSpan ?? 1, cell.Kind?.ToString());
        }
        return result;
    }

    private static bool Overlaps(ContentSpan? left, ContentSpan? right)
        => left is not null && right is not null &&
            left.Offset < right.Offset + right.Length && right.Offset < left.Offset + left.Length;

    private static void Associate(DocumentElement element, ContentSpan? span,
        List<(Azure.AI.ContentUnderstanding.DocumentPage Native, DocumentSourcePage Source)> pages)
    {
        if (span is not null) Associate(element, span.Offset, span.Length, pages);
    }

    private static void Associate(DocumentElement element, int offset, int length,
        List<(Azure.AI.ContentUnderstanding.DocumentPage Native, DocumentSourcePage Source)> pages)
    {
        foreach (var page in pages)
            if (page.Native.Spans.Any(pageSpan => pageSpan.Offset < (long)offset + length &&
                offset < (long)pageSpan.Offset + pageSpan.Length))
                element.Sources.Add(new(page.Source));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is not null ? null : serviceType.IsInstanceOfType(this) ? this :
            serviceType.IsInstanceOfType(_client) ? _client : null;

    public void Dispose() { }
}
