using EfCoreLab.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace EfCoreLab.Common.Data;

/// <summary>
/// The one and only DbContext, used by every lesson. Lessons never subclass it: when a lesson needs
/// something different it uses a query option (AsNoTracking, IgnoreQueryFilters, ...) or an options
/// hook on LessonContext.NewDb(). The tables are created from this model when the baseline DB is built.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderProduct> OrderProducts => Set<OrderProduct>();
    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MessageQueue> MessageQueues => Set<MessageQueue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Sales domain ---------------------------------------------------------------
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.City).HasMaxLength(100);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Sku).HasMaxLength(40);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.UnitPrice).HasPrecision(12, 2);
            e.HasIndex(x => x.Sku).IsUnique();
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).HasMaxLength(30);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.TotalAmount).HasPrecision(12, 2);
            e.HasIndex(x => x.OrderNumber).IsUnique();
            // NOTE: no index on (status, order_date) on purpose - the indexing lesson adds it.

            e.HasOne(x => x.Customer).WithMany(c => c.Orders)
                .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SalesRep).WithMany(u => u.AssignedOrders)
                .HasForeignKey(x => x.SalesRepId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderProduct>(e =>
        {
            e.ToTable("order_products");
            e.HasKey(x => new { x.OrderId, x.ProductId });
            e.Property(x => x.UnitPrice).HasPrecision(12, 2);

            e.HasOne(x => x.Order).WithMany(o => o.OrderProducts)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany(p => p.OrderProducts)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderNote>(e =>
        {
            e.ToTable("order_notes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).HasMaxLength(1000);

            e.HasOne(x => x.Order).WithMany(o => o.OrderNotes)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany(u => u.AuthoredNotes)
                .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Identity (carried over from the original learning-core-api) ------------------
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.EntraId).IsUnique();
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_users_created_at").IsDescending();
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.IsDeleted).HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        });

        // Soft delete: soft-deleted users are excluded from every query unless IgnoreQueryFilters() is used.
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique().HasDatabaseName("uq_user_role");

            e.HasOne(x => x.User).WithMany(u => u.UserRoles)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.OldValues).HasColumnType("jsonb");
            e.Property(x => x.NewValues).HasColumnType("jsonb");
            e.HasIndex(x => x.UserId).HasDatabaseName("idx_audit_logs_user_id");
            e.HasIndex(x => x.Timestamp).HasDatabaseName("idx_audit_logs_timestamp").IsDescending();
            e.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("idx_audit_logs_entity");

            e.HasOne(x => x.User).WithMany(u => u.AuditLogs)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MessageQueue>(e =>
        {
            e.ToTable("message_queue");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Status).HasDefaultValue("pending");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.RetryCount).HasDefaultValue(0);
            e.Property(x => x.MaxRetries).HasDefaultValue(3);
            e.Property(x => x.Payload).HasColumnType("jsonb");
            e.HasIndex(x => x.Status).HasDatabaseName("idx_message_queue_status");
            e.HasIndex(x => x.QueueName).HasDatabaseName("idx_message_queue_queue_name");
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_message_queue_created_at").IsDescending();
        });
    }
}
