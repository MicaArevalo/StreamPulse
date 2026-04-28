namespace StreamPulse.Producer.Models;

public record TransactionEvent
{
    public Guid TransactionId { get; init; } = Guid.NewGuid();
    public string AccountId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "ARS";
    public TransactionType Type { get; init; }
    public TransactionChannel Channel { get; init; }
    public TransactionStatus Status { get; init; }
    public string? FailureReason { get; init; }
    public int ProcessingTimeMs { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public enum TransactionType { TRANSFER, PAYMENT, DEPOSIT, WITHDRAWAL }
public enum TransactionChannel { ONLINE, ATM, POS }
public enum TransactionStatus { COMPLETED, FAILED, PENDING }
