using Azure.Communication;
using Azure.Communication.CallAutomation;
    using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Storage.Blobs;
using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;


namespace Recording
{
    public static class Callback
    {
        private static string serverCallId = "";
        private static string recordingId = "";
        private static string callConnectionId = "";
        private static string correlationId = "";
        private static ConcurrentDictionary<string, string> callConnectionMap = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, string> callConnectionRecordingMap = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, string> callConnectionCallIdMap = new ConcurrentDictionary<string, string>();
        private static DateTime pauseResponseTime;
        private static Stopwatch watch = new Stopwatch();
        private static DateTime callConnected;
        private static DateTime? firstRecordingStateChanged;
        private static int pauseResumeExecutedOnce = 0;


        [FunctionName("Callback")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {
            try
            {

                //  log.LogInformation($"Event received: {JsonConvert.SerializeObject(cloudEvents)}");
                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();

                // Don't move this line below, it should be before reading from stream
                var content = await req.ReadAsStringAsync();

                string response = string.Empty;
                BinaryData binEvents = await BinaryData.FromStreamAsync(req.Body);
                log.LogInformation($"Received events: {binEvents}");

                var events1 = Azure.Messaging.CloudEvent.Parse(binEvents);

                CallAutomationEventBase @event = CallAutomationEventParser.Parse(events1);
                if (@event is RecordingStateChanged rc)
                {
                    //   rc.s
                    //  Console.WriteLine($"{rc.RecordingKind + " " + rc.State + " " + rc.RecordingId + " "}");

                }

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

                        if (rawEventType.Contains("Recording"))
                        {
                            Console.WriteLine(events.ToString());
                        }

                        ///  serverCallId = rawEvent["data"]["serverCallId"].ToObject<string>();
                        //CallConnection
                        switch (rawEventType)
                        {


                            case "Microsoft.Communication.CallConnected":
                                callConnected = DateTime.UtcNow;

                                //  var playSource = new FileSource(new Uri($"{TextToSpeech.BlobStoreBaseUri}/{TextToSpeech.SpeechContainerName}/{callConnectionId}.wav"));

                                serverCallId = rawEvent["data"]["serverCallId"].ToObject<string>(); //callAutomationClient.GetCallConnection(callConnectionId).GetCallConnectionProperties().Value?.ServerCallId;
                                callConnectionId = rawEvent["data"]["callConnectionId"].ToObject<string>();
                                Console.WriteLine("Connection Id " + callConnectionId);
                                correlationId = rawEvent["data"]["correlationId"].ToObject<string>();


                                callConnectionMap.TryAdd(callConnectionId, serverCallId);
                                callConnectionCallIdMap.TryAdd(callConnectionId, correlationId);

                                var hasChainId = req.Headers["X-Microsoft-Skype-Chain-ID"];

                                Console.WriteLine("Call Id " + hasChainId);
                                Console.WriteLine("Server Call Id " + serverCallId);

                                // StartRecording(serverCallId);

                                break;

                            case "Microsoft.Communication.RecordingStateChanged":

                                // if(firstRecordingStateChanged == null)
                                //{
                                firstRecordingStateChanged = DateTime.UtcNow;
                                TimeSpan? diff = firstRecordingStateChanged - callConnected;
                                Console.WriteLine("Recording Start time is : " + diff?.TotalMilliseconds);
                                //}
                               // callAutomationClient.GetCallConnection("").HangUp(true);
                                recordingId = rawEvent["data"]["recordingId"].ToObject<string>();
                                //var part = await callAutomationClient.GetCallConnection(callConnectionId).GetParticipantsAsync();
                                Console.WriteLine("RecordingStateChanged");
                                Console.WriteLine(recordingId);
                                
                                
                               // callAutomationClient.GetCallRecording().Resume(recordingId);
                                //await Task.Delay(2000);



                               // callAutomationClient.GetCallRecording().Pause(recordingId);
                                //await Task.Delay(1000);
                               // callAutomationClient.GetCallConnection(rawEvent["data"]["callConnectionId"].ToObject<string>()).HangUp(true);



                                //  Console.WriteLine(JsonConvert.SerializeObject(rawEvent));

                                break;

                            case "Microsoft.Communication.TeamsComplianceRecordingStateChanged":
                                //watch.Stop();
                                //Console.WriteLine("Time to start Compliance recording: " + watch.ElapsedMilliseconds);
                                recordingId = rawEvent["data"]["recordingId"].ToObject<string>();
                                //var part = await callAutomationClient.GetCallConnection(callConnectionId).GetParticipantsAsync();
                                Console.WriteLine("TeamsRecordingStatus");
                                Console.WriteLine(recordingId);
                                Console.WriteLine(JsonConvert.SerializeObject(rawEvent));


                                break;

                            case "Microsoft.Communication.CallDisconnected":
                                callConnectionId = rawEvent["data"]["callConnectionId"].ToObject<string>();

                                callConnectionMap.TryRemove(callConnectionId, out string servercallId);


                                callConnectionRecordingMap.TryRemove(callConnectionId, out string recording);
                                callConnectionCallIdMap.TryRemove(callConnectionId, out string callId);

                                Console.WriteLine($" Call ended with Call Id {callId}");

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

        [FunctionName("StartRecordingUnmixed")]
        public static async Task<IActionResult> StartRecordingUnmixed(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {
            try
            {
                string serverCallId = null;
                if (req.GetQueryParameterDictionary().ContainsKey("servercallId"))
                {
                    serverCallId = (req.GetQueryParameterDictionary()["servercallId"]);
                }

                //var connectionId = ValidateGetConnectionId(req);


                //if (string.IsNullOrEmpty(serverCallId))
                //{
                //    serverCallId = callConnectionMap[connectionId];
                //}


                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();

                StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId))
                {
                    RecordingContent = RecordingContent.Audio,
                    RecordingChannel = RecordingChannel.Unmixed,
                    RecordingFormat = RecordingFormat.Wav,
                    // PauseOnStart = true,
                    RecordingStateCallbackUri = new Uri(Environment.GetEnvironmentVariable("RecordingStateUrl")),
                    //RecordingStorage = RecordingStorage.CreateAzureCommunicationsRecordingStorage()

                };

                // recordingOptions.ChannelAffinity.Add(new ChannelAffinity(new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("CallerUserIdentifier"))) { Channel = 1 });



                //   recordingOptions.AudioChannelParticipantOrdering.Add(new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("CalleeUserIndentifier")));
                // recordingOptions.AudioChannelParticipantOrdering.Add(new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("TargetPhoneNumber")));
                //   var participant = await callAutomationClient.GetCallConnection(connectionId).GetParticipantsAsync();

                var watch = new Stopwatch();
                watch.Start();
                var start = DateTime.UtcNow;
                // Console.WriteLine($"Start Recording - {start}");
                var response = await callAutomationClient.GetCallRecording()
                  .StartAsync(recordingOptions);

                //  Console.WriteLine($"Time to Start - {watch.ElapsedMilliseconds}");

                //var end = DateTime.UtcNow;

                //Console.WriteLine($"End Recording - {start}");

                //TimeSpan ts = end - start;

                //Console.WriteLine($"{end} -- Time to Start Recording - {ts.TotalMilliseconds}");


                recordingId = response.Value.RecordingId;

                //if (!callConnectionRecordingMap.ContainsKey(connectionId))
                //{
                //    callConnectionRecordingMap.TryAdd(connectionId, recordingId);
                //}
                //else
                //{
                //    callConnectionRecordingMap[connectionId] = recordingId;
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkObjectResult(recordingId);

        }


        [FunctionName("StartRecording")]
        public static async Task<IActionResult> StartRecording(
          [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            try
            {
                await StartRecording(serverCallId);



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkObjectResult("ok");

        }

        private async static Task StartRecording( string servecallId)
        {
            CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();

            CallLocator cl = new ServerCallLocator(serverCallId);

            //callAutomationClient.


            StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId))
            {
                RecordingContent = RecordingContent.Audio,
                RecordingChannel = RecordingChannel.Mixed,
                RecordingFormat = RecordingFormat.Wav,
                RecordingStateCallbackUri = new Uri(Environment.GetEnvironmentVariable("RecordingStateUrl")),
                RecordingStorage = RecordingStorage.CreateAzureCommunicationsRecordingStorage(),
                PauseOnStart = true

                //  RecordingStorage = RecordingStorage.CreateAzureBlobContainerRecordingStorage(new Uri("https://acsrecording.blob.core.windows.net/byos")),                    

            };


            var watch = new Stopwatch();
            watch.Start();
            var start = DateTime.UtcNow;
            // Console.WriteLine($"Start Recording - {start}");

            //if (i == 0)
            //{
            //    var response = await callAutomationClient.GetCallRecording()
            //      .StartAsync(recordingOptions);

            //    FileSource playSource = new FileSource(new Uri("https://acscallrecordingstorage.blob.core.windows.net/test/Conv.wav?sp=r&st=2024-11-15T17:39:55Z&se=2024-11-30T01:39:55Z&spr=https&sv=2022-11-02&sr=b&sig=N7KGXF%2F%2FE3lExC%2FlIG4ZVNZnD6Z%2BoTpqoy8huvUS7qU%3D"));

            //    PlayOptions playOptions = new PlayOptions(playSource, new List<CommunicationIdentifier>());
            //    await callAutomationClient.GetCallConnection(callConnectionId).GetCallMedia().PlayAsync(playOptions);
            //}
            //else
            //{
            //    var response = await callAutomationClient.GetCallRecording()
            //      .StartAsync(recordingOptions);
            //}

            // Task.Delay(10).Wait();

            var response = await callAutomationClient.GetCallRecording()
                .StartAsync(recordingOptions);
        }


        [FunctionName("StopRecording")]
        public static async Task<IActionResult> StopRecording(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {


            IEnumerable<string> vals;
            try
            {
                string recordingId = "";

                if (req.GetQueryParameterDictionary().ContainsKey("recordingId"))
                {
                    recordingId = (req.GetQueryParameterDictionary()["recordingId"]);
                }

                if (string.IsNullOrEmpty(recordingId))
                {
                    var connectionId = ValidateGetConnectionId(req);
                    recordingId = callConnectionRecordingMap[connectionId];
                }


                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
                await callAutomationClient.GetCallRecording().StopAsync(recordingId);
                // callAutomationClient.GetCallConnection(callConnectionId).HangUpAsync(true);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new ExceptionResult(ex, true);
            }

            return new OkObjectResult("ok");

        }

        [FunctionName("CancelPlay")]
        public static async Task<IActionResult> CancelPlay(
         [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
          ILogger log)
        {
            Console.WriteLine("Start Recording");
            CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
            var cancelResult = await callAutomationClient.GetCallConnection(callConnectionId).GetCallMedia().CancelAllMediaOperationsAsync();

            return new OkObjectResult(cancelResult);
        }

        [FunctionName("PlayTo")]
        public static async Task<IActionResult> PlayTo(
   [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
    ILogger log)
        {
            Console.WriteLine("PLay to");
            CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();

            FileSource playSource = new FileSource(new Uri("https://acssolutionstorage.blob.core.windows.net/acs/text.wav?sp=r&st=2023-09-11T19:42:53Z&se=2024-09-12T03:42:53Z&spr=https&sv=2022-11-02&sr=b&sig=a1BMZJVx%2BPAnCbgOAFjlH4Ww1fNeNeanNuBZRnjtNdY%3D"));
            var playOptions = new PlayOptions(playSource, new List<CommunicationIdentifier>() { new MicrosoftTeamsUserIdentifier("296781a1-a30d-4b9d-bd61-dcfa54abfebb") }) { Loop = true };
            await callAutomationClient.GetCallConnection(callConnectionId).GetCallMedia().PlayAsync(playOptions);
            var cancelResult = await callAutomationClient.GetCallConnection(callConnectionId).GetCallMedia().CancelAllMediaOperationsAsync();

            return new OkObjectResult(cancelResult);
        }



        [FunctionName("PauseRecording")]
        public static async Task<IActionResult> PauseRecording(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                string recordingId = "";

                if (req.GetQueryParameterDictionary().ContainsKey("recordingId"))
                {
                    recordingId = (req.GetQueryParameterDictionary()["recordingId"]);

                }

                if (string.IsNullOrEmpty(recordingId))
                {
                    var connectionId = ValidateGetConnectionId(req);
                    recordingId = callConnectionRecordingMap[connectionId];
                }


                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
                var stopRecording = await callAutomationClient.GetCallRecording().PauseAsync(recordingId);
                pauseResponseTime = DateTime.UtcNow;
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
                string recordingId = "";

                if (req.GetQueryParameterDictionary().ContainsKey("recordingId"))
                {
                    recordingId = (req.GetQueryParameterDictionary()["recordingId"]);

                }

                if (string.IsNullOrEmpty(recordingId))
                {
                    var connectionId = ValidateGetConnectionId(req);
                    recordingId = callConnectionRecordingMap[connectionId];
                }


                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
                var stopRecording = await callAutomationClient.GetCallRecording().ResumeAsync(recordingId);
                pauseResponseTime = DateTime.UtcNow;
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

                string recordingId = "";

                if (req.GetQueryParameterDictionary().ContainsKey("recordingId"))
                {
                    recordingId = (req.GetQueryParameterDictionary()["recordingId"]);

                }

                if (string.IsNullOrEmpty(recordingId))
                {
                    var connectionId = ValidateGetConnectionId(req);
                    recordingId = callConnectionRecordingMap[connectionId];
                }


                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
                recordingState = await callAutomationClient.GetCallRecording().GetStateAsync(recordingId);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkObjectResult(recordingState.RecordingState?.ToString());

        }


        [FunctionName("RecordingFileStatus")]
        public static async Task<IActionResult> RecordingFileStatus(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {
            // return new OkObjectResult("ok");

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

            Uri sasurl = new Uri("http://localhost");

            var callId = eventGridEvents[0].Subject.Split("/")[3];
            try
            {

                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClientForDownload();

                var payload = JsonConvert.DeserializeObject<IEnumerable<RecordingFileStatusUpdatedDataPayload>>(content).FirstOrDefault();

                Console.WriteLine("Payload");

                Console.WriteLine(payload);

                foreach (var item in payload.Data.RecordingStorageInfo.RecordingChunks)
                {

                    var recordingDownloadUri = new Uri(item.ContentLocation);
                    var callResponse = await callAutomationClient.GetCallRecording().DownloadStreamingAsync(recordingDownloadUri);


                    var recordingStreamResult = callResponse.GetRawResponse().Status.ToString();


                    var recordingFileName = callId + "--" + item.Index + Guid.NewGuid() + ".wav";

                    var blobStorageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                    var blobStorageContainerName = Environment.GetEnvironmentVariable("ContainerName");
                    var container = new BlobContainerClient(blobStorageConnectionString, blobStorageContainerName);

                    var uploadResult = await container.UploadBlobAsync(recordingFileName,
                      callResponse.Value);



                    BlobClient blobClient = new BlobClient(blobStorageConnectionString, blobStorageContainerName, recordingFileName);

                    var uploadResultStatus = uploadResult.GetRawResponse().Status.ToString();


                    sasurl = blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTime.Now.AddDays(1));




                    var metaDataDownloadUri = new Uri(item.MetadataLocation);
                    var metaDataResponse = await callAutomationClient.GetCallRecording().DownloadStreamingAsync(metaDataDownloadUri);

                    var recordingMetaFileName = callId + "_Content.txt";

                    blobStorageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                    blobStorageContainerName = Environment.GetEnvironmentVariable("ContainerName");
                    container = new BlobContainerClient(blobStorageConnectionString, blobStorageContainerName);

                    uploadResult = await container.UploadBlobAsync(recordingMetaFileName,
                          metaDataResponse.Value);

                    uploadResultStatus = uploadResult.GetRawResponse().Status.ToString();

                    blobClient = new BlobClient(blobStorageConnectionString, blobStorageContainerName, recordingMetaFileName);
                    var sasurl1 = blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTime.Now.AddDays(1));

                    Console.WriteLine("DownLoadRecording :" + sasurl);
                    Console.WriteLine(sasurl1);


                    //SpeechService.ConvertAudioToText(sasurl);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkObjectResult(sasurl);

        }


        [FunctionName("AddTeamsUser")]
        public static async Task<IActionResult> AddTeamsUser(
       [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
        {

            var connectionId = ValidateGetConnectionId(req);

            string teamsUserId = Environment.GetEnvironmentVariable("TeamsUserAADId");

            if (req.GetQueryParameterDictionary().ContainsKey("TeamsUserAADId"))
            {
                teamsUserId = (req.GetQueryParameterDictionary()["TeamsUserAADId"]);
            }


            MicrosoftTeamsUserIdentifier teamsUserIdentifier = new MicrosoftTeamsUserIdentifier(teamsUserId);
            CallInvite ci = new CallInvite(teamsUserIdentifier);
            ci.SourceDisplayName = "Vikas";

            //teamsUserIdentifier?.

            try
            {
                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
                var x = callAutomationClient.GetCallConnection(connectionId).AddParticipant(ci);
                watch.Restart();
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

                var userId = Environment.GetEnvironmentVariable("AnotherUser");

                if (req.GetQueryParameterDictionary().ContainsKey("userId"))
                {
                    userId = (req.GetQueryParameterDictionary()["userId"]);
                }

                CommunicationUserIdentifier cui = new CommunicationUserIdentifier(userId);
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


        [FunctionName("RefreshCache")]
        public static async Task<IActionResult> RefreshCache(
       [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            try
            {
                callConnectionCallIdMap.Clear();
                callConnectionMap.Clear();
                callConnectionRecordingMap.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkObjectResult("Ok");
        }

        [FunctionName("GetCache")]
        public static async Task<IActionResult> GetCache(
       [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            try
            {
                return new OkObjectResult(
                    "CallServerIdMap" + JsonConvert.SerializeObject(callConnectionMap)
                   + "CallIdMap" + JsonConvert.SerializeObject(callConnectionCallIdMap)
                    + "CallRecordingMap" + JsonConvert.SerializeObject(callConnectionRecordingMap)
                    );

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        [FunctionName("AddPhoneUser")]
        public static async Task<IActionResult> AddPhoneUser(
     [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
      ILogger log)
        {
            try
            {
                var connectionId = ValidateGetConnectionId(req);

                string targetPhoneNumber = Environment.GetEnvironmentVariable("TargetPhoneNumber");

                if (req.GetQueryParameterDictionary().ContainsKey("targetPhoneNumber"))
                {
                    targetPhoneNumber = (req.GetQueryParameterDictionary()["targetPhoneNumber"]);
                }

                PhoneNumberIdentifier phone = new PhoneNumberIdentifier(targetPhoneNumber);
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

        [FunctionName("DownLoadRecordingLink")]
        public static async Task<IActionResult> DownloadRecordingLink(
     [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
      ILogger log)
        {

            string url = "";
            try
            {
                string callId = "";

                if (req.GetQueryParameterDictionary().ContainsKey("callId"))
                {
                    callId = (req.GetQueryParameterDictionary()["callId"]);
                }

                if (string.IsNullOrEmpty(callId))
                {
                    throw new ArgumentException("CallId not provided");
                }

                var blobStorageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var blobStorageContainerName = Environment.GetEnvironmentVariable("ContainerName");
                var container = new BlobContainerClient(blobStorageConnectionString, blobStorageContainerName);
                var recordingFileName = callId + ".wav";
                BlobClient blobClient = new BlobClient(blobStorageConnectionString, blobStorageContainerName, recordingFileName);

                if (blobClient.Exists())
                {
                    url = blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTime.Now.AddDays(1)).ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            if (string.IsNullOrEmpty(url))
            {
                return new NotFoundObjectResult("Recording file not found");
            }

            return new OkObjectResult(url);

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
   [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            //log.LogInformation($"Event received: {JsonConvert.SerializeObject(cloudEvents)}");
            Console.WriteLine($"State - {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}");
            TimeSpan t = pauseResponseTime - DateTime.UtcNow;
            //  Console.WriteLine($"Pause/Resume event callback latency {t.TotalMilliseconds}");
            var content = await req.ReadAsStringAsync();

            Task<Class1[]> ts = Task.Run(() =>
            {
                return JsonConvert.DeserializeObject<Class1[]>(content);
            });

            Class1[] x = await ts.ConfigureAwait(false);

            Console.WriteLine(DateTime.UtcNow + " --- " + x[0].data.state.ToString());
            Console.WriteLine(DateTime.UtcNow + " --- " + x[0].data.recordingId.ToString());

            if (pauseResumeExecutedOnce < 1)
            {
                if (x[0].data.state.ToString().ToLower() == "inactive")
                {
                    CallAutomationClient ca = CallAutomationFactory.GetAutomationClient();

                    await ca.GetCallRecording().ResumeAsync(x[0].data.recordingId);
                    Console.WriteLine("resume executed");

                    pauseResumeExecutedOnce++;
                }
            }

            return new OkObjectResult(x[0].data.state.ToString());

        }

        //   [FunctionName("JobRouter")]
        //   public static async Task JobRouter(
        //[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        // ILogger log)
        //   {

        //       var jb = new JobRouter(); ;
        //       await jb.SetupJobRouter();

        //   }


        [FunctionName("RemoveTeamsUser")]
        public static async Task<IActionResult> RemoveTeamsUser(
     [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
      ILogger log)
        {

            var connectionId = ValidateGetConnectionId(req);

            string teamsUserId = Environment.GetEnvironmentVariable("TeamsUserAADId");

            if (req.GetQueryParameterDictionary().ContainsKey("TeamsUserAADId"))
            {
                teamsUserId = (req.GetQueryParameterDictionary()["TeamsUserAADId"]);
            }


            MicrosoftTeamsUserIdentifier teamsUserIdentifier = new MicrosoftTeamsUserIdentifier(teamsUserId);
            CallInvite ci = new CallInvite(teamsUserIdentifier);
            ci.SourceDisplayName = "Vikas";

            try
            {
                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
                var x = await callAutomationClient.GetCallConnection(connectionId).RemoveParticipantAsync(teamsUserIdentifier);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkObjectResult("Ok");

        }


        [FunctionName("RemoveBot")]
        public static async Task<IActionResult> RemoveBot(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
     ILogger log)
        {

            var connectionId = ValidateGetConnectionId(req);



            try
            {
                CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
                var x = await callAutomationClient.GetCallConnection(connectionId).GetParticipantsAsync();

                foreach (var participant in x.Value)
                {

                    if (participant != null)
                    {
                        if (!participant.Identifier.RawId.Contains("8:acs:c3282ab4-1e6d-47f9-b9cc-722e0230a1e2_00000021-5c9d-5fa8-51b9-a43a0d008d68"))
                        {
                            CommunicationIdentifier identifier = new CommunicationUserIdentifier(participant.Identifier.RawId);
                            await callAutomationClient.GetCallConnection(callConnectionId).RemoveParticipantAsync(identifier);

                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkObjectResult("Ok");

        }



        [FunctionName("GenerateChapters")]
        public static async Task<IList<Chapter>> GenerateChapters(
     [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
      ILogger log)
        {


            IList<Chapter> chapters = null;

            Transcript transcript = new Transcript();

            StreamReader reader = new StreamReader(@"C:\Projects\POCs\ACS\ACSSamples\Recording\Transcript2.json");
            string s = reader.ReadToEnd();
            var a = JsonConvert.DeserializeObject<TranscriptItem[]>(s);

            List<Conversationitem> c = new List<Conversationitem>();
            foreach (var chapter in a)
            {
                c.Add(new Conversationitem() { id = chapter.id, text = chapter.text, participantId = chapter.speakerId });

            }

            ChapterHelper.GetChapters(c, a);


            return chapters;

        }


        private static string GetName()
        {
            return null;
        }

        //       [FunctionName("PlayTextToSpeech")]
        //       public static async Task PlayTextToSpeech(
        //[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        // ILogger log)
        //       {
        //           try
        //           {
        //               String textToPlay = "Welcome to Contoso";

        //             //  Provide SourceLocale and VoiceKind to select an appropriate voice.
        //              var playSource = new TextSource(textToPlay, "en-US", VoiceKind.Female);

        //               var playResponse = await CallAutomationFactory.GetAutomationClient().GetCallConnection(callConnectionId)
        //       .GetCallMedia()
        //       .PlayToAllAsync(playSource);
        //           }
        //           catch (Exception ex)
        //           {
        //               Console.Write(ex.ToString());
        //               throw;
        //           }



        //       }


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
