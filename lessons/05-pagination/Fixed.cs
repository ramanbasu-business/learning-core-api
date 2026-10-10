using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace Pagination;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDb();

        // ----------------------------------------------------------------------------------
        // YOUR TURN
        //
        // Return the same 50 order numbers, but WITHOUT Skip(). Use keyset pagination:
        // "give me the next 50 orders AFTER the last one I have already seen".
        //
        //   - The last order number of the previous page is Naive.PreviousPageLastOrderNumber.
        //   - Filter with Where(...) so only order numbers GREATER than that are considered.
        //   - Keep OrderBy(o => o.OrderNumber) and Take(Naive.PageSize), and Select(o => o.OrderNumber).
        //   - For strings, the comparison is written as   o.OrderNumber.CompareTo(cursor) > 0
        //
        // Then return the result the same way Naive does:
        //
        //     return Naive.Describe(numbers);
        //
        // When it works, the runner should show:
        //   - 50 rows (ORD-0190001 .. ORD-0190050) and "OK - same result as Naive"
        //   - Queries: 1 -> 1, but a much lower time
        // ----------------------------------------------------------------------------------

        throw new NotImplementedException("TODO - open lessons/05-pagination/Fixed.cs and write the keyset query (see README, 'Your turn').");
    }
}
