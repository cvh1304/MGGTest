using RabbitMQ.Client;

namespace MessageBus.RabbitMQBus.Interfaces;

public interface IRabbitMQConnection : IDisposable
{
    bool IsConnected { get; }

    bool TryConnect();

    bool TryCreateModel(out IModel model);
}
