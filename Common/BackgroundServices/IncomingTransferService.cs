using System.Text;
using Common.Config;
using Common.Entities;
using Common.Models;
using Common.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Common.BackgroundServices;

public class IncomingTransferService : BackgroundService
{
    private readonly AccountService _accountService;
    private readonly IModel _channel;
    private readonly MyConfig _config;

    private readonly IConnection _connection;
    private readonly ILogger<IncomingTransferService> _logger;
    private readonly TransferService _transferService;

    public IncomingTransferService(ILogger<IncomingTransferService> logger,
        IOptions<MyConfig> config,
        TransferService transferService,
        AccountService accountService)
    {
        _config = config.Value;
        _logger = logger;

        _transferService = transferService;
        _accountService = accountService;

        _connection = new ConnectionFactory {HostName = "localhost"}.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: _config.SecondBank + "_outgoing_transfers",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.QueueDeclare(queue: _config.SecondBank + "_outgoing_transfers_ack",
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
            var transfer = JsonConvert.DeserializeObject<Transfer>(message)!;

            if (_accountService.AccountExistsByNumber(transfer.ToAccountNumber))
            {
                _logger.LogInformation($"Transfer {transfer.Id} received");
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                SendTransferResponse(transfer.Id, true, null);

                transfer.Succeeded = true;
                _transferService.AddTransfer(transfer);
            }
            else
            {
                _logger.LogInformation($"Transfer {transfer.Id} rejected");
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, requeue: false, multiple: false);
                SendTransferResponse(transfer.Id, false, "Account number not found");
            }

            _logger.LogInformation($"Received {message}");
        };

        _channel.BasicConsume(queue: _config.SecondBank + "_outgoing_transfers", autoAck: false, consumer);
        return Task.CompletedTask;
    }

    private void SendTransferResponse(string transferId, bool succeeded, string? reasonFailed)
    {
        var response = new TransferResponse
        {
            TransferId = transferId,
            Succeeded = succeeded,
            ReasonFailed = reasonFailed
        };
        var sendBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
        _channel.BasicPublish("", _config.SecondBank + "_outgoing_transfers_ack", null, sendBytes);
    }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}