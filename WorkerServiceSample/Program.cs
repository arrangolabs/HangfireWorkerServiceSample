using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.SqlServer;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace WorkerServiceSample
{
    public partial class Program
    {
        static ILogger<Program> _logger;

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            CreateHostBuilder(args).Build().Run();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var args = e;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureLogging(loggerFactory => loggerFactory.AddEventLog())
                .ConfigureServices(services =>
                {
                    //-- Based on https://github.com/HangfireIO/Hangfire/blob/master/samples/NetCoreSample/Program.cs
                    
                    services.TryAddSingleton<SqlServerStorageOptions>(new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.FromTicks(1),
                        UseRecommendedIsolationLevel = true,
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(1)
                    });

                    services.TryAddSingleton<IBackgroundJobFactory>(x => new CustomBackgroundJobFactory(
                        new BackgroundJobFactory(x.GetRequiredService<IJobFilterProvider>())));

                    services.TryAddSingleton<IBackgroundJobPerformer>(x => new CustomBackgroundJobPerformer(
                        new BackgroundJobPerformer(
                            x.GetRequiredService<IJobFilterProvider>(),
                            x.GetRequiredService<JobActivator>(),
                            TaskScheduler.Default)));

                    services.TryAddSingleton<IBackgroundJobStateChanger>(x => new CustomBackgroundJobStateChanger(
                            new BackgroundJobStateChanger(x.GetRequiredService<IJobFilterProvider>())));

                    services.AddHangfire((provider, options) =>
                    {
                        options.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
                        options.UseSimpleAssemblyNameTypeSerializer();
                        options.UseSqlServerStorage(
                            "Server=(localdb)\\mssqllocaldb; Database=Automation; Trusted_Connection=True; MultipleActiveResultSets=true",
                            provider.GetRequiredService<SqlServerStorageOptions>());
                    });

                    services.AddHangfireServer(options =>
                    {
                        options.StopTimeout = TimeSpan.FromSeconds(15);
                        options.ShutdownTimeout = TimeSpan.FromSeconds(30);
                    });

                    services.AddHostedService<HangfireBackgroundJobService>();
                });
    }
}
