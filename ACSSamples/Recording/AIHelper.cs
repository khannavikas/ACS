using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
//using Azure.AI.Language.Conversations;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Azure;

namespace Recording
{
    public static class AIHelper
    {
       public static Uri endpoint = new("<endpoint>");
      public static  AzureKeyCredential credential = new("<apiKey>");
        public static TextAnalyticsClient client = new(endpoint, credential);

        public static string RedactPI(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            try
            {
               ;

                Response<PiiEntityCollection> response = client.RecognizePiiEntities(text, null);
                PiiEntityCollection entities = response.Value;

                Console.WriteLine($"Redacted Text: {entities.RedactedText}");
                Console.WriteLine();
                Console.WriteLine($"Recognized {entities.Count} PII entities:");
                foreach (PiiEntity entity in entities)
                {
                    Console.WriteLine($"  Text: {entity.Text}");
                    Console.WriteLine($"  Category: {entity.Category}");
                    if (!string.IsNullOrEmpty(entity.SubCategory))
                        Console.WriteLine($"  SubCategory: {entity.SubCategory}");
                    Console.WriteLine($"  Confidence score: {entity.ConfidenceScore}");
                    Console.WriteLine();
                }
            }
            catch (RequestFailedException exception)
            {
                Console.WriteLine($"Error Code: {exception.ErrorCode}");
                Console.WriteLine($"Message: {exception.Message}");
            }

            return text;

        }

    }
}
