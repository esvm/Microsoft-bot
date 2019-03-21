// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Net.Sockets;
using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service. Transient lifetime services are created
    /// each time they're requested. Objects that are expensive to construct, or have a lifetime
    /// beyond a single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class MyBot : IBot
    {           
        private const string WelcomeText = @"Sou o CInWiki-BOT e vou te ajudar a obter mais informações sobre algumas cadeiras do CIn.
                                             Digite o código de alguma cadeira para começarmos!";     
        /// <summary>
        /// Initializes a new instance of the <see cref="MyBot"/> class.
        /// </summary>                        

        /// <summary>
        /// Every conversation turn calls this method.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                string responseMessage = "";
                string template = "https://pet.cin.ufpe.br/~pet/wiki/";
                string messageReceived = turnContext.Activity.Text.ToUpper().Trim();
                
                responseMessage = template + messageReceived;

                //---create a TCPClient object at the IP and port no.---
                TcpClient client = new TcpClient(SERVER_IP, PORT_NO);
                NetworkStream nwStream = client.GetStream();
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(responseMessage);

                nwStream.Write(bytesToSend, 0, bytesToSend.Length);

                //---read back the text---
                byte[] bytesToRead = new byte[client.ReceiveBufferSize];
                int bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);

                client.Close();
                string answer = Encoding.UTF8.GetString(bytesToRead, 0, bytesRead);

                string nomeCadeira = answer.Split("-")[0].Trim();
                string professor = answer.Split("-")[1].Trim();
                
                var reply = turnContext.Activity.CreateReply();
                reply.Attachments.Add(GetThumbnailCard(nomeCadeira, professor, "", responseMessage).ToAttachment());

                await turnContext.SendActivityAsync(reply);
            
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (turnContext.Activity.MembersAdded != null)
                {
                    await SendWelcomeMessageAsync(turnContext, cancellationToken);
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }
        /// <summary>
        /// Greet new users as they are added to the conversation.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(
                        $"Olá, {member.Name}! {WelcomeText}",
                        cancellationToken: cancellationToken);
                }
            }
        }
        /// <summary>
        /// Creates a <see cref="ThumbnailCard"/>.
        /// </summary>
        /// <returns>A <see cref="ThumbnailCard"/> the user can view and/or interact with.</returns>
        /// <remarks>Related types <see cref="CardImage"/>, <see cref="CardAction"/>,
        /// and <see cref="ActionTypes"/>.</remarks>
        private static ThumbnailCard GetThumbnailCard(string nome, string professor, string texto, string link)
        {
            var heroCard = new ThumbnailCard
            {
                Title = nome,
                Subtitle = professor,
                Text = texto,
                Images = new List<CardImage> { new CardImage("https://www2.cin.ufpe.br/site/uploads/arquivos/18/20120530161145_marca_cin_2012_producao.jpg") },
                Buttons = new List<CardAction> { new CardAction(ActionTypes.OpenUrl, "Página da CInWiki", value: link) }
            };

            return heroCard;
        }

        const int PORT_NO = 5000;
        const string SERVER_IP = "127.0.0.1";
    }
}
