using System.Threading.Tasks;

namespace Shared.Kernel.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(string queueName, string eventName, T data, int priority = 3);
}
