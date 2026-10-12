using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace OptimisticConcurrency;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var dbA = ctx.NewDbContext();
        await using var dbB = ctx.NewDbContext();

        // ----------------------------------------------------------------------------------
        // YOUR TURN
        //
        // Product.Version is mapped to Postgres' xmin and marked as a concurrency token, so EF adds
        // "WHERE xmin = @version" to every tracked UPDATE. If someone else changed the row, 0 rows match and
        // EF throws DbUpdateConcurrencyException.
        //
        //   1. Load the product in both contexts as TRACKED entities (FirstAsync, no AsNoTracking).
        //   2. A: a.UnitPrice += 10; SaveChangesAsync()               -> succeeds.
        //   3. B: b.UnitPrice += 10; SaveChangesAsync()               -> throws DbUpdateConcurrencyException.
        //      Catch it, count it (conflicts++), then:
        //         await b.ReloadAsync(); (db.Entry(b).ReloadAsync())   // refresh from the database
        //         b.UnitPrice += 10; await dbB.SaveChangesAsync();     // re-apply your change and save again
        //   4. Read the final price with a third context and return:
        //
        //         return Naive.Describe(start, final, conflicts);
        //
        // When it works: final = start + 20, conflicts caught: 1, and the runner shows "OK".
        // ----------------------------------------------------------------------------------

        throw new NotImplementedException("TODO - open lessons/11-optimistic-concurrency/Fixed.cs (see README, 'Your turn').");
    }
}
