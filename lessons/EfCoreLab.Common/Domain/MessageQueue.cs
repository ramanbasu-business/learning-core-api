namespace EfCoreLab.Common.Domain;

/// <summary>Outbox-style table (used by the transactional outbox lesson).</summary>
public class MessageQueue
{
    public Guid Id { get; set; }
    public string QueueName { get; set; } = default!;
    public string MessageType { get; set; } = default!;
    public string Payload { get; set; } = default!;   // jsonb
    public string? Status { get; set; } = "pending";
    public DateTime? CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
}
