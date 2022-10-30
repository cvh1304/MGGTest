namespace MessageBus.RabbitMQBus.Models;

public class RabbitSettings
{
    public int RetryConnectCount { get; set; }

    public Uri Uri { get; set; }

    public string ExchangeName { get; set; }

    public string QueueName { get; set; }

    public int RetryPublishCount { get; set; }
}
