namespace Common.Models;

public class TransferResponse
{
    public string TransferId { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? ReasonFailed { get; set; }
}