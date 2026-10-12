using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace Projection;

public static class Naive
{
    public static async Task<LessonOutcome> RunAsync(LessonContext lessonContext)
    {
        await using var context = lessonContext.NewDbContext();

        // Step 1: load WHOLE entities, plus the related ones we need a value from.
        // (AsNoTracking is already here - that was lesson 01. The cost that is left is the amount of data.)
        var orders = await context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)          // every Customer column, just to read the name
            .Include(o => o.OrderProducts)     // every order line, just to count them
            .OrderBy(o => o.OrderNumber)
            .Take(20_000)
            .ToListAsync();

        // Step 2: throw most of it away - copy the 5 values we need into a small object.
        var items = orders
            .Select(o => new OrderListItem
            {
                OrderNumber = o.OrderNumber,
                CustomerName = o.Customer.Name,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                ItemCount = o.OrderProducts.Count
            })
            .ToList();

        return Describe(items);
    }

    /// <summary>Used by both Naive and Fixed, so they report their results in exactly the same way.</summary>
    public static LessonOutcome Describe(List<OrderListItem> items) =>
        new(
            Rows: items.Count,
            Fingerprint: $"{items.Sum(i => i.TotalAmount):0.00}|{items.Sum(i => i.ItemCount)}",
            Note: items.Count > 0 ? $"first row: {items[0].OrderNumber} / {items[0].CustomerName} / {items[0].ItemCount} items" : null);
}
