using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace NPlusOne;

public static class Naive
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDbContext();

        // Query number 1: the first 500 customers.
        var customers = await db.Customers
            .AsNoTracking()
            .OrderBy(c => c.Email)
            .Take(500)
            .ToListAsync();

        var summaries = new List<CustomerSummary>();
        foreach (var customer in customers)
        {
            // Queries number 2 ... 501: one extra database round trip FOR EVERY CUSTOMER.
            // This "1 query + N more queries" shape is the N+1 problem.
            var orders = await db.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customer.Id)
                .ToListAsync();

            summaries.Add(new CustomerSummary
            {
                Name = customer.Name,
                OrderCount = orders.Count,
                TotalSpent = orders.Sum(o => o.TotalAmount)
            });
        }

        return Describe(summaries);
    }

    /// <summary>Used by both Naive and Fixed, so they report their results in exactly the same way.</summary>
    public static LessonOutcome Describe(List<CustomerSummary> summaries) =>
        new(
            Rows: summaries.Count,
            Fingerprint: $"{summaries.Sum(s => s.OrderCount)}|{summaries.Sum(s => s.TotalSpent):0.00}",
            Note: $"orders counted: {summaries.Sum(s => s.OrderCount):N0}, total spent: {summaries.Sum(s => s.TotalSpent):N2}");
}
