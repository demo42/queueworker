using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Important
{
    public class QueueProcessorService : IHostedService
    {
        private Task _worker;
        private readonly CancellationTokenSource _tokenSource;
        private readonly IConfiguration _config;
        private readonly ILogger<QueueProcessorService> _logger;

        public QueueProcessorService(IConfiguration config, ILogger<QueueProcessorService> logger)
        {
            _config = config;
            _logger = logger;
            _tokenSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _worker = DoWork(_tokenSource.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _tokenSource.Cancel();
            if (await Task.WhenAny(_worker, Task.Delay(20000)) != _worker)
            {
                //Didn't stop gracefully in timeout. We should do something about that.
            }
        }

        public async Task DoWork(CancellationToken cancellationToken)
        {
            var storageAccount = CloudStorageAccount.Parse(_config["StorageConnectionString"]);

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            CloudQueue queue = queueClient.GetQueueReference(_config["QueueName"]);

            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await queue.GetMessageAsync();

                //If there are no messages then sleep for a bit and wait.
                //ONe common pattern here is to do exponential sleep time
                //up to a max. If overnight there is no work then you want
                //to sleep as much as possible, for example.
                if(message == null)
                {
                    await Task.Delay(10000);
                    continue;
                }

                //We aren't actually doing anything with this data.
                //WE will log that we recieved it and wait for a bit to simulate
                //work. Can observe data with logs in k8s.
                _logger.LogInformation("Processing data {data}", message.AsString);
                await Task.Delay(1000);
                await queue.DeleteMessageAsync(message);
                _logger.LogInformation("Processing data complete.");
            }
        }
    }
}