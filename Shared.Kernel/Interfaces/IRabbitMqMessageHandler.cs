using System.Threading;
using System.Threading.Tasks;

namespace Shared.Kernel.Interfaces;

public interface IRabbitMqMessageHandler<in T>
{
    Task HandleAsync(T message, CancellationToken cancellationToken);
}
