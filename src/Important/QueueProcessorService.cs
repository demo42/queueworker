using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace Important
{
    public class QueueProcessorService : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<QueueProcessorService> _logger;
        private readonly ImportantBackend _backend;

        public QueueProcessorService(IConfiguration config,
                                     ILogger<QueueProcessorService> logger,
                                     ImportantBackend backend)
        {
            _config = config;
            _logger = logger;
            _backend = backend;            
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var storageAccount = CloudStorageAccount.Parse(_config["StorageConnectionString"]);
            int loopDelay = _config.GetValue<int>("LoopDelay");
            _logger.LogInformation("LoopDelay=" + loopDelay.ToString());

            bool exitOnComplete=_config.GetValue<bool>("ExitOnComplete");
            _logger.LogInformation("ExitOnComplete=" + exitOnComplete.ToString());
            _backend.Delay = loopDelay;

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            CloudQueue queue = queueClient.GetQueueReference(_config["QueueName"]);


            while (!stoppingToken.IsCancellationRequested)
            {
                var message = await queue.GetMessageAsync();

                //If there are no messages then sleep for a bit and wait.
                //One common pattern here is to do exponential sleep time
                //up to a max. If overnight there is no work then you want
                //to sleep as much as possible, for example.
                if (message == null)
                {
                    await Task.Delay(loopDelay);
                    if (exitOnComplete)
                    {
                        _logger.LogInformation("Message Processing Complete");
                        Environment.Exit(0);
                    } else
                    {
                        _logger.LogInformation("Waiting for new messages: {} milliseconds",loopDelay);
                        continue;
                    }
                }

                if (message.DequeueCount > 3)
                {
                    //TODO: here you would transfer the message to table/blob storage
                    //for later recovery and analysis. Kind of a manual dead letter queue.
                    //This would handle bad messages.
                    _logger.LogCritical("Giving up on processing {messageId} : {messageContent}", message.Id, message.AsString);
                    await queue.DeleteMessageAsync(message);
                }

                try
                {
                    //We aren't actually doing anything with this data.
                    //We will log that we received it and wait for a bit to simulate
                    //work. Can observe data with logs in k8s.
                    _logger.LogInformation("Processing data: {data}", message.AsString);
                    await _backend.SubmitData(message.AsString);
                    await queue.DeleteMessageAsync(message);
                    //_logger.LogInformation("Processing data complete.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "unknown error processing message {messageId}", message.Id);
                }
            }
       }
    }
}