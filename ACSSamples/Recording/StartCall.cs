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
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Recording
{
    public static class StartCall
    {
        [FunctionName("StartCall")]

        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {



            CallAutomationClient callAutomationClient = CallAutomationFactory.GetAutomationClient();
            CommunicationUserIdentifier caller = new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("CalleeUserIndentifier"));




            CommunicationUserIdentifier callee = new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("CalleeUserIndentifier"));

            CallInvite callInviteUser = new CallInvite(callee);

            PhoneNumberIdentifier phone = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("TargetPhoneNumber"));
            PhoneNumberIdentifier callerPhone = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("ACSCallerPhoneNumber"));
            CallInvite ci = new CallInvite(phone, callerPhone);

            // CreateCallOptions callOptions = new CreateCallOptions(callInvite,                 
            //   callbackUri: new Uri(Environment.GetEnvironmentVariable("Callbackurl")));

            // /**** Old code 16.1
            // CallSource callSource = new CallSource(new CommunicationUserIdentifier(Environment.GetEnvironmentVariable("CallerUserIdentifier")));

            //  callSource.CallerId = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("ACSCallerPhoneNumber"));

            // callSource.DisplayName = "Contoso";
            //   CommunicationIdentifier callee = new PhoneNumberIdentifier(Environment.GetEnvironmentVariable("CallePhoneNumber")); 

            // CreateCallOptions callOptions = new CreateCallOptions(callSource, targets: new List<CommunicationIdentifier>() { callee },
            // callbackUri: new Uri(Environment.GetEnvironmentVariable("Callbackurl")));

            // Old Code end

            CreateCallResult call;
            try
            {
                CallInvite callInvite = null;

                if (req.GetQueryParameterDictionary().ContainsKey("PhoneCall"))
                {
                    callInvite = ci;
                }
                else
                {
                    callInvite = callInviteUser;
                }

                call = callAutomationClient.CreateCall(callInvite, callbackUri: new Uri(Environment.GetEnvironmentVariable("Callbackurl")));
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
                throw;
            }

            return new OkObjectResult(call.CallConnection.CallConnectionId);
        }

        [FunctionName("Transcription")]
        public static async Task<IActionResult> Transcription(
             [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var url = "https://acssolutionstorage.blob.core.windows.net/acs/0-eus-d1-a65e90fb5d62ce9d4c5f1c7969629b73-0-2023-02-16T08%3A09%3A03.6876504Z.wav?sp=r&st=2023-02-17T05:57:37Z&se=2023-02-17T13:57:37Z&spr=https&sv=2021-06-08&sr=b&sig=IWX5RxwMXbbGBHCFcs%2BkGxG9zYAZIWyo3lab6R9YN9E%3D";

            await SpeechService.ConvertAudioToText(new Uri(url));


            return new OkResult();
        }
    }
}

