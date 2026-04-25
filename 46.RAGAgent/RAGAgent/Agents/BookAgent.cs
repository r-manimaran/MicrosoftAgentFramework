using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI;
using OpenAI.Chat;

namespace RAGAgent.Agents;

public class BookAgent
{
    public const int EmbeddingDimensions = 3072;
    public const string CollectionName = "books-tips";

    // ✅ expose for CostCenter construction in Program.cs
    public string ChatModel { get; private set; } = string.Empty;

    private readonly TextSearchStore _textSearchStore;

    public BookAgent(OpenAIClient openAIClient, string embeddingModel)
    {
        var vectorStore = new InMemoryVectorStore(new()
        {
            EmbeddingGenerator = openAIClient.GetEmbeddingClient(embeddingModel)
                                             .AsIEmbeddingGenerator()
        });
        _textSearchStore = new TextSearchStore(vectorStore, CollectionName, EmbeddingDimensions);
    }

    public async Task<AIAgent> CreateAgentAsync(OpenAIClient openAIClient, string model)
    {
        ChatModel = model; // ✅ store for CostCenter

        await _textSearchStore.UpsertDocumentAsync(TextSearchStore.GetSampleDocuments());

        var textSearchOptions = new TextSearchProviderOptions
        {
            SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
            CitationsPrompt = """
                Always cite sources at the end of your response using the format: **Source:**[SourceName](SourceLink)
            """
        };
        var textSearchProvider = new TextSearchProvider(SearchAsync,textSearchOptions);

        return openAIClient.GetChatClient(model)
            .AsAIAgent(new ChatClientAgentOptions
            {
                ChatOptions = new()
                {
                    Instructions = """
                        You are an expert book advisor and reading coach.
                        Your knowledge covers:
                        - Practical reading tips (speed reading, active reading, building habits)
                        - Genre introductions and recommendations for new readers
                        - Key insights and summaries for popular non-fiction titles

                        Guidelines:
                        • Be concise but specific — name actual techniques or book titles.
                        • If a user seems like a beginner, suggest accessible entry points.
                        • If a user asks for a book summary, share 3–5 key insights.
                        • Always cite the source documents provided to you.
                        • If you don't have enough information, say so honestly.
                        """
                },
               AIContextProviders = [textSearchProvider],
               ChatHistoryProvider = new InMemoryChatHistoryProvider()
            });
            
    }

    private async Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAsync(string text, CancellationToken ct)
    {
        var searchResults = await _textSearchStore.SearchAsync(text, 2, ct);

        return searchResults.Select(r => new TextSearchProvider.TextSearchResult
        {
            SourceName = r.SourceName,
            SourceLink = r.SourceLink,
            Text = r.Text,
            RawRepresentation = r
        });
    }
}
