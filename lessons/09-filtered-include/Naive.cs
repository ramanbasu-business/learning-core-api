using EfCoreLab.Common.Domain;
using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace FilteredInclude;

public static class Naive
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDb();

        // Step 1: include ALL of each customer's orders (about 20 each)...
        var customers = await db.Customers
            .AsNoTracking()
            .Include(c => c.Orders)
            .OrderBy(c => c.Email)
            .Take(1_000)
            .ToListAsync();

        // Step 2: ...then keep only the 3 most recent per customer, in memory.
        foreach (var customer in customers)
        {
            customer.Orders = customer.Orders
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderNumber)
                .Take(3)
                .ToList();
        }

        return Describe(customers);
    }

    /// <summary>Used by both Naive and Fixed, so they report their results in exactly the same way.</summary>
    public static LessonOutcome Describe(List<Customer> customers)
    {
        var kept = customers.Sum(c => c.Orders.Count);
        var value = customers.Sum(c => c.Orders.Sum(o => o.TotalAmount));

        return new LessonOutcome(
            Rows: customers.Count,
            Fingerprint: $"{kept}|{value:0.00}",
            Note: $"orders shown on the cards: {kept:N0}, worth {value:N2}");
    }
}
