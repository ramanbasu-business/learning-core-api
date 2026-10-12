using EfCoreLab.Common.Data;
using EfCoreLab.Common.Domain;
using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace Tracking;

public static class Naive
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDbContext();

        // A perfectly normal query. By default EF Core TRACKS every Order it returns:
        // it remembers each one in the change tracker and keeps a copy of its original values,
        // in case you later change the object and call SaveChanges().
        var orders = await db.Orders
            .Where(o => o.Status == "Delivered")
            .OrderBy(o => o.OrderNumber)
            .Take(50_000)
            .ToListAsync();

        // This is a read-only export: we never change these orders, so all that tracking was wasted work.
        return Describe(orders, db);
    }

    /// <summary>Used by both Naive and Fixed, so they report their results in exactly the same way.</summary>
    public static LessonOutcome Describe(List<Order> orders, AppDbContext db)
    {
        // Counting the tracked entities would normally make EF scan them for changes first - switch that off,
        // we only want to count.
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        var tracked = db.ChangeTracker.Entries().Count();

        return new LessonOutcome(
            Rows: orders.Count,
            Fingerprint: orders.Sum(o => o.TotalAmount).ToString("0.00"),   // both variants must add up to the same total
            Note: $"entities tracked by EF: {tracked:N0}");
    }
}
