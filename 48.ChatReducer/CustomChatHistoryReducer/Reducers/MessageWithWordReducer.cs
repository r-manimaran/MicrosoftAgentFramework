using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomChatHistoryReducer.Reducers;

internal class MessageWithWordReducer(string word) : IChatReducer
{
    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> previousMessages, CancellationToken cancellationToken)
    {
        Utils.Yellow("MessagesWithWordReducer called");
        return Task.FromResult(previousMessages.Where(x => !x.Text.Contains(word, StringComparison.InvariantCultureIgnoreCase)));
    }
}
