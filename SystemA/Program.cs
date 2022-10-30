using Data.SqliteEF;
using Data.SqliteEF.Extensions;
using Domain.Interfaces;
using Domain.Models;
using MessageBus.RabbitMQBus.Extensions;
using MessageBus.RabbitMQBus.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

class Programm
{
    private static volatile bool _stop = false;
    private static Random _random = new Random();

    public static async Task Main(string[] args)
    {
        Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelKeyPress);
        await RunAsync();
    }

    static async Task RunAsync()
    {
        string environment = Environment
            .GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var rabbitSettings = configuration.GetSection(
            "RabbitMQ");

        ServiceCollection serviceCollection = new();

        serviceCollection.AddSqliteEFDbContext();

        serviceCollection.AddRabitMQMessageBus(new RabbitSettings
        {
            Uri = new Uri(rabbitSettings[nameof(RabbitSettings.Uri)]),
            ExchangeName = rabbitSettings[nameof(RabbitSettings.ExchangeName)],
            QueueName = rabbitSettings[nameof(RabbitSettings.QueueName)],
            RetryConnectCount = int.Parse(rabbitSettings[nameof(RabbitSettings.RetryConnectCount)]),
            RetryPublishCount = int.Parse(rabbitSettings[nameof(RabbitSettings.RetryPublishCount)])
        });

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        var messageBus = serviceProvider.GetRequiredService<IMessageBus>();
        var messagesDbContext = serviceProvider.GetRequiredService<MessagesDbContext>();

        messageBus.PublishErrorHandler += message =>
        {
            messagesDbContext.Add(message);
        };

        Console.WriteLine("If you want to stop app, press Ctrl+C");

        for (var i = 10;; i++)
        {
            var msg = new Message
            {
                PriorityLevel = _random.Next(0, 9),
                TextMessage = GenerateString()
            };

            SendMessage(messageBus, msg);

            await Task.Delay(_random.Next(5, 5 * i));

            if (_stop)
            {
                Console.WriteLine($"Was generated {i - 9} messages");

                break;
            }
        }
    }

    private static string GenerateString()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789" +
            "abcdefghijklmnopqrstuvwxyz/*-+ ";

        return new string(
                Enumerable.Repeat(chars, _random.Next(10, 500))
            .Select(x => x[_random.Next(x.Length)]).ToArray());
    }

    private static void SendMessage(
        IMessageBus messageBus,
        Message message)
    {
        messageBus.Publish(message);
    }

    private static void ConsoleCancelKeyPress(
        object sender,
        ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        _stop = true;
    }
}