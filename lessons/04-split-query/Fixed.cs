using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace SplitQueryLesson;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDb();

        // ----------------------------------------------------------------------------------
        // YOUR TURN
        //
        // Load the same 5,000 orders with their lines AND their notes, but tell EF Core to
        // send ONE QUERY PER COLLECTION instead of one giant JOIN.
        //
        //   - Copy the query from Naive.cs. Add one method call to it.
        //   - Keep the OrderBy before Take: split queries need a stable order to line the pieces up.
        //
        // Then return the result the same way Naive does:
        //
        //     return Naive.Describe(orders);
        //
        // When it works, the runner should show:
        //   - 5,000 rows and "OK - same result as Naive"
        //   - Queries: 1 -> 3   (MORE queries - but each one is small and narrow)
        //   - run with --sql to see the three statements
        // ----------------------------------------------------------------------------------

        throw new NotImplementedException("TODO - open lessons/04-split-query/Fixed.cs and write the query (see README, 'Your turn').");
    }
}
