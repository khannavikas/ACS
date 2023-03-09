# Getting started - local setup

This sample project demostrates Recording capabilities of Azure Communication Service, follow below steps:-

1. Install pre-requisites

1. Git Clone the repository 

1. Create supporting Azure resources (ACS & Storage Account)

1. Start ngrok

1. Update local config settings

1. Build and launch the `ACSSamples\ACSSamples.sln` project

1. Register for the Recording File status EventGrid event


## Pre-requisities

1. [VS 2022](https://visualstudio.microsoft.com/vs/)

1. [Git bash](https://git-scm.com/downloads)

1. [ngrok](https://ngrok.com/)

1. [ARMClient](https://github.com/projectkudu/ARMClient)

1. [Postman] (https://www.postman.com/downloads/)

## Git Clone

Open Git bash window and run git clone

## Create ACS resources

* [Create a Storage account and a container within it](https://docs.microsoft.com/azure/storage/common/storage-account-overview)
* [Create ACS resoucre](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/create-communication-resource?tabs=windows&pivots=platform-azp)
* [Provision ACS phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number?tabs=windows&pivots=platform-azp)
* [Create ACS user identity](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/identity/quick-create-identity)

## Start ngrok

Start ngrok proxy forwarding to local port 7288 

```shell
ngrok.exe http 7288
```
"https" url the ngrok gives you ("https://e917-2604-3d08-8480-200-c5e0-b086-5f34-a6db.ngrok.io"
)- you'll need to put it into the server's config
later. You'll also use it with `armclient` to register your EventGrid listener.


## Update local config settings

Create `.Recoding\local.settings.json` if it doesn't exist, and initialize it
this way:

```json
{
  
 "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "Azure Storage connection string",
    "BlobStoreBaseUri": "Azure Blob Storage Base Url",
    "ContainerName": "Storage ContainerName",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "connectionString": "ACS resource connection string",
    "Callbackurl": "<NGROK URL>/api/Callback",
    "ACSCallerPhoneNumber": "ACS configured Phone number",
    "CallePhoneNumber": "Actual phone number to be called",
    "CallerUserIdentifier": "ACS configured user identifier",
    "TeamsUserAADId":"<Teams User AAD ID>",
    "AnotherUser":"ACS User ID",
    "CalleeUserIndentifier":"ACS configured user identifier to be called"
  }

}
```

## Build and launch the Azure function project

* Open the ACSSample solution file in VS 2022
* Build the solution 
* Set Recording as startup project
* Run the project

## Register for Recording File Status Updated EventGrid event

Once `ngrok` and the Azure Functions Project is running, you should be able to
subscribe to the Recoridng file updated status event in the EventGrid. When subscription is
first created, EventGrid will immediately issue a validation callback,
so you should see activity in the `ngrok` console and in your `func host start`
console.

First do `armclient login` and make sure to use your _debug account to log in.

Then, to subscribe for the V2 resource, issue the command below. Be sure to
replace vaules in angular brackects (<>) as required:

* <YOUR_NGROK_HTTPS_URL> will be the endpoint EventGrid invokes, which will
then forward the request to your localhost,


```shell
armclient put "/subscriptions/<Azure Subscription Name>/resourceGroups/<Resource Group Name>/providers/Microsoft.Communication/CommunicationServices/<ACS resoucre name>/providers/Microsoft.EventGrid/eventSubscriptions/<EventSub>?api-version=2020-06-01" "{'properties':{'destination':{'properties':{'endpointUrl':'<YOUR_NGROK_HTTPS_URL>/api/RecordingFileStatus'},'endpointType':'WebHook'},'filter':{'includedEventTypes': ['Microsoft.Communication.RecordingFileStatusUpdated']}}}" -verbose
```
