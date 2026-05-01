using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomChatHistoryReducer.Reducers;

internal class AIDrivenPirateSummaryReducer(AIAgent agent, int numberOfPreviousMessagesBeforeSummarize) : IChatReducer
{
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> previousMessages, CancellationToken cancellationToken)
    {
        Utils.Yellow("AIDrivenPirateSummaryReducer called");
        List<ChatMessage> messages = previousMessages.ToList();
        if(messages.Count <= numberOfPreviousMessagesBeforeSummarize)
        {
            Utils.Yellow("[No summary yet]");
            return messages;
        }
        Utils.Green("[Summarizing...]");
        AgentResponse response = await agent.RunAsync(messages, cancellationToken: cancellationToken);

        return new List<ChatMessage>
        {
            new(ChatRole.User, "Summary so far:"+response.Text)
        };
    }
}
