using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Recording
{
    public static class PostRecordingInsight
    {


        [FunctionName("GenerateRecordingAIInsight")]
        public static async Task<IActionResult> SummariseRecording(
[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
 ILogger log)
        {
            try
            {

                var recordingFilePath = @"C:\\Users\\vikhann\\Downloads\\Conv.Wav";

                if (req.GetQueryParameterDictionary().ContainsKey("recordingFilePath"))
                {
                    recordingFilePath = (req.GetQueryParameterDictionary()["recordingFilePath"]);
                }

                // Use Indexing service and generate transcription for recording and get local path of the file
                var captionFilePath = await RecordingInsight.GetTranscription(recordingFilePath);

                OpenAIClient1 ai = new OpenAIClient1();

                // Read all the caption file
                string text = File.ReadAllText(captionFilePath, Encoding.UTF8);

                // Use Open API to generate other insight based on the caption and summary
                var summary = await ai.GenerateText($"Summarize conversation between Customer and Agent: {text}");
                var issue = await ai.GenerateText($"What was the issue faced by customer in 20 words: {text}");
                var resolved = await ai.GenerateText($"Was customer problem resolved based on conversation?: {summary}");
                var steps = await ai.GenerateText($"How was issue resolved based on the conversation?: {text}");
                var sentiments = await ai.GenerateText($"What is Customer sentiment Postive or Negative: {summary}");
                var action = await ai.GenerateText($"Follow up action on Internet plans for agent from this conversation: {text}");

                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine("Call Summary: " + summary.Replace("\n", ""));
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine("\n Customer Issue: " + issue.Replace("\n", ""));
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine("\n Was Issue resolved: " + resolved.Replace("\n", ""));
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine("\n Resolution: " + steps.Replace("\n", ""));
                Console.WriteLine("--------------------------------------------------------");

                Console.WriteLine("\n Conversation sentiments: " + sentiments.Replace("\n", ""));
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine("\n" + action.Replace("\n", ""));
                Console.WriteLine("--------------------------------------------------------");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

            return new OkObjectResult("Ok");

        }
    }
}
