using learning_user_service.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace learning_user_service.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MessageQueue> MessageQueues => Set<MessageQueue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.EntraId).IsUnique();
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_users_created_at").IsDescending();
            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique().HasDatabaseName("uq_user_role");

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents", t => t.HasCheckConstraint("documents_status_check", "status IN ('pending', 'approved', 'rejected')"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Status).HasDefaultValue("pending");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.CreatedBy).HasDatabaseName("idx_documents_created_by");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_documents_status");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_documents_created_at").IsDescending();

            entity.HasOne(e => e.Creator)
                .WithMany(u => u.CreatedDocuments)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Reviewer)
                .WithMany(u => u.ReviewedDocuments)
                .HasForeignKey(e => e.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.OldValues).HasColumnType("jsonb");
            entity.Property(e => e.NewValues).HasColumnType("jsonb");

            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_audit_logs_user_id");
            entity.HasIndex(e => e.Timestamp).HasDatabaseName("idx_audit_logs_timestamp").IsDescending();
            entity.HasIndex(e => new { e.EntityType, e.EntityId }).HasDatabaseName("idx_audit_logs_entity");

            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MessageQueue>(entity =>
        {
            entity.ToTable("message_queue");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Status).HasDefaultValue("pending");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.MaxRetries).HasDefaultValue(3);
            entity.Property(e => e.Payload).HasColumnType("jsonb");

            entity.HasIndex(e => e.Status).HasDatabaseName("idx_message_queue_status");
            entity.HasIndex(e => e.QueueName).HasDatabaseName("idx_message_queue_queue_name");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_message_queue_created_at").IsDescending();
        });
    }
}
