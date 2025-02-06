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
using System.Globalization;
using System.Management.Automation.Language;

using Newtonsoft.Json;
using System.IO.Compression;
using System.IO;
using System.Text;
//using System.IdentityModel.Tokens.Jwt;
//using System.IdentityModel.Tokens.Jwt;
//using System.IdentityModel.Tokens.Jwt;

namespace Recording
{
    public static class StartCall
    {

        [FunctionName("StartCall")]

        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {          

            RecordingFileStatusUpdatedDataPayload payload = new RecordingFileStatusUpdatedDataPayload();
            payload.Data = new Data();
            payload.Data.RecordingStorageInfo = new RecordingStorageInfo() { };
            payload.Data.RecordingStorageInfo.RecordingChunks = new List<RecordingChunk>().ToArray();


            CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
            //   CommunicationUserIdentifier caller = new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("CalleeUserIndentifier"));

          
            //  CallInvite ci = new CallInvite(teamsUserIdentifier);

             CommunicationUserIdentifier targetUser = null;

            if (req.GetQueryParameterDictionary().ContainsKey("userid") && !string.IsNullOrEmpty(req.GetQueryParameterDictionary()["userid"]))
            {
                targetUser = new CommunicationUserIdentifier(req.GetQueryParameterDictionary()["userid"]);

            }
            else
            {
                targetUser = new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("CalleeUserIndentifier"));
            }


            CallInvite callInviteUser = new CallInvite(targetUser);
           

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

               // call = callAutomationClient.CreateGroupCall(new CreateGroupCallOptions(new List<CommunicationIdentifier>() { targetUser }, new Uri(Environment.GetEnvironmentVariable("Callbackurl"))));

                CreateCallOptions co = new CreateCallOptions(callInvite, callbackUri: new Uri(Environment.GetEnvironmentVariable("Callbackurl")));
               
                //  CallIntelligenceOptions io = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri("https://recordingmutliservicecog.cognitiveservices.azure.com/") };
                //  co.CallIntelligenceOptions = io;
                
                call = callAutomationClient.CreateCall(co);

            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
                throw;
            }           

