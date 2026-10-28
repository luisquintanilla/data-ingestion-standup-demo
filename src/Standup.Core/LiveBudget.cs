using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Standup;

public sealed class LiveBudget
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public LiveBudget()
    {
        if (Environment.GetEnvironmentVariable("STANDUP_LIVE") != "1")
            throw new InvalidOperationException("Live calls are disabled. The live phase needs separate approval; see RUNBOOK.md.");
        _path = Path.GetFullPath(Environment.GetEnvironmentVariable("STANDUP_BUDGET_PATH") ??
            throw new InvalidOperationException("STANDUP_BUDGET_PATH must identify the authoritative private cumulative receipt."));
        if (Read()["authorized"]?.GetValue<bool>() != true)
            throw new InvalidOperationException("The private receipt does not authorize this run.");
    }

    private JsonObject Read()
    {
        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        return JsonNode.Parse(stream)?.AsObject() ?? throw new InvalidDataException("Invalid private budget receipt.");
    }

    public string Resource(string group, string key) =>
        Read()["resources"]?[group]?[key]?.GetValue<string>() ??
        throw new InvalidDataException($"Missing receipt resource field {group}/{key}.");

    public void CheckEndpoint(string group, Uri endpoint)
    {
        if (endpoint != new Uri(Resource(group, "endpoint")) || endpoint.Scheme != "https")
            throw new InvalidOperationException("The endpoint differs from the authorized private resource.");
    }

    public void CheckFixture(byte[] bytes)
    {
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        if (hash is not ("8B9C6F80F2C957EC8AE8D7074E70FE40E9C682FC0D9A9BC70571A2E5B16F7306" or
            "90A4F734F3001F39F8907EC024EC84A011648A8CACFE8F2099D3438C0367FA66"))
            throw new InvalidOperationException("Only the two pinned public demo fixtures are authorized for upload.");
    }

    public void Reserve(string counter, int amount, string operation)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        // Exclusive sidecar lock spans read/check/atomic replace across processes.
        // Contention is an explicit error, never a request retry.
        using var guard = new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var receipt = Read();
        if (receipt["authorized"]?.GetValue<bool>() != true || receipt["halted"]?.GetValue<bool>() == true)
            throw new InvalidOperationException("Live work is halted or no longer authorized. Inspect the private receipt; do not retry.");
        int used = receipt["used"]?[counter]?.GetValue<int>() ??
            throw new InvalidDataException($"Missing budget counter {counter}.");
        int maximum = receipt["maximums"]?[counter]?.GetValue<int>() ??
            throw new InvalidDataException($"Missing budget maximum {counter}.");
        if (used < 0 || maximum < 0)
            throw new InvalidDataException("Budget counts and limits must be nonnegative.");
        int hardLimit = counter switch
        {
            "contentUnderstandingAnalyses" => 4,
            "chatModelRequestsIncludingToolTurns" => 6,
            "embeddingInputTokens" => 100000,
            _ => throw new InvalidDataException("Unknown live budget counter.")
        };
        maximum = Math.Min(maximum, hardLimit);
        if (amount > maximum - used)
            throw new InvalidOperationException($"Budget exhausted for {counter}: {used}/{maximum}; requested {amount}.");
        receipt["used"]![counter] = used + amount;
        receipt["attempts"]!.AsArray().Add(new JsonObject
        {
            ["time"] = DateTimeOffset.UtcNow.ToString("O"),
            ["operation"] = operation,
            ["counter"] = counter,
            ["reserved"] = amount,
            ["state"] = "reserved-before-call; retained on failure"
        });
        Write(receipt);
    }

    public void HaltAfterFailure(Exception originalError, string reason)
    {
        try
        {
            using var guard = new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var receipt = Read();
            receipt["halted"] = true;
            receipt["haltReason"] = reason;
            Write(receipt);
        }
        catch (Exception receiptError)
        {
            throw new AggregateException("The live request failed and its persistent halt could not be recorded. Stop all live work.",
                originalError, receiptError);
        }
    }

    private void Write(JsonObject receipt)
    {
        var temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temporary, receipt.ToJsonString(JsonOptions), new UTF8Encoding(false));
        File.Move(temporary, _path, overwrite: true);
    }
}
