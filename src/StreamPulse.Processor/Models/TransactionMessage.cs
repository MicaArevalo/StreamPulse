namespace StreamPulse.Processor.Models;

internal record TransactionMessage
{
    public string AccountId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ProcessingTimeMs { get; init; }
    public string? FailureReason { get; init; }
}
