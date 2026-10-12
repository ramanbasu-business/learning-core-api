using EfCoreLab.Common.Domain;
using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace SplitQueryLesson;

public static class Naive
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDbContext();

        // Two collections in ONE query (EF's default, "single query" mode).
        // The database must join orders x order_products x order_notes, so for every order it returns
        // (number of lines) x (number of notes) rows - each row repeating all the order's columns.
        var orders = await db.Orders
            .AsNoTracking()
            .Include(o => o.OrderProducts)
            .Include(o => o.OrderNotes)
            .OrderBy(o => o.OrderNumber)
            .Take(5_000)
            .ToListAsync();

        return Describe(orders);
    }

    /// <summary>Used by both Naive and Fixed, so they report their results in exactly the same way.</summary>
    public static LessonOutcome Describe(List<Order> orders)
    {
        var lines = orders.Sum(o => o.OrderProducts.Count);
        var notes = orders.Sum(o => o.OrderNotes.Count);
        var joinedRows = orders.Sum(o => Math.Max(1, o.OrderProducts.Count) * Math.Max(1, o.OrderNotes.Count));

        return new LessonOutcome(
            Rows: orders.Count,
            Fingerprint: $"{lines}|{notes}",
            Note: $"lines: {lines:N0}, notes: {notes:N0}. A single JOINed query would return about {joinedRows:N0} rows for these.");
    }
}
