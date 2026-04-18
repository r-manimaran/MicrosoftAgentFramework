using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentWithMemory_04_BoundedChatHistory;

internal sealed class BoundedChatHistoryProvider : ChatHistoryProvider, IDisposable
{
    private readonly InMemoryChatHistoryProvider _chatHistoryProvider;
    private readonly ChatHistoryMemoryProvider _memoryProvider;
    private readonly TruncatingChatReducer _reducer;
    private readonly string _contextPrompt;
    private IReadOnlyList<string>? _stateKeys;

    public BoundedChatHistoryProvider(int maxSessionMessages,
        VectorStore vectorStore,
        string collectionName,
        int vectorDimensions,
        Func<AgentSession?, ChatHistoryMemoryProvider.State> stateInitializer,
        string? contextPrompt = null)
    {
        if(maxSessionMessages < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSessionMessages), "maxSessionMessages must be non-negative");
        }

        _reducer = new TruncatingChatReducer(maxSessionMessages);
        _chatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
        {
            ChatReducer = _reducer,
            ReducerTriggerEvent = InMemoryChatHistoryProviderOptions.ChatReducerTriggerEvent.AfterMessageAdded,
            StorageInputRequestMessageFilter = msgs => msgs,
        });
        _memoryProvider = new ChatHistoryMemoryProvider(
            vectorStore,
            collectionName,
            vectorDimensions,
            stateInitializer,
            options: new ChatHistoryMemoryProviderOptions
            {
                SearchInputMessageFilter = msgs => msgs,
                StorageInputRequestMessageFilter = msgs => msgs,
            });
        _contextPrompt = contextPrompt ??
            "The following are memories from earlier in this conversation. Use them to inform your responses:";
    }

    public override IReadOnlyList<string> StateKeys => _stateKeys ??= _chatHistoryProvider.StateKeys.Concat(_memoryProvider.StateKeys).ToArray();

    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
                InvokingContext context, 
                CancellationToken cancellationToken = default)
    {
        // Delegate to the inner provider's full lifecycle (retrieve, filter, stamp, merge with request messages).
#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var chatHistoryProviderInputContext = new InvokingContext(context.Agent, context.Session, []);
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var allMessages = await this._chatHistoryProvider.InvokingAsync(chatHistoryProviderInputContext, cancellationToken).ConfigureAwait(false);
        var allMessagesList = allMessages.ToList();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[BoundedChatHistory] Session messages loaded: {allMessagesList.Count}");
        foreach (var msg in allMessagesList)
            Console.WriteLine($"  [{msg.Role}]: {msg.Text?.Substring(0, Math.Min(80, msg.Text?.Length ?? 0))}...");
        Console.ResetColor();

        // Search the vector store for relevant older messages.
        var aiContext = new AIContext { Messages = context.RequestMessages.ToList() };
        var invokingContext = new AIContextProvider.InvokingContext(
            context.Agent, context.Session, aiContext);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[BoundedChatHistory] Searching vector store for: \"{context.RequestMessages.LastOrDefault()?.Text}\"");
        Console.ResetColor();

        var result = await this._memoryProvider.InvokingAsync(invokingContext, cancellationToken).ConfigureAwait(false);

        // Extract only the messages added by the memory provider (stamped with AIContextProvider source type).
        var memoryMessages = result.Messages?
            .Where(m => m.GetAgentRequestMessageSourceType() == AgentRequestMessageSourceType.AIContextProvider)
            .ToList();

        if (memoryMessages is { Count: > 0 })
        {
            var memoryText = string.Join("\n", memoryMessages.Select(m => m.Text).Where(t => !string.IsNullOrWhiteSpace(t)));

            if (!string.IsNullOrWhiteSpace(memoryText))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[BoundedChatHistory] Recalled {memoryMessages.Count} message(s) from vector store.");
                Console.WriteLine($"  Memory context injected: {memoryText.Substring(0, Math.Min(120, memoryText.Length))}...");
                Console.ResetColor();

                var contextMessage = new ChatMessage(ChatRole.User, $"{this._contextPrompt}\n{memoryText}");
                return new[] { contextMessage }.Concat(allMessagesList);
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("[BoundedChatHistory] No relevant memories found in vector store.");
            Console.ResetColor();
        }

        return allMessagesList;
    }

    protected override async ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        // Delegate storage to the in-memory provider. Its TruncatingChatReducer (AfterMessageAdded trigger)
        // will automatically truncate to the configured maximum and expose any removed messages.
#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var innerContext = new InvokedContext(
            context.Agent, context.Session, context.RequestMessages, context.ResponseMessages!);
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        await this._chatHistoryProvider.InvokedAsync(innerContext, cancellationToken).ConfigureAwait(false);

        // Archive any messages that the reducer removed to the vector store.
        if (this._reducer.RemovedMessages is { Count: > 0 })
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[BoundedChatHistory] Overflow! Archiving {this._reducer.RemovedMessages.Count} message(s) to vector store:");
            foreach (var msg in this._reducer.RemovedMessages)
                Console.WriteLine($"  [{msg.Role}]: {msg.Text?.Substring(0, Math.Min(80, msg.Text?.Length ?? 0))}...");
            Console.ResetColor();

#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var overflowContext = new AIContextProvider.InvokedContext(
                context.Agent, context.Session, this._reducer.RemovedMessages, []);
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            await this._memoryProvider.InvokedAsync(overflowContext, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("[BoundedChatHistory] No overflow — session is within bounds.");
            Console.ResetColor();
        }
    }
    public void Dispose()
    {
        this._memoryProvider.Dispose();
    }
}
