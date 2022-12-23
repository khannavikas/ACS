using Azure;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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
            try
            {
                var content = await req.ReadAsStringAsync();
                var connectionString = Environment.GetEnvironmentVariable("connectionString"); // Find your Communication Services resource in the Azure portal


                CallAutomationClient callAutomationClient = new CallAutomationClient(connectionString);

                var payload = JsonConvert.DeserializeObject<IEnumerable<RecordingFileStatusUpdatedDataPayload>>(content).FirstOrDefault();

                foreach (var item in payload.Data.RecordingStorageInfo.RecordingChunks)
                {
                    var recordingDownloadUri = new Uri(item.ContentLocation);
                    var response = await callAutomationClient.GetCallRecording().DownloadStreamingAsync(recordingDownloadUri);


                    var recordingStreamResult = response.GetRawResponse().Status.ToString();


                    var recordingFileName = item.DocumentId + "-" + item.Index + "-" +
                        payload.Data.RecordingStartTime + ".wav";

                    var blobStorageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                    var blobStorageContainerName = Environment.GetEnvironmentVariable("ContainerName");
                    var container = new BlobContainerClient(blobStorageConnectionString, blobStorageContainerName);

                    var uploadResult = await container.UploadBlobAsync(recordingFileName,
                        response.Value);

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
