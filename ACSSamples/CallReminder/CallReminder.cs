using Azure.Communication;
using Azure.Communication.CallingServer;
using Azure.Communication.Sms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace EventReminder
{
    public static class CallReminder
    {
        [FunctionName("CallReminder")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var content = await req.ReadAsStringAsync();

            ReminderRequest rr = JsonConvert.DeserializeObject<ReminderRequest>(content);

            var connectionString = Environment.GetEnvironmentVariable("connectionString"); // Find your Communication Services resource in the Azure portal
            CallAutomationClient callAutomationClient = new CallAutomationClient(connectionString);

         

            CallSource callSource = new CallSource(            
       new CommunicationUserIdentifier("##ACS configured User")// Your Azure Communication Resource Guid Id used to make a Call
       );
            callSource.CallerId = new PhoneNumberIdentifier("##ACS configured Phone number");


            CommunicationIdentifier callee = null;


            if (!string.IsNullOrEmpty(rr.PhoneNumber))
            {
                var sms = new SmsClient(connectionString);
                SmsSendResult sendResult = await sms.SendAsync(
                            from: "##ACS configured Phone number",
                            to: rr.PhoneNumber,
                            message: $"Hi {rr.UserName}, this is a reminder message for upcoming event {rr.MeetingSubject}"
                        ) ;
                callee = new PhoneNumberIdentifier(rr.PhoneNumber);
            }
            else
            {
                // Predefined to test
                callee = new CommunicationUserIdentifier("##ACS configured user");               
            }

            CreateCallResult createCallResult = callAutomationClient.CreateCall(
                              source: callSource,
                          targets: new List<CommunicationIdentifier>() { callee },
                          callbackUri: new Uri("api/Callback")
              );

            var connectionId = createCallResult.CallConnection.CallConnectionId;

            TextToSpeech.WriteTextToSpeech($"Hi {rr.UserName}, this is a reminder call for upcoming event {rr.MeetingSubject}", connectionId);

            return new OkObjectResult("ok");
        }
    }
}

