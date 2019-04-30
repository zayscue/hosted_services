using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

internal class ScopedProcessingService : IScopedProcessingService
{
    private readonly ILogger _logger;

    public ScopedProcessingService(ILogger<ScopedProcessingService> logger)
    {
        _logger = logger;
    }

    public Task DoWork(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"{DateTime.UtcNow} - Scoped Processing Service is working.");
        return Task.CompletedTask;
    }
}