using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace FilteredInclude;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDb();

        // ----------------------------------------------------------------------------------
        // YOUR TURN
        //
        // Load the same 1,000 customers, each with ONLY their 3 most recent orders, and do the
        // trimming INSIDE the Include so the database never sends the other ~17 orders.
        //
        //   - Same customers as Naive.cs (AsNoTracking, OrderBy email, Take 1,000).
        //   - EF Core lets you filter and order inside Include:
        //         .Include(c => c.Orders.OrderBy...().Take(...))
        //     Use exactly the same ordering as Naive: OrderDate descending, then OrderNumber descending, first 3.
        //   - No loop afterwards.
        //
        // Then return the result the same way Naive does:
        //
        //     return Naive.Describe(customers);
        //
        // When it works, the runner should show:
        //   - 1,000 rows and "OK - same result as Naive"
        //   - Queries: 1 -> 1, but fewer rows come back from the database (~3,000 orders instead of ~20,000)
        // ----------------------------------------------------------------------------------

        throw new NotImplementedException("TODO - open lessons/09-filtered-include/Fixed.cs and write the filtered Include (see README, 'Your turn').");
    }
}
