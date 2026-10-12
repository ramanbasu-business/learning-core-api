using EfCoreLab.Common.Data;
using EfCoreLab.Common.Domain;
using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace Tracking;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDbContext();


        // Load the same 50,000 orders as Naive.cs, but tell EF Core NOT to track them.
        // (Open Naive.cs and copy the query. You only need to add one thing to it.)
        //
        // Then return the result the same way Naive does:
        //
        //     return Naive.Describe(orders, db);
        //
        // When it works, the runner should show:
        //   - the same number of rows and "OK - same result as Naive"
        //   - "entities tracked by EF: 0"
        //   - the same number of queries (1), but a lower time
        // ----------------------------------------------------------------------------------

        var orders = await db.Orders
            .AsNoTracking()
            .Where(o => o.Status == "Delivered")
            .OrderBy(o => o.OrderNumber)
            .Take(50_000)
            .ToListAsync();

        return Naive.Describe(orders, db);
    }
}
