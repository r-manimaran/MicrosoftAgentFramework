using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentWithMemory_04_BoundedChatHistory;

internal sealed class TruncatingChatReducer : IChatReducer
{
    private readonly int _maxMessages;
    public TruncatingChatReducer(int maxMessages)
    {
        _maxMessages = maxMessages > 0 ? maxMessages : throw new ArgumentOutOfRangeException(nameof(maxMessages));
    }

    /// <summary>
    /// Gets the messages that are removed during the most recent call to <see cref="ReduceAsync"/>
    /// </summary>
    public IReadOnlyList<ChatMessage> RemovedMessages { get; private set; } = [];

    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, 
                                                    CancellationToken cancellationToken)
    {
       _ = messages ?? throw new ArgumentNullException(nameof(messages));

        ChatMessage? systemMessage = null;
        Queue<ChatMessage> retained = new(capacity: _maxMessages);
        List<ChatMessage> removedMessages = [];

        foreach(var message in messages)
        {
            if (message.Role == ChatRole.System)
            {
                // preserve the first system message outside the counting window
            }
            else if(!message.Contents.Any(c=>c is FunctionCallContent or FunctionResultContent))
            {
                if(retained.Count >= _maxMessages)
                {
                    removedMessages.Add(retained.Dequeue());
                }

                retained.Enqueue(message);
            }
        }
        RemovedMessages = removedMessages;

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"[TruncatingReducer] Max={_maxMessages} | Retained={retained.Count} | Removed={removedMessages.Count}");
        Console.ResetColor();

        IEnumerable<ChatMessage> result = systemMessage is not null 
                       ? new[] { systemMessage }.Concat(retained)
                       : retained;
        return Task.FromResult(result);
    }
}
