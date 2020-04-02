using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Engine
{
    /// <summary>
    /// <see cref="IHostedService"/> implementation for subscribing/unsubscribing to message queues.
    /// </summary>
    public class ManageSQSSubscriptionsService : IHostedService
    {
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly ILogger<ManageSQSSubscriptionsService> logger;

        public ManageSQSSubscriptionsService(IHostApplicationLifetime hostApplicationLifetime,
            ILogger<ManageSQSSubscriptionsService> logger)
        {
            this.hostApplicationLifetime = hostApplicationLifetime;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Hosted service StartAsync");
            
            // TODO subscribe to SQS
            
            hostApplicationLifetime.ApplicationStopping.Register(OnStopping);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Hosted service StopAsync");
            return Task.CompletedTask;
        }

        private void OnStopping()
        {
            // TODO cancel processing, graceful cleanup.
            // in ECS this will be received 20s before kill command
            logger.LogInformation("Hosted service Stopping..");
        }
    }
}