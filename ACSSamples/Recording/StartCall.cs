using Azure.Communication;
using Azure.Communication.CallAutomation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Text.Json.Nodes;

namespace Recording
{
    public static class StartCall
    {
        [FunctionName("StartCall")]

        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {



            CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
            //   CommunicationUserIdentifier caller = new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("CalleeUserIndentifier"));


            CommunicationUserIdentifier targetUser = null;

            if (req.GetQueryParameterDictionary().ContainsKey("userid"))
            {
                targetUser = new CommunicationUserIdentifier(req.GetQueryParameterDictionary()["userid"]);

            }
            else
            {
                targetUser = new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("CalleeUserIndentifier"));

            }


            CallInvite callInviteUser = new CallInvite(targetUser);


            // CreateCallOptions callOptions = new CreateCallOptions(callInvite,                 
            //   callbackUri: new Uri(Environment.GetEnvironmentVariable("Callbackurl")));

            // /**** Old code 16.1
            // CallSource callSource = new CallSource(new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("CallerUserIdentifier")));

            //  callSource.CallerId = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("ACSCallerPhoneNumber"));

            // callSource.DisplayName = "Contoso";
            //   CommunicationIdentifier callee = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("CallePhoneNumber")); 

            // CreateCallOptions callOptions = new CreateCallOptions(callSource, targets: new List<CommunicationIdentifier>() { callee },
            // callbackUri: new Uri(Environment.GetEnvironmentVariable("Callbackurl")));

            // Old Code end

            CreateCallResult call;
            try
            {
                CallInvite callInvite = null;

                if (req.GetQueryParameterDictionary().ContainsKey("PhoneCall"))
                {
                    PhoneNumberIdentifier phone = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("TargetPhoneNumber"));
                    PhoneNumberIdentifier callerPhone = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("ACSCallerPhoneNumber"));
                    CallInvite ci = new CallInvite(phone, callerPhone);
                    callInvite = ci;
                }
                else
                {
                    callInvite = callInviteUser;
                }

                call = callAutomationClient.CreateCall(callInvite, callbackUri: new Uri(Environment.GetEnvironmentVariable("Callbackurl")));
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
                throw;
            }

            return new OkObjectResult(call.CallConnection.CallConnectionId);
        }

        [FunctionName("Transcription")]
        public static async Task<IActionResult> Transcription(
             [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var url = "";

            await SpeechService.ConvertAudioToText(new Uri(url));


            return new OkResult();
        }



        [FunctionName("IncomingCall")]
        public static async Task<IActionResult> IncomingCall(
             [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            CallAutomationClient ca = CallAutomationFactory.GetAutomationClient();
            
            // Don't move this line below, it should be before reading from stream
            var content = await req.ReadAsStringAsync();

            string response = string.Empty;
            BinaryData events = await BinaryData.FromStreamAsync(req.Body);
            log.LogInformation($"Received events: {events}");

            EventGridEvent[] eventGridEvents = EventGridEvent.ParseMany(events);


            foreach (var eventGridEvent in eventGridEvents)
            {
                if (eventGridEvent.TryGetSystemEventData(out object eventData))
                {
                    // Handle the webhook subscription validation event.
                    if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                    {
                        var responseData = new SubscriptionValidationResponse
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };
                        return new OkObjectResult(responseData);
                    }
                }
                var jsonObject = JsonNode.Parse(eventGridEvent.Data).AsObject();
                var callerId = (string)(jsonObject["from"]["rawId"]);
                var incomingCallContext = (string)jsonObject["incomingCallContext"];
                var callbackUri = new Uri(Environment.GetEnvironmentVariable("Callbackurl"));

                AnswerCallResult answerCallResult = await ca.AnswerCallAsync(incomingCallContext, callbackUri);
            }


            return new OkResult();
        }
    }
}

