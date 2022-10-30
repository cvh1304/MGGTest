using Domain.Interfaces;
using MessageBus.RabbitMQBus.Interfaces;
using MessageBus.RabbitMQBus.Models;
using MessageBus.RabbitMQBus.Services;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace MessageBus.RabbitMQBus.Extensions;

/// <summary>
/// Helper for register
/// into <see cref="IServiceCollection"/>
/// services for using RabbitMQBus.
/// </summary>
public static class MessageBusInjectionHelper
{
    /// <summary>
    /// Register
    /// <see cref="RabbitMQConnection"/> - singleton,
    /// with <see cref="ConnectionFactory"/> having giving settings;
    /// Register <see cref="RabbitMQMessageBus"/> - singleton.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="settings">Rabbit settings.</param>
    /// <returns></returns>
    public static IServiceCollection AddRabitMQMessageBus(
        this IServiceCollection services,
        RabbitSettings settings)
    {
        services.AddSingleton<IRabbitMQConnection>(sp =>
        {
            var connectionFactory = new ConnectionFactory()
            {
                Uri = settings.Uri,
                AutomaticRecoveryEnabled = true,
                RequestedHeartbeat = TimeSpan.FromSeconds(30),
                DispatchConsumersAsync = true,
                ConsumerDispatchConcurrency = 1
            };

            return new RabbitMQConnection(
                connectionFactory,
                settings.RetryConnectCount);
        });

        services.AddSingleton<IMessageBus>(sp =>
        {
            var connection = sp.GetRequiredService<IRabbitMQConnection>();

            return new RabbitMQMessageBus(
                connection,
                settings.ExchangeName,
                settings.QueueName,
                settings.RetryPublishCount);
        });

        return services;
    }
}