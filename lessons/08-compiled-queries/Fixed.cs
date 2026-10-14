using EfCoreLab.Common.Data;
using EfCoreLab.Common.Domain;
using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace CompiledQueries;

public static class Fixed
{
    // ----------------------------------------------------------------------------------
    // YOUR TURN
    //
    // 1) Replace the null below with a COMPILED query, created once and stored in this static field:
    //
    //        EF.CompileAsyncQuery((AppDbContext db, string number) => ...a normal LINQ query...)
    //
    //    The query is the same as in Naive.cs: AsNoTracking, find the order whose OrderNumber == number.
    //    Inside a compiled query you use the NON-async operator (FirstOrDefault, not FirstOrDefaultAsync);
    //    the compiled delegate itself returns a Task.
    //
    // 2) Nothing else to change: the loop below already calls it.
    //
    // When it works, the runner should show:
    //   - the same rows and "OK - same result as Naive"
    //   - Queries: 5,000 -> 5,000 (compiling does not change the SQL)
    //   - a lower time - but probably a SMALL difference. Read the README for why.
    // ----------------------------------------------------------------------------------

    // Store it in a static readonly field so the preparation happens once per app, not once per call.

    // For interviews: IQueryable and EF take Expression<Func<>> so they can translate your code; IEnumerable takes Func<> so it can run it. That difference is also the core of lesson 07, client-side vs server-side evaluation.
    private static readonly Func<AppDbContext, string, Task<Order?>>? GetOrderByNumber =
        EF.CompileAsyncQuery(
            (AppDbContext appDbContext, string number) =>
            appDbContext.Orders.AsNoTracking().FirstOrDefault(o => o.OrderNumber == number)
        );


    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        if (GetOrderByNumber is null)
            throw new NotImplementedException("TODO - open lessons/08-compiled-queries/Fixed.cs and compile the query (see README, 'Your turn').");

        await using var context = ctx.NewDbContext();

        var found = 0;
        var total = 0m;

        for (var i = 0; i < Naive.Lookups; i++)
        {
            var order = await GetOrderByNumber(context, Naive.NumberFor(i));
            if (order is not null)
            {
                found++;
                total += order.TotalAmount;
            }
        }

        return Naive.Describe(found, total);
    }
}
