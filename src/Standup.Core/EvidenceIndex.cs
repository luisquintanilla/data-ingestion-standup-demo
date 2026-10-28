using CommunityToolkit.VectorData.SqliteVec;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Standup;

public sealed class IndexedEvidence
{
    [VectorStoreKey]
    public string Key { get; set; } = "";
    [VectorStoreData]
    public string DocumentId { get; set; } = "";
    [VectorStoreData]
    public string Text { get; set; } = "";
    [VectorStoreData]
    public string Pages { get; set; } = "";
    [VectorStoreData]
    public string Element { get; set; } = "";
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineDistance)]
    public ReadOnlyMemory<float> Vector { get; set; }

    public string Citation => CitationFor(DocumentId, Pages);

    public static string CitationFor(string documentId, string pages)
    {
        if (documentId is not ("digital.pdf" or "scanned.pdf"))
            throw new InvalidDataException("An index record references an unknown fixture.");
        var url = "/Data/" + Uri.EscapeDataString(documentId);
        return int.TryParse(pages, out int page) && page > 0 ? url + "#page=" + page : url;
    }
}

public sealed class EvidenceIndex : IDisposable
{
    public SqliteVectorStore Store { get; }
    public VectorStoreCollection<string, IndexedEvidence> Collection { get; }
    public string Name { get; }
    private readonly string _readyPath;

    public EvidenceIndex(string provider)
    {
        Name = DemoConfiguration.IndexName(provider);
        var directory = Path.Combine(DemoConfiguration.Root, ".build", "indexes");
        Directory.CreateDirectory(directory);
        _readyPath = Path.Combine(directory, Name + ".ready");
        var databasePath = Path.Combine(directory, "standup.db");
        Store = new SqliteVectorStore($"Data Source={databasePath}");
        Collection = Store.GetCollection<string, IndexedEvidence>(Name);
    }

    public void Invalidate() => File.Delete(_readyPath);
    public void MarkReady(int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        File.WriteAllText(_readyPath, count.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public bool IsReady => File.Exists(_readyPath);

    public async Task RebuildAsync(IReadOnlyList<EvidenceChunk> chunks,
        IEmbeddingGenerator<string, Embedding<float>> generator, CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0) throw new InvalidOperationException("Extraction produced no usable chunks; no index was marked ready.");
        var embeddings = await generator.GenerateAsync(chunks.Select(chunk => chunk.Text),
            new() { Dimensions = 1536 }, cancellationToken);
        if (embeddings.Count != chunks.Count || embeddings.Any(item => item.Vector.Length != 1536))
            throw new InvalidDataException("Embedding count or 1536-dimensional schema mismatch.");
        Invalidate();
        await Collection.EnsureCollectionDeletedAsync(cancellationToken);
        await Collection.EnsureCollectionExistsAsync(cancellationToken);
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            await Collection.UpsertAsync(new IndexedEvidence
            {
                Key = i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                DocumentId = chunk.DocumentId,
                Text = chunk.Text,
                Pages = string.Join(",", chunk.Pages),
                Element = chunk.Element,
                Vector = embeddings[i].Vector
            }, cancellationToken);
        }
        MarkReady(chunks.Count);
    }

    public async Task<List<IndexedEvidence>> SearchAsync(string question,
        IEmbeddingGenerator<string, Embedding<float>> generator, CancellationToken cancellationToken = default)
    {
        if (!IsReady || !await Collection.CollectionExistsAsync(cancellationToken))
            throw new InvalidOperationException($"The current provider/config index '{Name}' is missing. Build it explicitly first.");
        var query = await generator.GenerateAsync([question], new() { Dimensions = 1536 }, cancellationToken);
        Console.WriteLine("Actual query embedding usage: " + System.Text.Json.JsonSerializer.Serialize(query.Usage));
        List<IndexedEvidence> results = [];
        await foreach (var match in Collection.SearchAsync(query[0].Vector, top: 4, cancellationToken: cancellationToken))
            results.Add(match.Record);
        return results;
    }

    public void Dispose()
    {
        Collection.Dispose();
        Store.Dispose();
    }
}
