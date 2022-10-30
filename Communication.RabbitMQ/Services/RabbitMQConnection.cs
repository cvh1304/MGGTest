using Domain.Common;
using MessageBus.RabbitMQBus.Interfaces;
using MessageBus.RabbitMQBus.Models;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace MessageBus.RabbitMQBus.Services;

public class RabbitMQConnection : IRabbitMQConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly int _retryAttemptsCount;
    private IConnection _connection;
    private bool _disposed;

    public RabbitMQConnection(
        IConnectionFactory connectionFactory,
        int retryAttemptsCount = 10)
    {
        _connectionFactory = connectionFactory;
        _retryAttemptsCount = retryAttemptsCount;
    }

    public bool IsConnected
    {
        get
        {
            return _connection?.IsOpen == true
                && !_disposed;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            _connection?.Dispose();
        }
        catch
        {
            // TODO: Handle exception, log
        }
    }

    public bool TryConnect()
    {
        var retryPolicy = RetryPolicy.Handle<SocketException>()
            .Or<BrokerUnreachableException>()
            .WaitAndRetry(
                _retryAttemptsCount,
                attempt => TimeSpan.FromSeconds(2 * attempt),
                (e, time) =>
                {
                    // TODO: log exception
                });

        retryPolicy.ExecuteAndCapture(() =>
        {
            _connection = _connectionFactory
                .CreateConnection();
        });

        if (IsConnected)
        {
            _connection.ConnectionBlocked +=
                (_, e) => ConnectionBrokeHandler(e.Reason);
            _connection.ConnectionShutdown +=
                (_, e) => ConnectionBrokeHandler(e.ReplyText);
            _connection.CallbackException +=
                (_, e) => ConnectionBrokeHandler(e.Exception.Message);

            return true;
        }

        return false;
    }

    public bool TryCreateModel(out IModel model)
    {
        model = null;

        if (!IsConnected)
        {
            return false;
        }

        try
        {
            model = _connection.CreateModel();

            return true;
        }
        catch
        {
            // TODO: Handle and log
            return false;
        }
    }

    private void ConnectionBrokeHandler(string eventMessage)
    {
        if (_disposed) return;

        // TODO: handle event message

        TryConnect();
    }
}
