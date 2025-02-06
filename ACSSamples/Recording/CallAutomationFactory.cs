using Azure.Communication.CallAutomation;
using Azure.Core;
using Azure.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recording
{
    public class CallAutomationFactory
    {
        public static CallAutomationClient GetAutomationClient()
        {
            var connectionString = Environment.GetEnvironmentVariable("connectionString");

            var pmaUrl = Environment.GetEnvironmentVariable("PMAUrl");

            CallAutomationClient callAutomationClient = null;

            if (!string.IsNullOrEmpty(pmaUrl))
            {
                callAutomationClient = new CallAutomationClient(pmaEndpoint: new Uri(pmaUrl), connectionString);
                // callAutomationClient = new CallAutomationClient(connectionString);

                //string userAssignedClientId = "62d626ea-9dcb-4e04-b9ee-d41d8f310633";
                //TokenCredential credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId });

                //callAutomationClient = new CallAutomationClient(new Uri(pmaUrl), credential: credential);
            }
            else
            {

                callAutomationClient = new CallAutomationClient(connectionString);
                //TokenCredential tc = ;
                //VisualStudioCredential credential = new VisualStudioCredential();
                //callAutomationClient = new CallAutomationClient(new Uri("https://noam-test-resource.unitedstates.communication.azure.com"), credential, new CallAutomationClientOptions(CallAutomationClientOptions.ServiceVersion.V2024_04_15));
            }


            return callAutomationClient;
        }

        public static CallAutomationClient GetAutomationClientForDownload()
        {
            return new CallAutomationClient(Environment.GetEnvironmentVariable("connectionString"));
        }

    }
}
