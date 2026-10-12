using EfCoreLab.Common.Data;
using EfCoreLab.Common.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EfCoreLab.Common.Running;

/// <summary>Handed to Naive/Fixed. Creates DbContexts that point at the lab database and are query-counted.</summary>
public sealed class LessonContext
{
    internal LessonContext(MyDatabaseOptions options, QueryCounter counter)
    {
        Options = options;
        Counter = counter;
    }

    public MyDatabaseOptions Options { get; }
    public QueryCounter Counter { get; }

    /// <summary>
    /// The EF Core options (connection, snake_case naming, query counter) used by every lesson context.
    /// Pass <paramref name="configure"/> only if a lesson needs to change them (for example to add an interceptor).
    /// </summary>
    public DbContextOptions<AppDbContext> NewOptions(Action<DbContextOptionsBuilder>? configure = null)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();

        builder.UseNpgsql(Options.WorkingConnection);
        builder.UseSnakeCaseNamingConvention();
        builder.AddInterceptors(Counter);
        configure?.Invoke(builder);

        return builder.Options;
    }

    /// <summary>A fresh AppDbContext. Create one per variant (or per unit of work), as you would in a real app.</summary>
    public AppDbContext NewDbContext(Action<DbContextOptionsBuilder>? configure = null)
    {
        return new AppDbContext(NewOptions(configure));
    }
}
