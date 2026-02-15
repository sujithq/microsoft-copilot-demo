using Azure.Identity;
using CopilotAgent.Handlers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace CopilotAgent;

public class CopilotBot : ActivityHandler
{
    private readonly CopilotMessageHandler _messageHandler;

    public CopilotBot(CopilotMessageHandler messageHandler)
    {
        _messageHandler = messageHandler;
    }

    protected override async Task OnMessageActivityAsync(
        ITurnContext<IMessageActivity> turnContext, 
        CancellationToken cancellationToken)
    {
        // Handle the message and get response from orchestrator
        var response = await _messageHandler.HandleMessageAsync(
            (Activity)turnContext.Activity, 
            cancellationToken);

        // Send the response back to the user
        await turnContext.SendActivityAsync(
            MessageFactory.Text(response), 
            cancellationToken);
    }

    protected override async Task OnMembersAddedAsync(
        IList<ChannelAccount> membersAdded, 
        ITurnContext<IConversationUpdateActivity> turnContext, 
        CancellationToken cancellationToken)
    {
        var welcomeText = "Hello! I'm your GraphRAG assistant. Ask me anything about your knowledge base.";
        
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Text(welcomeText), 
                    cancellationToken);
            }
        }
    }
}
