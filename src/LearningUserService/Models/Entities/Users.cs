// Models/Entities/User.cs
namespace learning_user_service.Models.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? Name { get; set; }
    public string? EntraId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Document> CreatedDocuments { get; set; } = new List<Document>();
    public ICollection<Document> ReviewedDocuments { get; set; } = new List<Document>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

// Models/Entities/Role.cs
public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

// Models/Entities/UserRole.cs
public class UserRole
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;
}

// Models/Entities/Document.cs
public class Document
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Content { get; set; }
    public string Status { get; set; } = "pending";
    public Guid CreatedBy { get; set; }
    public User Creator { get; set; } = default!;
    public Guid? ReviewedBy { get; set; }
    public User? Reviewer { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Models/Entities/AuditLog.cs
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = default!;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime? Timestamp { get; set; }
}

// Models/Entities/MessageQueue.cs
public class MessageQueue
{
    public Guid Id { get; set; }
    public string QueueName { get; set; } = default!;
    public string MessageType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string? Status { get; set; } = "pending";
    public DateTime? CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
}
