using System.Threading;
using System.Threading.Tasks;

internal interface IScopedProcessingService
{
    Task DoWork(CancellationToken cancellationToken);
}