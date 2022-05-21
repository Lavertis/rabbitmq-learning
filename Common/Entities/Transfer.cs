namespace Common.Entities;

public class Transfer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FromAccountNumber { get; set; } = string.Empty;
    public string ToAccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool? Succeeded { get; set; }
    public string? ReasonFailed { get; set; }
    public string OriginBankName { get; set; } = string.Empty;
    public string DestinationBankName { get; set; } = string.Empty;
}