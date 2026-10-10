using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace ClientVsServer;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDb();

        // ----------------------------------------------------------------------------------
        // YOUR TURN
        //
        // Return the same orders, but let the DATABASE do the filtering, so that only matching rows
        // are downloaded.
        //
        //   - Write the condition inline in Where(...) using the properties directly (o.Status, o.TotalAmount).
        //     Do NOT call Naive.IsLargeDeliveredOrder inside the query - try it once to see the error EF gives!
        //   - Keep AsNoTracking() and finish with ToListAsync().
        //
        // Then return the result the same way Naive does:
        //
        //     return Naive.Describe(large);
        //
        // When it works, the runner should show:
        //   - the same rows and "OK - same result as Naive"
        //   - Queries: 1 -> 1, but far less data transferred and a lower time
        // ----------------------------------------------------------------------------------

        throw new NotImplementedException("TODO - open lessons/07-client-vs-server/Fixed.cs and write the filtered query (see README, 'Your turn').");
    }
}
