using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Recording
{
    public class RecordingInsight
    {

        // Replace with your Azure Video Indexer API endpoint and key and AccountId
        static string apiUrl = "https://api.videoindexer.ai";
        static string apiKey = Environment.GetEnvironmentVariable("VideoIndexerAPIKey");
        static string accountId = "";
        static string location = "trial";

        public static async Task<string> GetTranscription(string recordingFilePath)
        {
            try
            {

                // Create HttpClient
                using (HttpClient client = new HttpClient())
                {

                    System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

                    // create the http client
                    var handler = new HttpClientHandler();
                    handler.AllowAutoRedirect = false;

                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

                    // obtain account access token
                    var accountAccessTokenRequestResult = client.GetAsync($"{apiUrl}/auth/{location}/Accounts/{accountId}/AccessToken?allowEdit=true").Result;
                    var accountAccessToken = accountAccessTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

                    client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
                    client.DefaultRequestHeaders.Add($"Authorization", $"Bearer {accountAccessToken}");

                    // upload recording content
                    var content = new MultipartFormDataContent();

                    // get the video from URL
                    // var videoUrl = url; // replace with the video URL

                    // as an alternative to specifying video URL, is to upload a file as in the code below, 
                    // video url didn't work for some reason but should be preferred way rather than uploding the file.
                    //client.PostAsync($"https://api.videoindexer.ai/trial/Accounts/a9fee7e3-8f3d-4ce6-bd06-ac4b7d8dc8d3/Videos?name=test&videoUrl={url}", content).Result; //client.PostAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos?accessToken={accountAccessToken}&name=vkription=vktest&privacy=private&partition=some_partition", content).Result;


                    // Uploading a local file here 
                    var recordingFilepath = recordingFilePath;
                    FileStream video = File.OpenRead(recordingFilepath);

                    var streamcontent = new StreamContent(video);
                    streamcontent.Headers.Add("content-type", "application/octet-stream");
                    streamcontent.Headers.Add("content-length", video.Length.ToString());
                    content.Add(streamcontent, "audio", Path.GetFileName(recordingFilepath));

                    // Get the video from URL
                    var queryParams = CreateQueryString(
                    new Dictionary<string, string>()
                    {
                      {"accessToken", accountAccessToken},
                      {"name", "FHL Audio sample"},
                      {"description", "Recording Insight"},
                      {"privacy", "private"},
                      {"partition", "partition"},
                    });

                    Console.WriteLine("Uploading the Audio file");
                    var uploadRequestResult = await client.PostAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos?{queryParams}", content);

                    var uploadResult = uploadRequestResult.Content.ReadAsStringAsync().Result;

                    // get the video id from the upload result
                    var videoId = JsonConvert.DeserializeObject<dynamic>(uploadResult)["id"];
                    Console.WriteLine($"Uploaded the Audio file and generated file Id is {videoId} ");

                    //Debug.WriteLine("Video ID: " + videoId);

                    // obtain video access token
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
                    var videoTokenRequestResult = client.GetAsync($"{apiUrl}/auth/{location}/Accounts/{accountId}/Videos/{videoId}/AccessToken?allowEdit=true").Result;
                    var videoAccessToken = videoTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

                    client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

                    Console.WriteLine($"File Indexing Started");

                    // wait for the video index to finish
                    while (true)
                    {
                        Thread.Sleep(5000);

                        var videoGetIndexRequestResult = client.GetAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/Index?accessToken={videoAccessToken}&language=English").Result;
                        var videoGetIndexResult = videoGetIndexRequestResult.Content.ReadAsStringAsync().Result;

                        var processingState = JsonConvert.DeserializeObject<dynamic>(videoGetIndexResult)["state"];

                        Debug.WriteLine("");
                        Debug.WriteLine("State:");
                        Console.WriteLine("File indexing State: " + processingState);

                        // job is finished
                        if (processingState != "Uploaded" && processingState != "Processing")
                        {
                            Debug.WriteLine("");
                            Debug.WriteLine("Full JSON:");
                            Debug.WriteLine(videoGetIndexResult);
                            break;
                        }
                    }

                    Console.WriteLine("Generating Caption....");

                    // Get the VTT file URL
                    var vttCaptionUrl = $"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/Captions?language=English&format=srt";

                    var vttResponse = await client.GetAsync(vttCaptionUrl);
                    vttResponse.EnsureSuccessStatusCode();
                    var vttContent = await vttResponse.Content.ReadAsStringAsync();

                    // Download the VTT file
                    var vttFilePath = $"C:\\FHL\\Caption_Demo.vtt";
                    await File.WriteAllTextAsync(vttFilePath, vttContent);

                    Console.WriteLine("Caption Generation Finished");

                    Console.WriteLine($"Caption Generation file saved at: {vttFilePath}");

                    Console.WriteLine("Generating Insight....");

                    var indexUrl = $"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/Index";
                    vttResponse = await client.GetAsync(indexUrl);
                    vttResponse.EnsureSuccessStatusCode();
                    vttContent = await vttResponse.Content.ReadAsStringAsync();

                    // Download the VTT file
                    var insightFilePath = @"C:\FHL\\ConvInsight.json";
                    await File.WriteAllTextAsync(insightFilePath, vttContent);

                    Console.WriteLine("Generating Insight Finished");

                    Console.WriteLine($"Insight file saved: {insightFilePath}");

                    return vttFilePath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        private static string CreateQueryString(IDictionary<string, string> parameters)
        {
            var queryParameters = HttpUtility.ParseQueryString(string.Empty);
            foreach (var parameter in parameters)
            {
                queryParameters[parameter.Key] = parameter.Value;
            }

            return queryParameters.ToString();
        }

    }
}
