using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace CompiledQueries;

public static class Naive
{
    public const int Lookups = 5_000;

    /// <summary>Some valid order number for lookup number i (works for both dataset sizes).</summary>
    public static string NumberFor(int i) => $"ORD-{(i * 37 % 20_000) + 1:D7}";

    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDbContext();

        var found = 0;
        var total = 0m;

        for (var i = 0; i < Lookups; i++)
        {
            var number = NumberFor(i);

            // The LINQ expression is processed on EVERY call: EF looks it up in its query cache
            // (it still has to walk the expression tree to build the cache key) before it runs the SQL.
            var order = await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderNumber == number);

            if (order is not null)
            {
                found++;
                total += order.TotalAmount;
            }
        }

        return Describe(found, total);
    }

    /// <summary>Used by both Naive and Fixed, so they report their results in exactly the same way.</summary>
    public static LessonOutcome Describe(int found, decimal total) =>
        new(
            Rows: found,
            Fingerprint: $"{found}|{total:0.00}",
            Note: $"orders found: {found:N0}, total {total:N2}");
}
