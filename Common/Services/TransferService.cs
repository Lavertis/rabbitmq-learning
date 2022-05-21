using System.Text;
using Common.Config;
using Common.Entities;
using Common.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Common.Services;

public class TransferService
{
    private readonly MyConfig _config;
    private readonly ILogger<TransferService> _logger;
    private readonly List<Transfer> _transfers = new();

    public TransferService(IOptions<MyConfig> config, ILogger<TransferService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public List<Transfer> GetAllTransfers()
    {
        return _transfers;
    }

    public void AddTransfer(Transfer transfer)
    {
        _transfers.Add(transfer);
    }

    public bool TransferExistsById(string transferId)
    {
        return _transfers.Any(transfer => transfer.Id == transferId);
    }

    public void SetTransferStatus(TransferResponse response)
    {
        var transfer = _transfers.First(transfer => transfer.Id == response.TransferId);
        transfer.Succeeded = response.Succeeded;
        transfer.ReasonFailed = response.ReasonFailed;
    }

    public void SendTransfer(SendTransferRequest request)
    {
        var transfer = new Transfer
        {
            FromAccountNumber = request.FromAccountNumber,
            ToAccountNumber = request.ToAccountNumber,
            Amount = request.Amount,
            OriginBankName = _config.CurrentBank,
            DestinationBankName = _config.SecondBank
        };
        AddTransfer(transfer);

        var factory = new ConnectionFactory {HostName = "localhost"};
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(
            queue: _config.CurrentBank + "_outgoing_transfers",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var sendBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(transfer));
        channel.BasicPublish("", _config.CurrentBank + "_outgoing_transfers", null, sendBytes);
        _logger.LogInformation($"Transfer {transfer} sent to " + _config.CurrentBank);
    }
}