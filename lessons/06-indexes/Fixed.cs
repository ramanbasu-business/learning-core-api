using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace Indexes;

public static class Fixed
{
    /// <summary>
    /// YOUR TURN - runs once before the timed Fixed run (and is not timed).
    ///
    /// Create an index that helps this query:
    ///     WHERE status = 'Shipped' AND order_date >= '2026-09-01'
    ///     ORDER BY order_date DESC, order_number
    ///     LIMIT 100
    ///
    ///   - Table: orders. Column names are snake_case: status, order_date, order_number.
    ///   - Put the column that is compared with "=" first, then the column used for the range and the sort.
    ///   - Run the SQL with:   await db.Database.ExecuteSqlRawAsync("CREATE INDEX ...");
    ///   - Then refresh the table statistics:   ExecuteSqlRawAsync("ANALYZE orders");
    ///
    /// The runner restores the baseline database afterwards, so the index disappears again.
    /// </summary>
    public static async Task SetupAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDb();

        // await db.Database.ExecuteSqlRawAsync("CREATE INDEX ... ON orders (...)");
        // await db.Database.ExecuteSqlRawAsync("ANALYZE orders");

        await Task.CompletedTask;
        throw new NotImplementedException("TODO - open lessons/06-indexes/Fixed.cs and write the CREATE INDEX statement (see README, 'Your turn').");
    }

    // The query itself is already written: it is the same as Naive's. The lesson is the index, not the LINQ.
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx) => await Naive.QueryAsync(ctx);
}
