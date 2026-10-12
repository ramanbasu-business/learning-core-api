using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace Projection;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var context = ctx.NewDbContext();

        // ----------------------------------------------------------------------------------
        // YOUR TURN
        //
        // Build the same list of 20,000 OrderListItem objects, but let the DATABASE do the shaping:
        // use Select(...) to create each OrderListItem directly inside the query.
        //
        //   - Same ordering and same Take(20_000) as Naive.cs.
        //   - No Include(...) is needed: inside Select you can simply write o.Customer.Name
        //     and EF adds the join for you.
        //   - Inside Select, "how many lines does this order have?" is o.OrderProducts.Count.
        //
        // Then return the result the same way Naive does:
        //
        //     return Naive.Describe(items);
        //
        // When it works, the runner should show:
        //   - 20,000 rows and "OK - same result as Naive"
        //   - 1 query -> 1 query, but a lower time
        //   - run it with --sql and compare the two SELECT lists!
        // ----------------------------------------------------------------------------------

        var orders = await context.Orders
            .AsNoTracking()
            //.Include(o => o.Customer)          // every Customer column, just to read the name
            .OrderBy(o => o.OrderNumber)
            .Take(20000)
            .Select(o => new OrderListItem
            {
                OrderNumber = o.OrderNumber,
                CustomerName = o.Customer.Name,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                ItemCount = o.OrderProducts.Count
            })
            .ToListAsync();

        return Naive.Describe(orders);
    }
}
