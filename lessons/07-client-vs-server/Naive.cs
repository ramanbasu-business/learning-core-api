using EfCoreLab.Common.Domain;
using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace ClientVsServer;

public static class Naive
{
    /// <summary>
    /// A normal, readable C# method. Handy - but EF Core cannot turn a method body into SQL,
    /// so it can only be run in .NET, on rows that have already been downloaded.
    /// </summary>
    public static bool IsLargeDeliveredOrder(Order order) =>
        order.Status == "Delivered" && order.TotalAmount >= 3000m;

    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDbContext();

        // Step 1: download EVERY order (200,000 rows) into memory...
        var allOrders = await db.Orders.AsNoTracking().ToListAsync();

        // Step 2: ...and filter them here, in C#, with our method.
        var large = allOrders.Where(IsLargeDeliveredOrder).ToList();

        return Describe(large);
    }

    /// <summary>Used by both Naive and Fixed, so they report their results in exactly the same way.</summary>
    public static LessonOutcome Describe(List<Order> orders) =>
        new(
            Rows: orders.Count,
            Fingerprint: orders.Sum(o => o.TotalAmount).ToString("0.00"),
            Note: $"large delivered orders: {orders.Count:N0}, worth {orders.Sum(o => o.TotalAmount):N2}");
}
