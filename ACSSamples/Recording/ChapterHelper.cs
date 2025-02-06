using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Recording
{
    public static class ChapterHelper
    {

        public static async void GetChapters(List<Conversationitem> conversation, TranscriptItem[] transcriptItems)
        {
            // ExecutePS(@"C:\FHL\ffmpeg\bin\ffmpeg -f mp4 -i C:\FHL\ChapterDemo.mp4 -i C:\FHL\chaps.txt -map_metadata 1 -f MP4 -c copy C:\FHL\ChapterDemoUpdated.MP4");

            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "281b4f3cb5994ac3987c7311640c65e4");

            ChapterRequest request = new ChapterRequest();

            List<Conversation> conversations = new List<Conversation>
            {
                new Conversation() { conversationItems = conversation.ToArray(), modality ="text", id ="89" }
            };

            request.analysisInput = new Analysisinput() { Conversations = conversations.ToArray() };

            var parameters = new Parameters();
            parameters.summaryAspects = new string[] { "chapterTitle" };

            request.tasks = new ConvTask[1];
            request.tasks[0] = new ConvTask() { kind = "ConversationalSummarizationTask", parameters = parameters, taskName = "Chapters" };

            var json = JsonConvert.SerializeObject(request);

            var res = await client.PostAsJsonAsync<ChapterRequest>("https://recordingtextanalytic.cognitiveservices.azure.com//language/analyze-conversations/jobs?api-version=2022-10-01-preview", request);

            if (res.IsSuccessStatusCode)
            {
                var status = "";
                Console.WriteLine(res.Headers.GetValues("operation-location").First());
                ChapterResponse cr = null;
                while (status != "succeeded")
                {
                    cr = await client.GetFromJsonAsync<ChapterResponse>(res.Headers.GetValues("operation-location").First());
                    Console.WriteLine("Chapters generation status -" + cr.status);
                    status = cr.status;
                    await Task.Delay(500);
                }

                Console.WriteLine("Chapters Generated:");
                foreach (var item in cr.tasks.items[0].results.conversations[0].summaries)
                {
                    Console.WriteLine($"Start Time {transcriptItems.Where(x => x.id == item.contexts[0].conversationItemId).First().instances[0].start} --" + item.text);
                }

                Console.WriteLine("");
                Console.WriteLine("Create Chapters Metadata file");
                Console.ReadLine();

                TextWriter textWriter = new StreamWriter(@"C:\FHL\Chaps.txt");

                textWriter.WriteLine(";FFMETADATA1");
                Int64 ms = 0;

                foreach (var item in cr.tasks.items[0].results.conversations[0].summaries)
                {
                    ms = ms + 1;
                    textWriter.WriteLine("[CHAPTER]");
                    textWriter.WriteLine("TIMEBASE=1/1");
                    textWriter.WriteLine($"START={ms}");
                   // Console.WriteLine($"Start {TimeSpan.Parse(transcriptItems.Where(x => x.id == item.contexts[0].conversationItemId).First().instances[0].start)} secs {TimeSpan.Parse(transcriptItems.Where(x => x.id == item.contexts[0].conversationItemId).First().instances[0].start).TotalSeconds} ");
                    var end = TimeSpan.Parse(transcriptItems.Where(x => x.id == item.contexts[0].conversationItemId).First().instances[0].start).TotalSeconds;
                    textWriter.WriteLine($"END={end}");
                    textWriter.WriteLine($"title={item.text}");
                    textWriter.WriteLine();
                    ms = (int)end;
                }

                textWriter.Dispose();
                Console.WriteLine("Chapters Metadata file created");
                       

              
                Console.WriteLine("");             
                Console.WriteLine("Update Recording file with Chapters using FFMPEG");
                Console.ReadLine();

                ExecutePS(@"C:\FHL\ffmpeg\bin\ffmpeg -f mp4 -i C:\FHL\ChapterDemo.mp4 -i C:\FHL\chaps.txt -map_metadata 1 -f MP4 -c copy C:\FHL\ChapterDemoUpdated.MP4");

                Console.WriteLine("Recording file updated with Chapters");

            }
        }

        static void ExecutePS(string command)
        {
            using (PowerShell PowerShellInstance = PowerShell.Create())
            {
                // Use the AddScript method to add your PowerShell command
                PowerShellInstance.AddScript(command);

                // Invoke execution on the PowerShell object
                Collection<PSObject> PSOutput = PowerShellInstance.Invoke();

                // Iterate through the output objects
                foreach (PSObject outputItem in PSOutput)
                {
                    // Access each property of the output object
                    if (outputItem != null)
                    {
                        // For example, print the process name
                        Console.WriteLine(outputItem.Properties["ProcessName"].Value);
                    }
                }

            }

        }
    }
}
