using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.CognitiveServices.Speech;
using Azure.Storage.Blobs;

namespace EventReminder
{
    public static class TextToSpeech
    {
        [FunctionName("TextToSpeech")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
           // string text,
            //string blobName,           
            ILogger log)
        {

            return new OkResult();
        }


        public static async Task<string> WriteTextToSpeech(string text,string connectionId)
        {
            var config = GetSpeechConfig();

            // ACS server calling PlayAudio API for now only understands mono .wav, 16-bit samples with a 16,000 (16KHz) sampling rate
            // This will give us the desired pcm output, but we still need to add the correct .wav header to it later
            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

            SpeechSynthesisResult result = null;

            using (var synthesizer = new SpeechSynthesizer(config, null))
            {

                try
                {
                    result =  synthesizer.SpeakTextAsync(text).Result;
                }
                catch (Exception ex)
                {

                    throw;
                }
            }
               
           

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                using var audioStream = AudioDataStream.FromResult(result);

                var tempFile = Path.GetTempFileName();
                // write to a temp file to add .wav header to the pcm data
                await audioStream.SaveToWaveFileAsync(tempFile);

                var blobStorageConnectionString = StorageConnectionString;
                var blobStorageContainerName = SpeechContainerName;
                var container = new BlobContainerClient(blobStorageConnectionString, blobStorageContainerName);
                var blob = container.GetBlobClient($"{connectionId}.wav");

                // The file is now compatible with what ACS supports - send it to the Azure blob
                using (var fs = new FileStream(tempFile, FileMode.Open))
                {
                    await blob.UploadAsync(fs, overwrite:true);
                }

                try
                {
                    File.Delete(tempFile);
                }
                catch
                {

                }
             
                return ($"{BlobStoreBaseUri}/{SpeechContainerName}/{connectionId}.wav");
            }

            return "Issue";
;           
        }

        private static SpeechConfig GetSpeechConfig()
        {
            return SpeechConfig.FromSubscription(
                Environment.GetEnvironmentVariable("CognitiveServicesKey"),
                Environment.GetEnvironmentVariable("CognitiveServicesRegion"));
        }

        public static string StorageConnectionString
        {
            get
            {
                return Environment.GetEnvironmentVariable("AzureWebJobsStorage");                
            }
        }

        public static string BlobStoreBaseUri
        {
            get
            {
                return Environment.GetEnvironmentVariable("BlobStoreBaseUri");
            }
        }

        public static string SpeechContainerName
        {
            get
            {
                return Environment.GetEnvironmentVariable("SpeechContainerName");
            }
        }
    }
}
