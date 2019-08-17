using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerServiceSample
{
    public class HangfireBackgroundJobServer : BackgroundService
    {
        readonly ILogger<HangfireBackgroundJobServer> _logger; 
        readonly IBackgroundJobClient _backgroundJobClient;
        readonly IRecurringJobManager _recurringJobManager;

        public HangfireBackgroundJobServer(
            ILogger<HangfireBackgroundJobServer> logger,
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
            _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting background service.");

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping background service.");
            
            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _recurringJobManager.AddOrUpdate(() => Console.WriteLine($"Job excuted at {DateTime.Now}"), "*/15 * * * * *");

            return Task.CompletedTask;
        }
    }
}
