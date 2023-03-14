using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace Recording
{
    public static class Callback
    {
        private static string serverCallId = "";
        private static string recordingId = "";
        private static string callConnectionId = "";
        private static ConcurrentDictionary<string, string> callConnectionMap = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, string> callConnectionRecordingMap = new ConcurrentDictionary<string, string>();

        [FunctionName("Callback")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {
            try
            {
                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();

                // Don't move this line below, it should be before reading from stream
                var content = await req.ReadAsStringAsync();

                string response = string.Empty;
                BinaryData binEvents = await BinaryData.FromStreamAsync(req.Body);
                log.LogInformation($"Received events: {binEvents}");
                EventGridEvent[] eventGridEvents = new List<EventGridEvent>().ToArray();

                if (binEvents != null)
                {
                    try
                    {
                        eventGridEvents = EventGridEvent.ParseMany(binEvents);
                    }
                    catch (Exception ex)
                    {
                        // do nothing if it does not parse well
                    }
                }

                foreach (EventGridEvent eventGridEvent in eventGridEvents)
                {
                    // Handle system events for webhook handshake
                    if (eventGridEvent.TryGetSystemEventData(out object eventData))
                    {
                        // Handle the subscription validation event
                        if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                        {
                            log.LogInformation($"Got SubscriptionValidation event data, validation code: {subscriptionValidationEventData.ValidationCode}, topic: {eventGridEvent.Topic}");
                            // Do any additional validation (as required) and then return back the below response
                            var responseData = new
                            {
                                ValidationResponse = subscriptionValidationEventData.ValidationCode
                            };

                            return new OkObjectResult(responseData);
                        }
                    }
                }

                if (content.Length > 0)
                {
                    var events = JArray.Parse(content);
                    foreach (var rawEvent in events)
                    {
                        log.LogInformation("Got raw event");
                        log.LogInformation(rawEvent.ToString());
                        var rawEventType = rawEvent["type"].ToObject<string>();

                        ///  serverCallId = rawEvent["data"]["serverCallId"].ToObject<string>();
                        //CallConnection
                        switch (rawEventType)
                        {
                            case "Microsoft.Communication.CallConnected":

                                // var playSource = new FileSource(new Uri($"{TextToSpeech.BlobStoreBaseUri}/{TextToSpeech.SpeechContainerName}/{callConnectionId}.wav"));
                                var playOptions = new PlayOptions() { Loop = true };
                                serverCallId = rawEvent["data"]["serverCallId"].ToObject<string>(); //callAutomationClient.GetCallConnection(callConnectionId).GetCallConnectionProperties().Value?.ServerCallId;
                                callConnectionId = rawEvent["data"]["callConnectionId"].ToObject<string>();


                                callConnectionMap.TryAdd(callConnectionId, serverCallId);

                                // play audio then recognize 3-digit DTMF input with pound (#) stop tone
                                //var recognizeOptions =
                                //    new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(Environment.GetEnvironmentVariable("CallerUserIdentifier")), 3)
                                //    {
                                //        InterruptPrompt = true,
                                //        InterToneTimeout = TimeSpan.FromSeconds(10),
                                //        InitialSilenceTimeout = TimeSpan.FromSeconds(5),
                                //     //   Prompt = new FileSource(new Uri("https://acssolutionstorage.blob.core.windows.net/acs/text.wav?sp=r&st=2023-03-14T00:40:02Z&se=2023-03-14T08:40:02Z&spr=https&sv=2021-12-02&sr=b&sig=9P7ldxukyNXkO1NmQVaFGgP9IhPKfMVi%2FLoOliowtY0%3D")),
                                //        StopTones = new[] { DtmfTone.Pound },
                                //        OperationContext = "MainMenu"
                                //    };
                                //await callAutomationClient.GetCallConnection(callConnectionId)
                                //    .GetCallMedia()
                                //    .StartRecognizingAsync(recognizeOptions);
                                break;

                            case "Microsoft.Communication.TeamsRecordingStateChanged":
                                recordingId = rawEvent["data"]["recordingId"].ToObject<string>();
                                //var part = await callAutomationClient.GetCallConnection(callConnectionId).GetParticipantsAsync();


                                break;

                            case "Microsoft.Communication.CallDisconnected":
                                callConnectionId = rawEvent["data"]["callConnectionId"].ToObject<string>();

                                callConnectionMap.TryRemove(callConnectionId, out string servercallId);


                                callConnectionRecordingMap.TryRemove(callConnectionId, out string recording);

                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
                // handle exception
            }

            return new OkResult();

        }

        [FunctionName("StartRecording")]
        public static async Task<IActionResult> StartRecording(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {
            try
            {

                var connectionId = ValidateGetConnectionId(req);


                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();

                // callAutomationClient.GetCallRecording().StartRecording

                // var part = await callAutomationClient.GetCallConnection(callConnectionId).GetParticipantsAsync();

                StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(callConnectionMap[connectionId]))
                {
                    RecordingContent = RecordingContent.Audio,
                    RecordingChannel = RecordingChannel.Unmixed,
                    RecordingFormat = RecordingFormat.Wav,
                    RecordingStateCallbackEndpoint = new Uri(Environment.GetEnvironmentVariable("RecordingStateUrl"))
                };

                var participant = await callAutomationClient.GetCallConnection(connectionId).GetParticipantsAsync();

                //  foreach (var item in participant.Value)
                //  {


                //    recordingOptions.AudioChannelParticipantOrdering.Add(new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("CalleeUserIndentifier")));
                //recordingOptions.AudioChannelParticipantOrdering.Add(new PhoneNumberIdentifier("+17882881402"));
                //  break;
                //   }


                Response<RecordingStateResult> response = callAutomationClient.GetCallRecording()
                .StartRecording(recordingOptions);

                recordingId = response.Value.RecordingId;

                if (!callConnectionRecordingMap.ContainsKey(connectionId))
                {
                    callConnectionRecordingMap.TryAdd(connectionId, recordingId);
                }
                else
                {
                    callConnectionRecordingMap[connectionId] = recordingId;

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
                // handle exception
            }

            return new OkObjectResult(recordingId);

        }


        [FunctionName("StopRecording")]
        public static async Task<IActionResult> StopRecording(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {
            try
            {
                var connectionId = ValidateGetConnectionId(req);

                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
                var stopRecording = await callAutomationClient.GetCallRecording().StopRecordingAsync(callConnectionRecordingMap[connectionId]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new ExceptionResult(ex, true);
            }

            return new OkResult();

        }


        [FunctionName("PauseRecording")]
        public static async Task<IActionResult> PauseRecording(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                var connectionId = ValidateGetConnectionId(req);


                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
                var stopRecording = await callAutomationClient.GetCallRecording().PauseRecordingAsync(callConnectionRecordingMap[connectionId]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new ExceptionResult(ex, true);
            }

            return new OkResult();

        }

        [FunctionName("ResumeRecording")]
        public static async Task<IActionResult> ResumeRecording(
         [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
          ILogger log)
        {
            try
            {

                var connectionId = ValidateGetConnectionId(req);


                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
                var stopRecording = await callAutomationClient.GetCallRecording().ResumeRecordingAsync(callConnectionRecordingMap[connectionId]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new ExceptionResult(ex, true);
                // handle exception
            }

            return new OkResult();

        }

        [FunctionName("GetRecordingState")]
        public static async Task<IActionResult> GetRecordingState(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
     ILogger log)
        {
            RecordingStateResult recordingState;
            try
            {

                var connectionId = ValidateGetConnectionId(req);


                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
                recordingState = await callAutomationClient.GetCallRecording().GetRecordingStateAsync(callConnectionRecordingMap[connectionId]);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkObjectResult(recordingState);

        }


        [FunctionName("RecordingFileStatus")]
        public static async Task<IActionResult> RecordingFileStatus(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {

            // Don't move this line below, it should be before reading from stream
            var content = await req.ReadAsStringAsync();

            string response = string.Empty;
            BinaryData events = await BinaryData.FromStreamAsync(req.Body);
            log.LogInformation($"Received events: {events}");

            EventGridEvent[] eventGridEvents = EventGridEvent.ParseMany(events);

            foreach (EventGridEvent eventGridEvent in eventGridEvents)
            {
                // Handle system events for webhook handshake
                if (eventGridEvent.TryGetSystemEventData(out object eventData))
                {
                    // Handle the subscription validation event
                    if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                    {
                        log.LogInformation($"Got SubscriptionValidation event data, validation code: {subscriptionValidationEventData.ValidationCode}, topic: {eventGridEvent.Topic}");
                        // Do any additional validation (as required) and then return back the below response
                        var responseData = new
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };

                        return new OkObjectResult(responseData);
                    }
                }
            }


            try
            {
                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();

                var payload = JsonConvert.DeserializeObject<IEnumerable<RecordingFileStatusUpdatedDataPayload>>(content).FirstOrDefault();

                foreach (var item in payload.Data.RecordingStorageInfo.RecordingChunks)
                {
                    var recordingDownloadUri = new Uri(item.ContentLocation);
                    var callResponse = await callAutomationClient.GetCallRecording().DownloadStreamingAsync(recordingDownloadUri);


                    var recordingStreamResult = callResponse.GetRawResponse().Status.ToString();


                    var recordingFileName = item.DocumentId + "-" + item.Index + "-" +
                        payload.Data.RecordingStartTime + ".wav";

                    var blobStorageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                    var blobStorageContainerName = Environment.GetEnvironmentVariable("ContainerName");
                    var container = new BlobContainerClient(blobStorageConnectionString, blobStorageContainerName);

                    var uploadResult = await container.UploadBlobAsync(recordingFileName,
                        callResponse.Value);

                    var uploadResultStatus = uploadResult.GetRawResponse().Status.ToString();

                    BlobClient blobClient = new BlobClient(blobStorageConnectionString, blobStorageContainerName, recordingFileName);
                    var sasUri = blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTime.Now.AddDays(1));


                    Console.Write(sasUri);

                    return new OkObjectResult(sasUri);
                    //   await SpeechService.ConvertAudioToText(sasUri);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkResult();

        }


        [FunctionName("AddTeamsUser")]
        public static async Task<IActionResult> AddTeamsUser(
       [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
        {

            var connectionId = ValidateGetConnectionId(req);


            MicrosoftTeamsUserIdentifier teamsUserIdentifier = new MicrosoftTeamsUserIdentifier(Environment.GetEnvironmentVariable("TeamsUserAADId"));
            CallInvite ci = new CallInvite(teamsUserIdentifier);

            try
            {
                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
                var x = await callAutomationClient.GetCallConnection(connectionId).AddParticipantAsync(ci);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkObjectResult("Ok");

        }


        [FunctionName("AddUser")]
        public static async Task<IActionResult> AddUser(
       [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            try
            {
                var connectionId = ValidateGetConnectionId(req);

                CommunicationUserIdentifier cui = new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("AnotherUser"));
                CallInvite ci = new CallInvite(cui);
                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();

                await callAutomationClient.GetCallConnection(connectionId).AddParticipantAsync(ci);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkObjectResult("Ok");

        }


        [FunctionName("AddPhoneUser")]
        public static async Task<IActionResult> AddPhoneUser(
     [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
      ILogger log)
        {
            try
            {
                var connectionId = ValidateGetConnectionId(req);
                PhoneNumberIdentifier phone = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("TargetPhoneNumber"));
                PhoneNumberIdentifier caller = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("ACSCallerPhoneNumber"));
                CallInvite ci = new CallInvite(phone, caller);
                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();

                await callAutomationClient.GetCallConnection(connectionId).AddParticipantAsync(ci);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkObjectResult("Ok");

        }




        private static string ValidateGetConnectionId(HttpRequest req)
        {

            string callConnectionId1 = "";

            if (req.GetQueryParameterDictionary().ContainsKey("callConnectionId"))
            {
                callConnectionId1 = (req.GetQueryParameterDictionary()["callConnectionId"]);

            }

            if (!callConnectionMap.ContainsKey(callConnectionId1))
            {
                throw new Exception($"Provided Call Connection Id {callConnectionId1} does not exists");
            }


            return callConnectionId1;

        }


        [FunctionName("RecordingState")]
        public static async Task<IActionResult> RecordingState(
   [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
    ILogger log)
        {
            var content = await req.ReadAsStringAsync();



            Task<Class1[]> ts = Task.Run(() =>
            {
                return JsonConvert.DeserializeObject<Class1[]>(content);
            });

            Class1[] x = await ts.ConfigureAwait(false);

            Console.WriteLine(DateTime.UtcNow + " --- " + x[0].data.state.ToString());

            return new OkObjectResult(x[0].data.state.ToString());

        }


        public class Rootobject
        {
            public Class1[] Property1 { get; set; }
        }

        public class Class1
        {
            public string id { get; set; }
            public string source { get; set; }
            public string type { get; set; }
            public Data data { get; set; }
            public DateTime time { get; set; }
            public string specversion { get; set; }
            public string datacontenttype { get; set; }
            public string subject { get; set; }
        }

        public class Data
        {
            public string recordingId { get; set; }
            public string state { get; set; }
            public DateTime startDateTime { get; set; }
            public string callConnectionId { get; set; }
            public string serverCallId { get; set; }
            public string correlationId { get; set; }
        }


    }
}
