using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;

namespace Willie.Bot
{
    public class WillieBot : IBot
    {
        private HttpClient _httpClient;

        public WillieBot(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Every Conversation turn for our EchoBot will call this method. In here
        /// the bot checks the Activty type to verify it's a message, bumps the 
        /// turn conversation 'Turn' count, and then echoes the users typing
        /// back to them. 
        /// </summary>
        /// <param name="context">Turn scoped context containing all the data needed
        /// for processing this conversation turn. </param>        
        public async Task OnTurn(ITurnContext context)
        {
            // This bot is only handling Messages
            if (context.Activity.Type == ActivityTypes.Message)
            {
                // Get the conversation state from the turn context
                var state = context.GetConversationState<WillieState>();

                // Bump the turn count. 
                state.TurnCount++;

                IMessageActivity response = null;

                if(context.Activity.Attachments?.Count > 0)
                {
                    IList<Attachment> attachments = new List<Attachment>();

                    foreach (Attachment originalAttachment in context.Activity.Attachments)
                    {
                        HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, originalAttachment.ContentUrl);
                        HttpResponseMessage res = await _httpClient.SendAsync(req);

                        if (res.IsSuccessStatusCode)
                        {
                            using (MemoryStream memStream = new MemoryStream())
                            {
                                await res.Content.CopyToAsync(memStream);
                                string base64Image = Convert.ToBase64String(memStream.GetBuffer());

                                Attachment attachment = new Attachment
                                {
                                    ContentType = originalAttachment.ContentType,
                                    Name = "Echoed Image",
                                    ContentUrl = $"data:{originalAttachment.ContentType};base64,{base64Image}"
                                };

                                attachments.Add(attachment);
                            }
                        }
                    }

                    response = MessageFactory.Carousel(attachments, $"Turn {state.TurnCount}: You sent this");
                }
                else
                {
                    response = MessageFactory.Text($"Turn {state.TurnCount}: You sent '{context.Activity.Text}'");
                }
                // Echo back to the user whatever they typed.
                await context.SendActivity(response);
            }
        }
    }    
}
