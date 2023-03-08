using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Recording
{
    public class OpenAIClient
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public OpenAIClient(string apiKey )
        {
            if(string.IsNullOrEmpty(apiKey))
            {
                _apiKey = Environment.GetEnvironmentVariable("OpenAPIKey");
            }
            
            _apiKey = apiKey;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.openai.com/v1/")
            };
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
        }

        public async void GenerateText(string prompt, int maxTokens, float temperature)
        {
            var request = new
            {
                prompt = prompt,
                max_tokens = maxTokens,
                temperature = temperature
            };
            var requestJson = JsonConvert.SerializeObject(request);
            var response = await _httpClient.PostAsync("/completions",
                new StringContent(requestJson, Encoding.UTF8, "application/json"));
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(responseJson);
            Console.WriteLine(result.choices[0].text);
        }
    }
}
