﻿using Azure.Communication.CallAutomation;
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
               // callAutomationClient = new CallAutomationClient(pmaEndpoint: new Uri(pmaUrl), connectionString);
                callAutomationClient = new CallAutomationClient(pmaEndpoint: new Uri(pmaUrl), connectionString);
            }
            else
            {

                callAutomationClient = new CallAutomationClient(connectionString);
            }

          
            return callAutomationClient;
        }

        public static CallAutomationClient GetAutomationClientForDownload()
        {
            return new CallAutomationClient(Environment.GetEnvironmentVariable("connectionString"));
        }

    }
}
