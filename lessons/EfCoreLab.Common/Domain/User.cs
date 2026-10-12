namespace EfCoreLab.Common.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? Name { get; set; }
    public string? EntraId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Order> AssignedOrders { get; set; } = new List<Order>();
    public ICollection<OrderNote> AuthoredNotes { get; set; } = new List<OrderNote>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
