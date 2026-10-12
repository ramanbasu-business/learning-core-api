using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace SplitQueryLesson;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var context = ctx.NewDbContext();

        // ----------------------------------------------------------------------------------
        // YOUR TURN
        //
        // Load the same 5,000 orders with their lines AND their notes, but tell EF Core to
        // send ONE QUERY PER COLLECTION instead of one giant JOIN.
        //
        //   - Copy the query from Naive.cs. Add one method call to it.
        //   - Keep the OrderBy before Take: split queries need a stable order to line the pieces up.
        //
        // Then return the result the same way Naive does:
        //
        //     return Naive.Describe(orders);
        //
        // When it works, the runner should show:
        //   - 5,000 rows and "OK - same result as Naive"
        //   - Queries: 1 -> 3   (MORE queries - but each one is small and narrow)
        //   - run with --sql to see the three statements
        // ----------------------------------------------------------------------------------

        /**
        Consistency: the 3 queries run at different moments. If a row is written between them, the results can disagree.
        To avoid that, wrap them in a transaction (Repeatable Read or Snapshot isolation).

        Round trips: 3 trips instead of 1. On a slow network, or when the collections are small, single-query mode can win.

        Setting it ALWAYS: UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery) in the options turns it on everywhere.
        Then .AsSingleQuery() switches it back off for one query.

        Where to put the call: AsSplitQuery() can go anywhere in the chain before ToListAsync().
        **/

        var orders = await context.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderProducts)
                    .Include(o => o.OrderNotes)
                    .OrderBy(o => o.OrderNumber)
                    .Take(5_000)
                    .AsSplitQuery()
                    .ToListAsync();

        return Naive.Describe(orders);
    }
}
