using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomChatHistoryReducer.Reducers;

internal class AIDrivenCityReducer(AIAgent agent) : IChatReducer
{
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> previousMessages, CancellationToken cancellationToken)
    {
        Utils.Yellow("AIDrivenCityReducer called");
        List<ChatMessage> messages = previousMessages.ToList();
        if (messages.Count <= 0) {
            return messages;
        }

        AlwaysRemoveToolCallsReducer alwaysRemoveToolCallsreducer = new();
        List<ChatMessage> normalMessages = (await alwaysRemoveToolCallsreducer.ReduceAsync(messages, cancellationToken: cancellationToken)).ToList();

        StringBuilder messagesAsString = new();
        for(int i = 0; i < normalMessages.Count; i++)
        {
            messagesAsString.AppendLine($"{i}:{normalMessages[i].Text}");
        }

        AgentResponse<List<int>> response = await agent.RunAsync<List<int>>(messagesAsString.ToString(), cancellationToken: cancellationToken);
        List<int> indexesToExclude = response.Result;

        List<ChatMessage> toKeep = [];
        toKeep.AddRange(normalMessages.Where((_, i) => !indexesToExclude.Contains(i)));
        return toKeep;
    }
}
