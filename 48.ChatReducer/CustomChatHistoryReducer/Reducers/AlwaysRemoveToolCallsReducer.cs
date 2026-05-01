using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomChatHistoryReducer.Reducers;

internal class AlwaysRemoveToolCallsReducer : IChatReducer
{
    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> previousMessages, CancellationToken cancellationToken)
    {
        Utils.Yellow("AlwaysRemoveToolCallsReducer called");
        List<ChatMessage> toKeep = [];

        foreach (ChatMessage message in previousMessages)
        {
            if(message.Role == ChatRole.Tool)
            {
                continue; // Get rid of Tool Results
            }
            if(message.Role == ChatRole.Assistant && message.Contents.Any(x=>x is FunctionCallContent))
            {
                continue; // get rid of Tool Requests from the AI
            }
            toKeep.Add(message);
        }
        return Task.FromResult(toKeep.AsEnumerable());
    }
}
