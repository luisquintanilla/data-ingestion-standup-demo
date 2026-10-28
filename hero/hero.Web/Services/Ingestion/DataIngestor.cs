using Standup;

namespace hero.Web.Services.Ingestion;

public sealed class DataIngestor
{
    private readonly SemaphoreSlim _buildLock = new(1, 1);
    public string Provider => DemoConfiguration.Provider;

    public async Task<List<EvidenceChunk>> InspectAsync(CancellationToken ct = default)
    {
        using var extractor = DemoConfiguration.CreateExtractor();
        List<EvidenceChunk> chunks = [];
        foreach (var fixture in new[] { "digital", "scanned" })
        {
            var file = DemoConfiguration.Fixture(fixture);
            await using var input = File.OpenRead(file);
            using var document = await extractor.ExtractAsync(input, Path.GetFileName(file), "application/pdf", cancellationToken: ct);
            chunks.AddRange(await DocumentIngestion.ChunkAsync(document, ct));
        }
        return chunks;
    }

    public async Task BuildIndexAsync(CancellationToken ct = default)
    {
        if (!await _buildLock.WaitAsync(0, ct))
            throw new InvalidOperationException("An index build is already in progress.");
        try
        {
            var budget = new LiveBudget();
            using var generator = ModelClients.Embeddings(budget);
            var chunks = await InspectAsync(ct);
            using var index = new EvidenceIndex(Provider);
            await index.RebuildAsync(chunks, generator, ct);
        }
        finally { _buildLock.Release(); }
    }
}
