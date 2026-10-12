namespace EfCoreLab.Common.Domain;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = default!;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }   // jsonb
    public string? NewValues { get; set; }   // jsonb
    public DateTime? Timestamp { get; set; }
}
