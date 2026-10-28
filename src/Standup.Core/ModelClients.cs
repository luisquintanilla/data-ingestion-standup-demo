using System.ClientModel.Primitives;
using System.Text;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

namespace Standup;

public static class ModelClients
{
    private static AzureOpenAIClient Create(LiveBudget budget)
    {
        var endpoint = new Uri(budget.Resource("models", "endpoint"));
        var credential = new DefaultAzureCredential();
        var options = new AzureOpenAIClientOptions
        {
            RetryPolicy = new ClientRetryPolicy(0)
        };
        return new AzureOpenAIClient(endpoint, credential, options);
    }

    public static IEmbeddingGenerator<string, Embedding<float>> Embeddings(LiveBudget budget)
    {
        if (budget.Resource("models", "embeddingModel") != "text-embedding-3-small")
            throw new InvalidOperationException("This demo requires text-embedding-3-small.");
        var client = Create(budget);
        var deployment = budget.Resource("models", "embeddingDeployment");
        IEmbeddingGenerator<string, Embedding<float>> inner =
            client.GetEmbeddingClient(deployment).AsIEmbeddingGenerator();
        return new BoundedEmbeddings(inner, budget);
    }

    public static IChatClient Chat(LiveBudget budget)
    {
        if (budget.Resource("models", "chatModel") != "gpt-4.1-mini")
            throw new InvalidOperationException("This demo requires the authorized gpt-4.1-mini deployment.");
        var client = Create(budget);
        var deployment = budget.Resource("models", "chatDeployment");
        IChatClient inner = client.GetChatClient(deployment).AsIChatClient();
        return new BoundedChat(inner, budget);
    }

    private sealed class BoundedEmbeddings(IEmbeddingGenerator<string, Embedding<float>> inner, LiveBudget budget)
        : DelegatingEmbeddingGenerator<string, Embedding<float>>(inner)
    {
        public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
        {
            var input = values.ToArray();
            if (input.Length == 0) throw new ArgumentException("Embedding input cannot be empty.", nameof(values));
            if (options?.Dimensions is not (null or 1536) || options?.ModelId is not null)
                throw new NotSupportedException("Model overrides and non-1536-dimensional embeddings are not supported.");
            // UTF-8 bytes conservatively bound cl100k token counts; reservations are never refunded.
            int upperBound = checked(input.Sum(text => Encoding.UTF8.GetByteCount(text)));
            if (input.Any(text => DocumentIngestion.CreateTokenizer().CountTokens(text) > 8191))
                throw new InvalidOperationException("An embedding input exceeds the model's per-input token bound.");
            budget.Reserve("embeddingInputTokens", upperBound, "text-embedding-3-small (UTF8 upper bound)");
            try
            {
                var result = await base.GenerateAsync(input, new() { Dimensions = 1536 }, cancellationToken);
                if (result.Count != input.Length || result.Any(embedding => embedding.Vector.Length != 1536))
                    throw new InvalidDataException("Embedding count/schema mismatch; the index was not updated.");
                return result;
            }
            catch (Exception error)
            {
                budget.HaltAfterFailure(error, "Embedding request/schema validation failed or was cancelled. Stop and inspect the original error.");
                throw;
            }
        }

        public override object? GetService(Type serviceType, object? serviceKey = null)
            => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    }

    private sealed class BoundedChat(IChatClient inner, LiveBudget budget) : DelegatingChatClient(inner)
    {
        public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (options?.Tools?.Count > 0 || options?.ModelId is not null)
                throw new NotSupportedException("The bounded payoff uses one retrieval-grounded request, without tool turns or model overrides.");
            budget.Reserve("chatModelRequestsIncludingToolTurns", 1, "gpt-4.1-mini answer");
            try { return await base.GetResponseAsync(messages, options, cancellationToken); }
            catch (Exception error)
            {
                budget.HaltAfterFailure(error, "Chat request failed or was cancelled. Stop and inspect the original error.");
                throw;
            }
        }

        public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Use one bounded GetResponseAsync call; this demo does not stream model responses.");

        public override object? GetService(Type serviceType, object? serviceKey = null)
            => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    }
}
