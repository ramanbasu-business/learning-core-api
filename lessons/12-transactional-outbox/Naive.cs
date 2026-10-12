using EfCoreLab.Common.Data;
using EfCoreLab.Common.Domain;
using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace TransactionalOutbox;

public static class Naive
{
    public const int OrderCount = 100;

    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        var broker = new FakeBroker();
        var customerId = await FirstCustomerIdAsync(ctx);
        var saved = 0;

        for (var i = 0; i < OrderCount; i++)
        {
            await using var db = ctx.NewDbContext();
            var order = NewOrder(i, customerId);
            db.Orders.Add(order);

            // Step 1: the database commit succeeds...
            await db.SaveChangesAsync();
            saved++;

            // Step 2: ...but this call can fail AFTER the commit. The order exists, the event is gone forever.
            try
            {
                await broker.PublishAsync(OrderPlacedJson(order));
            }
            catch (InvalidOperationException)
            {
                // swallowed: nobody will ever retry this
            }
        }

        return Describe(saved, broker.Delivered.Count);
    }

    // ---- helpers shared with Fixed.cs -------------------------------------------------

    public static async Task<Guid> FirstCustomerIdAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDbContext();
        return await db.Customers.AsNoTracking().OrderBy(c => c.Email).Select(c => c.Id).FirstAsync();
    }

    public static Order NewOrder(int i, Guid customerId) => new()
    {
        Id = Guid.CreateVersion7(),
        CustomerId = customerId,
        OrderNumber = $"WEB-{i:D5}",
        Status = "Pending",
        OrderDate = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        TotalAmount = 10 + i
    };

    public static string OrderPlacedJson(Order o) =>
        $"{{\"orderId\":\"{o.Id}\",\"orderNumber\":\"{o.OrderNumber}\",\"total\":{o.TotalAmount}}}";

    /// <summary>The goal: every saved order has exactly one delivered event.</summary>
    public static LessonOutcome Describe(int ordersSaved, int published) =>
        new(Rows: ordersSaved,
            Fingerprint: $"{ordersSaved} orders, {ordersSaved} events delivered",
            Note: $"orders saved: {ordersSaved}, events delivered: {published}" +
                  (published == ordersSaved ? "" : $"  <-- {ordersSaved - published} events LOST"));
}
