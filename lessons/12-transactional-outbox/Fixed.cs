using EfCoreLab.Common.Domain;
using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace TransactionalOutbox;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        var broker = new FakeBroker();
        var customerId = await Naive.FirstCustomerIdAsync(ctx);

        // ----------------------------------------------------------------------------------
        // YOUR TURN  (transactional outbox pattern)
        //
        // Part 1 - save the order AND the event in ONE SaveChanges (one transaction):
        //
        //   for each i in 0..Naive.OrderCount-1:
        //       var order = Naive.NewOrder(i, customerId);
        //       db.Orders.Add(order);
        //       db.MessageQueues.Add(new MessageQueue
        //       {
        //           QueueName   = "orders",
        //           MessageType = "OrderPlaced",
        //           Payload     = Naive.OrderPlacedJson(order),
        //           CreatedAt   = DateTime.UtcNow
        //       });
        //       await db.SaveChangesAsync();          // both rows commit together, or neither does
        //
        // Part 2 - a relay loop publishes pending messages (this is what a background worker would do):
        //
        //   repeat up to 3 rounds while pending messages exist:
        //       load MessageQueues where Status == "pending" (tracked)
        //       foreach: try broker.PublishAsync(m.Payload)
        //                   success -> m.Status = "processed"; m.ProcessedAt = DateTime.UtcNow;
        //                   failure -> m.RetryCount++; m.ErrorMessage = ex.Message;  (stays "pending")
        //       await db.SaveChangesAsync();
        //
        // Then return:
        //
        //     return Naive.Describe(Naive.OrderCount, broker.Delivered.Count);
        //
        // When it works: 100 orders, 100 events delivered, runner shows "OK".
        // ----------------------------------------------------------------------------------

        throw new NotImplementedException("TODO - open lessons/12-transactional-outbox/Fixed.cs (see README, 'Your turn').");
    }
}
