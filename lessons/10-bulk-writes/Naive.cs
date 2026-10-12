using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace BulkWrites;

public static class Naive
{
    public static readonly DateTime Cutoff = new(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDbContext();

        // Step 1: load every matching order into memory (tracked!)
        var orders = await db.Orders
            .Where(o => o.Status == "Pending" && o.OrderDate < Cutoff)
            .ToListAsync();

        // Step 2: change each one in memory
        foreach (var order in orders)
            order.Status = "Cancelled";

        // Step 3: SaveChanges sends one UPDATE statement per changed row
        await db.SaveChangesAsync();

        return Describe(orders.Count);
    }

    public static LessonOutcome Describe(int affected) =>
        new(Rows: affected, Fingerprint: $"cancelled {affected}", Note: $"orders cancelled: {affected:N0}");
}
