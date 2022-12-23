using Azure.Communication;
using Azure.Communication.CallAutomation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Recording
{
    public static class StartCall
    {
        [FunctionName("StartCall")]
       
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var connectionString = Environment.GetEnvironmentVariable("connectionString");
            CallAutomationClient callAutomationClient = new CallAutomationClient(connectionString);

            CallSource callSource = new CallSource(
                                            new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("CallerUserIdentifier"))

       );
            callSource.CallerId = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("ACSCallerPhoneNumber"));


            CommunicationIdentifier callee = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("CallePhoneNumber")); 

            CreateCallOptions callOptions = new CreateCallOptions(callSource,
                 targets: new List<CommunicationIdentifier>() { callee },
                          callbackUri: new Uri(Environment.GetEnvironmentVariable("Callbackurl")));

            callAutomationClient.CreateCall(callOptions);

            return new OkResult();
        }
    }
}

