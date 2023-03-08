using Azure.Communication.CallAutomation;
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
            CallAutomationClient callAutomationClient = new CallAutomationClient(connectionString);
            return callAutomationClient;
        }

    }
}
