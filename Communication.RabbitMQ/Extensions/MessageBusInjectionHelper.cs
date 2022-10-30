using Domain.Interfaces;
using MessageBus.RabbitMQBus.Interfaces;
using MessageBus.RabbitMQBus.Models;
using MessageBus.RabbitMQBus.Services;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace MessageBus.RabbitMQBus.Extensions;

public static class MessageBusInjectionHelper
{
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