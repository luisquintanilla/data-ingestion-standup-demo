using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Azure.Identity;
using Microsoft.Extensions.DocumentExtraction;

namespace Standup;

public static class DemoConfiguration
{
    public const string Question = "According to Table 2, what is the mean gas resource for the Ural Foredeep Continuous Gas AU, in BCFG?";
    public static string Provider => Environment.GetEnvironmentVariable("STANDUP_PROVIDER") ?? "pdfpig";

    public static IDocumentExtractionClient CreateExtractor(string? provider = null) => (provider ?? Provider) switch
    {
        "pdfpig" => new PdfPigExtractionClient(),
        "cu" => CreateCu(),
        _ => throw new ArgumentException("STANDUP_PROVIDER must be pdfpig or cu; no fallback is configured.")
    };

    private static IDocumentExtractionClient CreateCu()
    {
        var budget = new LiveBudget();
        return new ContentUnderstandingExtractionClient(new(budget.Resource("contentUnderstanding", "endpoint")),
            new DefaultAzureCredential(), budget);
    }

    public static string Root
    {
        get
        {
            var current = new DirectoryInfo(Environment.GetEnvironmentVariable("STANDUP_ROOT") ?? Environment.CurrentDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "fixtures", "digital.pdf"))) return current.FullName;
                current = current.Parent;
            }
            throw new DirectoryNotFoundException("Run from this repository or set STANDUP_ROOT to its root.");
        }
    }

    public static string Fixture(string name) => name switch
    {
        "digital" => Path.Combine(Root, "fixtures", "digital.pdf"),
        "scanned" => Path.Combine(Root, "fixtures", "scanned.pdf"),
        _ => throw new ArgumentException("Fixture must be digital or scanned.")
    };

    public static string IndexName(string provider)
    {
        if (provider is not ("pdfpig" or "cu")) throw new ArgumentException("Invalid provider.", nameof(provider));
        var identity = string.Join("|", new[]
        {
            "s0-0ff84e7", provider, "pdfpig-0.1.15", "cu-1.1.0-prebuilt-layout", ResourceFingerprint(provider),
            $"token-{DocumentIngestion.MaxTokens}-{DocumentIngestion.OverlapTokens}", "text-embedding-3-small-1536",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Fixture("digital")))),
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Fixture("scanned"))))
        });
        return $"standup_{provider}_{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..12].ToLowerInvariant()}";
    }

    private static string ResourceFingerprint(string provider)
    {
        var path = Environment.GetEnvironmentVariable("STANDUP_BUDGET_PATH");
        if (path is null) return "live-model-not-configured";
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        var resources = JsonNode.Parse(stream)?["resources"] ??
            throw new InvalidDataException("Invalid private receipt resources.");
        var models = resources["models"] ?? throw new InvalidDataException("Missing model resource configuration.");
        return string.Join("|", models["endpoint"], models["embeddingDeployment"], models["embeddingModel"],
            models["embeddingDimensions"], provider == "cu" ? resources["contentUnderstanding"]?["endpoint"] : null,
            provider == "cu" ? resources["contentUnderstanding"]?["analyzer"] : null);
    }
}
