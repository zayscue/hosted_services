using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class TimedHostedService : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private ITimer _timer;

    public IServiceProvider Services { get; }

    public TimedHostedService(IServiceProvider services,
        ILogger<TimedHostedService> logger)
    {
        Services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Timed Background Service is starting.");

        using (var scope = Services.CreateScope())
        {
            var scopedProcessingService =
                scope.ServiceProvider.GetRequiredService<IScopedProcessingService>();
            _timer = new TimerAsync(scopedProcessingService.DoWork, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }
        await _timer.Start(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Timed Background Service is stopping.");

        await _timer.Stop();
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}