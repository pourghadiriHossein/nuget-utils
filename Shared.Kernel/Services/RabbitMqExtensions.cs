using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotNetEnv;
using Shared.Kernel.Interfaces;

namespace Shared.Kernel.Services;

public static class RabbitMqExtensions
{
    public static IServiceCollection AddRabbitMqListener<TMessage, THandler>(
        this IServiceCollection services, 
        string queueEnvVarName)
        where THandler : class, IRabbitMqMessageHandler<TMessage>
    {
        services.AddScoped<THandler>();
        
        services.AddHostedService(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<RabbitMqListenerService<TMessage, THandler>>>();
            
            // Get Queue name from env
            var queueName = Environment.GetEnvironmentVariable(queueEnvVarName) 
                            ?? Env.GetString(queueEnvVarName);
                            
            if (string.IsNullOrEmpty(queueName))
            {
                // Fallback directly to the variable name if it wasn't found in env, though normally it should be in .env
                queueName = queueEnvVarName; 
            }

            return new RabbitMqListenerService<TMessage, THandler>(provider, logger, queueName);
        });

        return services;
    }

    public static IServiceCollection AddRabbitMqPublisher(this IServiceCollection services)
    {
        // Publisher should be a singleton to reuse the connection and channel efficiently
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        return services;
    }
}
