using System;
using System.Threading;
using System.Threading.Tasks;

public interface ITimer : IDisposable
{
    Task Start();
    Task Start(CancellationToken cancellationToken);
    Task Stop();
}