            return new OkObjectResult(call);
        }

        [FunctionName("StartAutoRecordTest")]

        public static async Task<IActionResult> StartAutoRecordTest(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           ILogger log)
        {
       //  var x =   DecompressGzipString("H4sIAAAAAAAACs1WXW/jNhD8L3roS4+WKFGiZCAoEvcSuFcHrp20aIsioEhKYS2LKknZMYL89y5lOx93KS7X9uEMJIZIcTg7u7Pr+4CzprnUTlWKM6d0G4zvg8rotf9WIhgHZPwtzpMkJlGUFHnwLhDKdg3bXbK1DMZt3zQvlpa6Nxw2gr5dtXrbwgHZik6r1k09HKskZrziCGNRIBLnHLGKcRTlXEa8ynNJGZxpWFv3rJb+zP6OjhmnuOrYAahkWcklIUiWQgCQSFFRRBkqIpGkWZYmMS0A6FYJISGqijVWAorRnTRud8bqPfDDu8Dpx2DzMeN2jHNS4DKmqOAZQaRKCCopZaiKiCjLRCZlwW+iwwe98u/4+c9qvfGSz6uVC1JQUaWoZIwhEqUCgmMUCZqlsoxwWbLyU7WMtAPBAeItqgSvK9yodmW9yMw5xm8B7Na5zo7DkHVqVDWqvnVw7m43sqtdJ0dcr/1OuIlD3oWco95uJYpiZEWF2MqOOH/2JufhBoeVNispQhZRVmEMVSUhPMJzimApQUlWFhHslZRlYUxDhUOSROGe0HfqBEcIxznCGYpTAmGspVDstIV7DdDlHLiufWECzRpksV9RDEdKr0Rh5J+Su6+I657QK0x70V0Z1tpOG88XHoFtGo8w9n9kROJxQmgeBg+H3Ex062TrjgXve9ZM3flsOdND7QLCB7nzRWdBG+hsw1OwtD//cH791yKcXvyKv6eZUxft3fbkBCiAYVbSX/4LXyxXlq/fL1xbx/FPW0U+/ObfgbtvlXXa7KZtBV3j93tQ2DFTS3cF6sBRJa47wZw812YJ/r4a9gBbG1WrljWHhX1bLaI4izDOafrJG+8PXeAAK2TF+sYDVS9Q/v9+Vb2BwyNVn7BKGm0OTv/DV5xQBlIMgs/3nUDJwfrKLg5bp02jt1LsEwWSQginvdMLybU5rH60uPjiPgSwXLcbaeww1abtRrnH+fZ8x5eR0U0z2PyLXAIgg0/iF0aB1edWgceQnzvo9Rf9JJqc/XjuMKPbTb998gBFOM++kSdZkmc0xikmGaXgIeznl7IzEF3NoZ/vjtooe2Y0E5xZd2jVEK6QZV8/mmI/2Ae90izmlGYVSglJEaEVRnlJY5TneSYrTMuykHARZIt7o7T1BE4+yTJtrWPtMKSO8nyuQfg2qT0PxZrJR78vlgO94zT+d1NqCqW0d92lrAF7AJ7D4ATyj9iz+ZH5UpqN4vLaqGcxlMhE+ygwHq27J/ZjDEZIw6HHQPa9JtpAw4N7nyD3v0D+CWI06xa6kTfTyxvv7Nl83peN4tPuitXX1ld+cDpZ+hqtje67IWl3h0728DfYLWOckwkAAA==");
            
            RecordingFileStatusUpdatedDataPayload payload = new RecordingFileStatusUpdatedDataPayload();
            payload.Data = new Data();
            payload.Data.RecordingStorageInfo = new RecordingStorageInfo() { };
            payload.Data.RecordingStorageInfo.RecordingChunks = new List<RecordingChunk>().ToArray();


            CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
            CommunicationUserIdentifier targetUser = null;
                       
            CallInvite callInviteUser = new CallInvite(targetUser);


            CreateCallResult call = null;

            for (int i = 0; i < 1; i++)
            {
                CallInvite callInvite = null;

                {
                    PhoneNumberIdentifier phone = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("TargetPhoneNumber"));
                    PhoneNumberIdentifier callerPhone = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("ACSCallerPhoneNumber"));
                    CallInvite ci = new CallInvite(phone, callerPhone);
                    callInvite = ci;
                }

                CreateCallOptions co = new CreateCallOptions(callInvite, callbackUri: new Uri(Environment.GetEnvironmentVariable("Callbackurl")));

                call = callAutomationClient.CreateCall(co);

                await Task.Delay(10000);

            }

            return new OkObjectResult("OK");
        }

        [FunctionName("StartTeamsCall")]

        public static async Task<IActionResult> StartTeamsCall(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            //DateTime startTime = new DateTime("2");


            string utcString = "2024-03-07T02:00:11Z";

            // Define the format of the UTC string
            string format = "yyyy-MM-ddTHH:mm:ssZ";

            // Parse the UTC string into a DateTime object
            if (DateTime.TryParseExact(utcString, format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime result))
            {
                Console.WriteLine("Parsed DateTime (UTC): " + result.ToString("yyyy-MM-dd HH:mm:ss"));
            }


            object a = null;

            Console.WriteLine("vk " + a);

            bool p = false;

            a = p;

            Console.WriteLine(a?.ToString());

            string x = null;

            //  var b= x.ToString();


            RecordingFileStatusUpdatedDataPayload payload = new RecordingFileStatusUpdatedDataPayload();
            payload.Data = new Data();
            payload.Data.RecordingStorageInfo = new RecordingStorageInfo() { };
            payload.Data.RecordingStorageInfo.RecordingChunks = new List<RecordingChunk>().ToArray();


            CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
            

            MicrosoftTeamsUserIdentifier teamsUserIdentifier = new MicrosoftTeamsUserIdentifier(req.GetQueryParameterDictionary()["teamsUserId"]);


            CallInvite callInviteUser = new CallInvite(teamsUserIdentifier);


            CreateCallResult call;
            try
            {
                CallInvite callInvite = null;


                callInvite = callInviteUser;

                CreateCallOptions co = new CreateCallOptions(callInvite, callbackUri: new Uri(Environment.GetEnvironmentVariable("Callbackurl")));
                
                call =  await callAutomationClient.CreateCallAsync(co);
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
                throw;
            }

            return new OkObjectResult(call);
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
                var targetId = (string)(jsonObject["to"]["rawId"]);
                var userType = (string)(jsonObject["from"]["kind"]);
                var incomingCallContext = (string)jsonObject["incomingCallContext"];
                string payloadJson = JsonConvert.SerializeObject(incomingCallContext);            


                var callbackUri = new Uri(Environment.GetEnvironmentVariable("Callbackurl"));

                var callee = (string)(jsonObject["to"]["rawId"]);

                if (callerId == "8:acs:8731bbaf-8768-408b-9d6a-79bfba64bf3d_00000016-6bb5-8c55-9806-113a0d000398")
                {
                    AnswerCallResult answerCallResult = await ca.AnswerCallAsync(incomingCallContext, callbackUri);

                }

                // Answer Call only if from Phone user
                if (targetId.Contains("28:orgid:0bee1b87-fd1d-426b-8d00-503bcc93bb1b"))
                {
                    try
                    {

                        AnswerCallResult answerCallResult = await ca.AnswerCallAsync(incomingCallContext, callbackUri);
                        Console.WriteLine(answerCallResult.CallConnection.CallConnectionId);
                    }
                    catch (Exception ex) {

                        }
                }
            }


            return new OkResult();
        }

        private static string DecompressGzipString(string text)
        {
            using (var inputStream = new MemoryStream(Convert.FromBase64String(text)))
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    gzipStream.CopyTo(outputStream);
                }

                return Encoding.UTF8.GetString(outputStream.ToArray());
            }
        }
    }
}

