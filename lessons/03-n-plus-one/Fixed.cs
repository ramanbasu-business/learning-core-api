using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace NPlusOne;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDbContext();

        // ----------------------------------------------------------------------------------
        // YOUR TURN
        //
        // Produce the same 500 CustomerSummary objects with ONE query - no loop, no query inside a loop.
        //
        //   - Same customers as Naive.cs: AsNoTracking, OrderBy(c => c.Email), Take(500).
        //   - Let the database count and add up the orders: inside Select(...) you can write
        //     c.Orders.Count() and c.Orders.Sum(o => o.TotalAmount).
        //
        // Then return the result the same way Naive does:
        //
        //     return Naive.Describe(summaries);
        //
        // When it works, the runner should show:
        //   - 500 rows and "OK - same result as Naive"
        //   - Queries: 501 -> 1
        // ----------------------------------------------------------------------------------

        var summaries = await db.Customers
        .AsNoTracking()
        .OrderBy(c => c.Email)
        .Take(500)
        .Select(c => new CustomerSummary
        {
            Name = c.Name,
            OrderCount = c.Orders.Count,
            TotalSpent = c.Orders.Sum(o => o.TotalAmount)
        })
        .AsSplitQuery()
        .ToListAsync();


        return Naive.Describe(summaries);
    }
}
