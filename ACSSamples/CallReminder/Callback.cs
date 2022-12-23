using Azure.Communication.CallingServer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace EventReminder
{
    public static class Callback
    {
        [FunctionName("Callback")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable("connectionString"); // Find your Communication Services resource in the Azure portal
                CallAutomationClient callAutomationClient = new CallAutomationClient(connectionString);

                var content = await req.ReadAsStringAsync();
                log.LogInformation(content);

                if (content.Length > 0)
                {
                    var events = JArray.Parse(content);
                    foreach (var rawEvent in events)
                    {
                        log.LogInformation("Got raw event");
                        log.LogInformation(rawEvent.ToString());
                        var rawEventType = rawEvent["type"].ToObject<string>();
                        var callConnectionId = rawEvent["data"]["callConnectionId"].ToObject<string>();

                        switch (rawEventType)
                        {
                            case "Microsoft.Communication.CallConnected":
                               
                                var playSource = new FileSource(new Uri($"{TextToSpeech.BlobStoreBaseUri}/{TextToSpeech.SpeechContainerName}/{callConnectionId}.wav"));
                                var playOptions = new PlayOptions() { Loop = true };
                                await callAutomationClient.GetCallConnection(callConnectionId)
                                     .GetCallMedia()
                                     .PlayToAllAsync(playSource, playOptions);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // handle exception
            }

            return new OkResult();

        }
    }
}
