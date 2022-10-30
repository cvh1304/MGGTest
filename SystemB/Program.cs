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

        messageBus.Subscribe(message =>
        {
            Console.WriteLine(
                $"Recieve message: {message.TextMessage} " +
                $"with priority: {message.PriorityLevel}");
        });

        Console.WriteLine("If you want to stop app, press Ctrl+C");

        while (!_stop)
        {
            await Task.Delay(400);
        }
    }

    private static void ConsoleCancelKeyPress(
        object sender,
        ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        _stop = true;
    }
}


