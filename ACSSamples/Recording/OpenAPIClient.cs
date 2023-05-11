using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI_API.Completions;
using OpenAI_API;
using OpenAI;
using OpenAI.Net;

namespace Recording
{
    public class OpenAIClient
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;


        public OpenAIClient(string apiKey)
        {

            if (string.IsNullOrEmpty(apiKey))
            {
                _apiKey = Environment.GetEnvironmentVariable("OpenAPIKey");
            }

        }

        public async Task GenerateText(string prompt, int maxTokens = 0)
        {
           // Console.WriteLine(prompt);
            OpenAI_API.OpenAIAPI api = new OpenAI_API.OpenAIAPI(_apiKey);

            var result = await api.Completions.CreateCompletionAsync(prompt, max_tokens:200);
            Console.WriteLine(result.Completions[0].Text);

            return;
        }


        public async Task RedactText(string text)
        {
                    // Define a text string that contains PII
            string text1 = "John Doe's email address is johndoe@example.com";

            // Define a list of PII entities to redact
            string[] pii_entities = { "name", "email address" };

            CompletionRequest cr = new CompletionRequest();

          //  cr.StopSequence = "\n";
            cr.MaxTokens = 250;
            cr.Prompt =  $"Redact the following PII entities from the given text:\n\n{string.Join("\n", pii_entities)}\n\n{text1}";

            cr.Prompt = $"Extract Key words from the given text:{text}";

            OpenAI_API.OpenAIAPI api = new OpenAI_API.OpenAIAPI(_apiKey);

            var result =  await api.Completions.CreateCompletionAsync(cr);

            // Print the redacted text
            Console.WriteLine("Redacted Text: " + result.Completions[0].Text.Trim());


        }
    }
}
