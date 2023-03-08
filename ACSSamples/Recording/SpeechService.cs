using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Azure.Storage.Blobs;

namespace Recording
{
    internal class SpeechService
    {
        public static async Task ConvertAudioToText(Uri filePath)
        {
            BlobClient blobClient = new BlobClient(filePath);

            var reader = new BinaryReader(blobClient.OpenRead());
            var audioInputStream = AudioInputStream.CreatePushStream();


            byte[] readBytes;
            do
            {
                readBytes = reader.ReadBytes(1024);
                audioInputStream.Write(readBytes, readBytes.Length);
            } while (readBytes.Length > 0);

            var audioConfig = AudioConfig.FromStreamInput(audioInputStream);


            // Create the SpeechConfig object with your Speech-to-Text endpoint ID and authentication token
            var speechConfig = SpeechConfig.FromEndpoint(new Uri(Environment.GetEnvironmentVariable("SpeechServiceEndpoint")), Environment.GetEnvironmentVariable("SpeechServiceKey"));

            audioConfig.SetProperty("DifferentiateGuestSpeakers", "true");
            speechConfig.SetProperty("DifferentiateGuestSpeakers", "true");
            var stopRecognition = new TaskCompletionSource<int>();
            // Create a SpeechRecognizer object with the SpeechConfig object and the audio stream
            using (var recognizer = new SpeechRecognizer(speechConfig, audioConfig))
            {
                // Subscribes to events. 
                recognizer.Recognizing += (s, e) =>
                {
                    Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                };

                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };

                recognizer.Canceled += (s, e) =>
                {
                    Console.WriteLine($"CANCELED: Reason={e.Reason}");

                    if (e.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }

                    stopRecognition.TrySetResult(0);
                };

                recognizer.SessionStarted += (s, e) =>
                {
                    Console.WriteLine("\n    Session started event.");
                };

                recognizer.SessionStopped += (s, e) =>
                {
                    Console.WriteLine("\n    Session stopped event.");
                    Console.WriteLine("\nStop recognition.");
                    stopRecognition.TrySetResult(0);
                };

                // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition. 
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                // Waits for completion. 
                // Use Task.WaitAny to keep the task rooted. 
                Task.WaitAny(new[] { stopRecognition.Task });

                // Stops recognition. 
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

            }

        }
    }
}

