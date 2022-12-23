using Azure;
using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Recording
{
    public static class Callback
    {
        private static string serverCallId = "";
        private static string recordingId = "";

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
                        serverCallId = rawEvent["data"]["serverCallId"].ToObject<string>();
                        //CallConnection
                        switch (rawEventType)
                        {
                            case "Microsoft.Communication.CallConnected":

                                // var playSource = new FileSource(new Uri($"{TextToSpeech.BlobStoreBaseUri}/{TextToSpeech.SpeechContainerName}/{callConnectionId}.wav"));
                                var playOptions = new PlayOptions() { Loop = true };
                                serverCallId = callAutomationClient.GetCallConnection(callConnectionId).GetCallConnectionProperties().Value.ServerCallId;

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

        [FunctionName("StartRecording")]
        public static async Task<IActionResult> StartRecording(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable("connectionString"); // Find your Communication Services resource in the Azure portal
                CallAutomationClient callAutomationClient = new CallAutomationClient(connectionString);


                StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId))
                {
                    RecordingContent = RecordingContent.Audio,
                    RecordingChannel = RecordingChannel.Unmixed,
                    RecordingFormat = RecordingFormat.Wav,
                    RecordingStateCallbackEndpoint = new Uri(Environment.GetEnvironmentVariable("Callbackurl"))
                };

                Response<RecordingStateResult> response = await callAutomationClient.GetCallRecording()
                .StartRecordingAsync(recordingOptions);

                recordingId = response.Value.RecordingId;


            }
            catch (Exception ex)
            {
                // handle exception
            }

            return new OkResult();

        }


        [FunctionName("StopRecording")]
        public static async Task<IActionResult> StopRecording(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
             ILogger log)
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable("connectionString"); // Find your Communication Services resource in the Azure portal
                CallAutomationClient callAutomationClient = new CallAutomationClient(connectionString);

                var stopRecording = await callAutomationClient.GetCallRecording().StopRecordingAsync(recordingId);
            }
            catch (Exception ex)
            {
                // handle exception
            }

            return new OkResult();

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

                var connectionString = Environment.GetEnvironmentVariable("connectionString"); // Find your Communication Services resource in the Azure portal


                CallAutomationClient callAutomationClient = new CallAutomationClient(connectionString);

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
