using Domain.Common;
using Domain.Interfaces;
using Domain.Models;
using MessageBus.RabbitMQBus.Interfaces;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MessageBus.RabbitMQBus.Services;

public class RabbitMQMessageBus : IMessageBus, IDisposable
{
    private readonly IRabbitMQConnection _connection;
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly int _publishRetryCount;

    private readonly ConcurrentDictionary<ulong, Message> _notConfirmedMessages =
        new ConcurrentDictionary<ulong, Message>();

    private IModel _publishChannel;
    private IModel _subscribeChannel;

    public PublishErrorHandler PublishErrorHandler { get; set; }

    public RabbitMQMessageBus(
        IRabbitMQConnection connection,
        string exchangeName,
        string queueName,
        int publishRetryCount)
    {
        _connection = connection;
        _exchangeName = exchangeName;
        _queueName = queueName;
        _publishRetryCount = publishRetryCount;
    }

    public void Dispose()
    {
        if (_publishChannel != null)
        {
            _publishChannel.Dispose();
        }

        if (_subscribeChannel != null)
        {
            _subscribeChannel.Dispose();
        }
    }

    public void Publish(Message message)
    {
        if (!_connection.IsConnected)
        {
            if (!_connection.TryConnect())
            {
                PublishErrorHandler(message);

                return;
            }
        }

        if (_publishChannel == null)
        {
            var channelIsCreated = _connection.TryCreateModel(
                out _publishChannel);

            if (!channelIsCreated)
            {
                PublishErrorHandler(message);

                return;
            }

            _publishChannel.ExchangeDeclare(
                exchange: _exchangeName,
                type: "direct",
                durable: true);

            _publishChannel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: GetQueueArgs());

            _publishChannel.QueueBind(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: _queueName);

            _publishChannel.CallbackException += (_, _) =>
            {
                _publishChannel?.Dispose();
                _publishChannel = null;
            };

            _publishChannel.ConfirmSelect();

            _publishChannel.BasicAcks += OnBasicAck;
            _publishChannel.BasicNacks += OnBasicNack;
        }

        var publishRetryPolicy = Policy
            .Handle<SocketException>()
            .Or<BrokerUnreachableException>()
            .WaitAndRetry(
                _publishRetryCount,
                attempts => TimeSpan.FromSeconds(2 * attempts),
                (e, time) =>
                {
                    // TODO: log
                });

        var result = publishRetryPolicy.ExecuteAndCapture(() =>
        {
            var props = _publishChannel.CreateBasicProperties();
            props.DeliveryMode = 2;
            props.Priority = (byte)message.PriorityLevel;

            var jsonBody = JsonSerializer.Serialize(message);

            _notConfirmedMessages.TryAdd(
                _publishChannel.NextPublishSeqNo,
                message);

            _publishChannel.BasicPublish(
                exchange: _exchangeName,
                routingKey: _queueName,
                basicProperties: props,
                body: Encoding.UTF8.GetBytes(jsonBody));
        });

        if (result.ExceptionType != null)
        {
            PublishErrorHandler(message);
        }
    }

    public void Subscribe(Action<Message> proceedAction)
    {
        if (!_connection.IsConnected)
        {
            if (!_connection.TryConnect())
            {
                return;
            }
        }

        if (_subscribeChannel == null)
        {
            CreateSubscribeChannel();
        }

        StartConsume(proceedAction);
    }

    private void OnBasicAck(
        object sender,
        BasicAckEventArgs args)
    {
        CleanNotConfirmedDictionary(args.DeliveryTag, args.Multiple);
    }

    private void OnBasicNack(
        object sender,
        BasicNackEventArgs args)
    {
        // TODO: publish error handler
    }

    private void CleanNotConfirmedDictionary(
        ulong confirmedNumber,
        bool isMultiple)
    {
        if (isMultiple)
        {
            var confirmed = _notConfirmedMessages
                .Where(x => x.Key <= confirmedNumber);

            foreach (var item in _notConfirmedMessages)
            {
                _notConfirmedMessages.TryRemove(item.Key, out _);
            }
        }
        else
        {
            _notConfirmedMessages.TryRemove(confirmedNumber, out _);
        }
    }

    private void CreateSubscribeChannel()
    {
        var channelIsCreated = _connection.TryCreateModel(
            out _subscribeChannel);

        if (!channelIsCreated)
        {
            throw new IOException(
                "Can not create RabbitMQ subscribe channel");
        }

        _subscribeChannel.ExchangeDeclare(
            exchange: _exchangeName,
            type: "direct",
            durable: true);

        _subscribeChannel.QueueDeclare(
            queue: _queueName,
            durable: true,
            autoDelete: false,
            exclusive: false,
            arguments: GetQueueArgs());

        _subscribeChannel.QueueBind(
            queue: _queueName,
            exchange: _exchangeName,
            routingKey: _queueName);
    }

    private void StartConsume(
        Action<Message> proceedAction)
    {
        AsyncEventingBasicConsumer consumer = new(_subscribeChannel);

        consumer.Received += async (sender, args) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<Message>(
                    Encoding.UTF8.GetString(args.Body.ToArray()));

                proceedAction.Invoke(message);
            }
            finally
            {
                await Task.Delay(2500);

                _subscribeChannel.BasicAck(args.DeliveryTag, multiple: false);
            }
        };

        _subscribeChannel.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            arguments: GetQueueArgs());
    }

    private Dictionary<string, object> GetQueueArgs()
    {
        Dictionary<string, object> queueArgs = new();
        queueArgs.Add("x-max-priority", 9);

        return queueArgs;
    }
}
