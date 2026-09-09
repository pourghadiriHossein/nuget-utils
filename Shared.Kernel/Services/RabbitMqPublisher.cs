using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using DotNetEnv;
using Shared.Kernel.Interfaces;

namespace Shared.Kernel.Services;

public class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private async Task EnsureConnectionAsync()
    {
        if (_channel != null) return;

        await _semaphore.WaitAsync();
        try
        {
            if (_channel != null) return;

            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? Env.GetString("RABBITMQ_HOST") ?? "localhost",
                Port = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? Env.GetString("RABBITMQ_PORT"), out int port) ? port : 5672,
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? Env.GetString("RABBITMQ_USER") ?? "docker",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? Env.GetString("RABBITMQ_PASS") ?? "docker"
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task PublishAsync<T>(string queueName, string eventName, T data, int priority = 3)
    {
        await EnsureConnectionAsync();

        // Ensure priority is clamped between 1 and 4 for JSON payload
        int payloadNumericPriority = priority switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 4,
            _ => 3
        };

        // Ensure the queue exists before publishing
        await _channel!.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var payload = new
        {
            @event = eventName,
            priority = payloadNumericPriority,
            data = data
        };

        var message = JsonSerializer.Serialize(payload);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            Persistent = true
        };

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
            await _channel.CloseAsync();
        
        if (_connection != null)
            await _connection.CloseAsync();
            
        _semaphore.Dispose();
    }
}
