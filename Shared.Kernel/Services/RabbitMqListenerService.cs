using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DotNetEnv;
using Shared.Kernel.Interfaces;

namespace Shared.Kernel.Services;

public class RabbitMqListenerService<TMessage, THandler> : BackgroundService
    where THandler : IRabbitMqMessageHandler<TMessage>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqListenerService<TMessage, THandler>> _logger;
    private readonly string _queueName;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqListenerService(
        IServiceProvider serviceProvider, 
        ILogger<RabbitMqListenerService<TMessage, THandler>> logger,
        string queueName)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _queueName = queueName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? Env.GetString("RABBITMQ_HOST") ?? "localhost",
                Port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? Env.GetString("RABBITMQ_PORT"), out int port) ? port : 5672,
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? Env.GetString("RABBITMQ_USER") ?? "docker",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? Env.GetString("RABBITMQ_PASS") ?? "docker"
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(queue: _queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null,
                                 cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received message from {QueueName}: {Message}", _queueName, message);

                try
                {
                    var payload = JsonSerializer.Deserialize<TMessage>(message);
                    if (payload != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<THandler>();
                        await handler.HandleAsync(payload, stoppingToken);
                    }

                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from RabbitMQ");
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(queue: _queueName,
                                 autoAck: false,
                                 consumer: consumer,
                                 cancellationToken: stoppingToken);

            _logger.LogInformation("RabbitMQ Listener started on {QueueName}.", _queueName);
            
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQ Listener for {QueueName}.", _queueName);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
            await _channel.CloseAsync(cancellationToken: cancellationToken);
        if (_connection != null)
            await _connection.CloseAsync(cancellationToken: cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
