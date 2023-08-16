using Azure;
using Azure.AI.OpenAI;
using System;
using System.Threading.Tasks;

namespace Recording
{
    public class OpenAIClient1
    {
        // Replace with your OpenAI Url and Key 
        const string endpoint = "https://acsrecordingchatgpt.openai.azure.com/";
        OpenAIClient client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(Environment.GetEnvironmentVariable("AzureOpenAIKey")));

        public async Task<string> GenerateText(string prompt, int maxTokens = 0)
        {
            try
            {
                var result = await client.GetCompletionsAsync("text-davinci-003", new CompletionsOptions { MaxTokens = 200, Prompts = { prompt } });
                return result.Value.Choices[0].Text;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return string.Empty;
        }

    }
}
