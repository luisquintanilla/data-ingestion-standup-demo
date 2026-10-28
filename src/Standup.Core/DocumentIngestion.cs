using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.Extensions.Documents;
using Microsoft.ML.Tokenizers;

namespace Standup;

public sealed record EvidenceChunk(string Text, string DocumentId, int[] Pages, string Element);

public static class DocumentIngestion
{
    public const int MaxTokens = 384;
    public const int OverlapTokens = 48;
    public static Tokenizer CreateTokenizer() => TiktokenTokenizer.CreateForModel("text-embedding-3-small");

    public static DocumentTokenChunker CreateChunker()
    {
        var tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
        var options = new IngestionChunkerOptions(tokenizer)
        {
            MaxTokensPerChunk = MaxTokens,
            OverlapTokens = OverlapTokens
        };
        return new DocumentTokenChunker(options);
    }

    // Each unit is independently chunked. This deliberately preserves its known source set,
    // rather than claiming the stock token chunker propagates arbitrary element metadata.
    public static IEnumerable<(IngestionDocument Document, int[] Pages, string Element)> ReadUnits(Document document)
    {
        int index = 0;
        if (document.Content is TextContent root && !string.IsNullOrWhiteSpace(root.Text))
            yield return Unit(root.Text, [], "document-local");
        foreach (var section in document.Sections)
            foreach (var unit in Visit(section))
                yield return unit;

        IEnumerable<(IngestionDocument, int[], string)> Visit(DocumentElement element)
        {
            if (element is DocumentSection section)
            {
                if (section.Content is TextContent local && !string.IsNullOrWhiteSpace(local.Text))
                    yield return Unit(section.Text ?? local.Text, Pages(section), $"section-local-{index++}");
                foreach (var child in section.Elements)
                    foreach (var unit in Visit(child))
                        yield return unit;
            }
            else
            {
                var text = element is DocumentTableElement ? element.ToMarkdown() :
                    element.Text ?? (element.Content as TextContent)?.Text;
                if (!string.IsNullOrWhiteSpace(text))
                    yield return Unit(text, Pages(element), $"element-{index++}");
            }
        }

        (IngestionDocument, int[], string) Unit(string text, int[] pages, string identity)
        {
            var ingestion = new IngestionDocument(document.Identifier);
            var section = new IngestionDocumentSection();
            section.Elements.Add(new IngestionDocumentParagraph(text)
            {
                Text = text,
                PageNumber = pages.Length == 1 ? pages[0] : null
            });
            ingestion.Sections.Add(section);
            return (ingestion, pages, identity);
        }
    }

    private static int[] Pages(DocumentElement element)
    {
        var sources = element.Sources.AsEnumerable();
        if (element is DocumentTableElement table)
            sources = sources.Concat(table.Cells.Where(cell => cell is not null)
                .SelectMany(cell => cell!.Sources.Concat(cell.Content?.Sources ?? [])));
        return sources.Select(source => source.Page.PageNumber).Distinct().Order().ToArray();
    }

    public static async Task<List<EvidenceChunk>> ChunkAsync(Document document, CancellationToken ct = default)
    {
        var chunker = CreateChunker();
        List<EvidenceChunk> result = [];
        foreach (var unit in ReadUnits(document))
            await foreach (var chunk in chunker.ProcessAsync(unit.Document, ct))
                result.Add(new(chunk.Content, document.Identifier, unit.Pages, unit.Element));
        return result;
    }
}
