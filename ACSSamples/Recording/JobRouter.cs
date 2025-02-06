//using Azure.Communication.JobRouter;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;
//using System.Threading.Tasks;

//namespace Recording
//{
//    public class JobRouter
//    {

//        public async Task SetupJobRouter()
//        {
//            try {
//                var connectionString = Environment.GetEnvironmentVariable("connectionString");
//                var routerAdminClient = new JobRouterAdministrationClient(connectionString);
//                var routerClient = new JobRouterClient(connectionString);

//                var distributionPolicy = await routerAdminClient.CreateDistributionPolicyAsync(
//                                        new CreateDistributionPolicyOptions(
//                                            distributionPolicyId: "distribution-policy-1",
//                                            offerExpiresAfter: TimeSpan.FromMinutes(1),
//                                            mode: new LongestIdleMode())
//                                        {
//                                            Name = "jrdp1"
//                                        }

//                                        );

//                var queue = await routerAdminClient.CreateQueueAsync(
//        new CreateQueueOptions(queueId: "queue1111", distributionPolicyId: distributionPolicy.Value.Id)
//        {
//            Name = "vkjrq"
//        });
//                var job = await routerClient.CreateJobAsync(
//        new CreateJobOptions(jobId: "job516", channelId: "voice", queueId: queue.Value.Id)
//        {
//            Priority = 1,
//            RequestedWorkerSelectors =
//            {
//            new RouterWorkerSelector(key: "expert", labelOperator: LabelOperator.GreaterThan, value: new LabelValue(10))
//            }
//        });

              

//                var worker = await routerClient.CreateWorkerAsync(
//        new CreateWorkerOptions(workerId: "worker11", totalCapacity: 1)
//        {
//            QueueIds = { [queue.Value.Id] = new RouterQueueAssignment() },
//            Labels = { ["expert"] = new LabelValue(11) },
//            ChannelConfigurations = { ["voice"] = new ChannelConfiguration(capacityCostPerJob: 1) },
//            AvailableForOffers = true
//        });

//                await Task.Delay(TimeSpan.FromSeconds(10));
//                worker = await routerClient.GetWorkerAsync(worker.Value.Id);
//                foreach (var offer in worker.Value.Offers)
//                {
//                    Console.WriteLine($"Worker {worker.Value.Id} has an active offer for job {offer.JobId}");
//                }


//                var accept = await routerClient.AcceptJobOfferAsync(worker.Value.Id,  worker.Value.Offers.FirstOrDefault().OfferId);
//                var accept1 = await routerClient.AcceptJobOfferAsync(worker.Value.Id, worker.Value.Offers.FirstOrDefault().OfferId);
//                Console.WriteLine($"Worker {worker.Value.Id} is assigned job {accept.Value.JobId}");

//                await routerClient.CompleteJobAsync(new CompleteJobOptions(accept.Value.JobId, accept.Value.AssignmentId));
//               // await routerClient.CompleteJobAsync(new CompleteJobOptions(accept.Value.JobId, accept.Value.AssignmentId));
//                Console.WriteLine($"Worker {worker.Value.Id} has completed job {accept.Value.JobId}");

//                await routerClient.CloseJobAsync(new CloseJobOptions(accept.Value.JobId, accept.Value.AssignmentId)
//                {
//                    DispositionCode = "Resolved"
//                });

//                //await routerClient.CloseJobAsync(new CloseJobOptions(accept.Value.JobId, accept.Value.AssignmentId)
//                //{
//                //    DispositionCode = "Resolved1"
//                //});
//                Console.WriteLine($"Worker {worker.Value.Id} has closed job {accept.Value.JobId}");

//                await routerClient.DeleteJobAsync("");
//                await routerClient.DeleteJobAsync(accept.Value.JobId);
//                Console.WriteLine($"Deleting job {accept.Value.JobId}");

//                return;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.ToString() );

//            }
//        }
       

//    }
//}
