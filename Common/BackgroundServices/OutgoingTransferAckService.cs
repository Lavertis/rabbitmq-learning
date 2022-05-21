using System.Text;
using Common.Config;
using Common.Models;
using Common.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Common.BackgroundServices;

public class OutgoingTransferAckService : BackgroundService
{
    private readonly IModel _channel;
    private readonly MyConfig _config;

    private readonly IConnection _connection;
    private readonly ILogger<OutgoingTransferAckService> _logger;

    private readonly TransferService _transferService;

    public OutgoingTransferAckService(ILogger<OutgoingTransferAckService> logger,
        IOptions<MyConfig> config,
        TransferService transferService)
    {
        _config = config.Value;
        _logger = logger;
        _transferService = transferService;
        _connection = new ConnectionFactory {HostName = "localhost"}.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: _config.CurrentBank + "_outgoing_transfers_ack",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.Span);
            var transferResponse = JsonConvert.DeserializeObject<TransferResponse>(message)!;

            if (_transferService.TransferExistsById(transferResponse.TransferId))
            {
                _transferService.SetTransferStatus(transferResponse);
            }

            _logger.LogInformation($"Received {message}");
        };

        _channel.BasicConsume(queue: _config.CurrentBank + "_outgoing_transfers_ack", autoAck: true, consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}