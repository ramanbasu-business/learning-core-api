using EfCoreLab.Common.Data;
using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace Observability;

public static class Naive
{
    public const int SlowMilliseconds = 25;

    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDb();
        var result = await RunWorkloadAsync(db);

        // Total time is all you get: you can't tell WHICH of the three queries is the slow one.
        return new LessonOutcome(Rows: 3, Fingerprint: result, Note: "no idea which query was slow (see Fixed)");
    }

    /// <summary>A small workload with one fast query and two heavier ones.</summary>
    public static async Task<string> RunWorkloadAsync(AppDbContext db)
    {
        // 1. fast: point lookup by unique index
        var order = await db.Orders.AsNoTracking().CountAsync(o => o.OrderNumber == "ORD-0000001");

        // 2. heavier: scan of the whole orders table
        var cancelled = await db.Orders.AsNoTracking().CountAsync(o => o.Status == "Cancelled");

        // 3. slowest: LIKE '%refund%' over ~400k note texts (no index can help)
        var refunds = await db.OrderNotes.AsNoTracking().CountAsync(n => n.Text.Contains("refund"));

        return $"{order}|{cancelled}|{refunds}";
    }
}
