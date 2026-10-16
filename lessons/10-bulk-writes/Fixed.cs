using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace BulkWrites;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var context = ctx.NewDbContext();

        // ----------------------------------------------------------------------------------
        // YOUR TURN
        //
        // Do the same cancellation WITHOUT loading any order into memory.
        //
        //   - Use the same filter as Naive: Status == "Pending" and OrderDate < Naive.Cutoff.
        //   - Finish with ExecuteUpdateAsync and SetProperty(o => o.Status, "Cancelled").
        //     It returns the number of affected rows.
        //   - No SaveChanges, no ToList.
        //
        //     return Naive.Describe(affected);
        //
        // When it works the runner should show "OK", and the query count drops
        // from (1 select + one UPDATE per row) to a single command.
        // ----------------------------------------------------------------------------------

        var affected = await context.Orders
            .Where(o => o.Status == "Pending" && o.OrderDate < Naive.Cutoff)
            .ExecuteUpdateAsync(o => o.SetProperty(p => p.Status, "Cancelled"));

        return Naive.Describe(affected);
    }
}
