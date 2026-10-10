using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace Pooling;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        var options = ctx.NewOptions();

        // ----------------------------------------------------------------------------------
        // YOUR TURN
        //
        // Create ONE pooled factory and ask it for contexts instead of calling "new AppDbContext(options)":
        //
        //     var factory = new PooledDbContextFactory<AppDbContext>(options);
        //     await using var db = await factory.CreateDbContextAsync();
        //
        //   - Same 20 workers x 250 requests, same Naive.LookupAsync(db, index) call, same sum.
        //   - Disposing a pooled context does not destroy it: it is reset and handed back to the pool.
        //   - (In a real ASP.NET app you just write services.AddDbContextPool<AppDbContext>(...)).
        //
        //     return Naive.Describe(totalOfAllWorkerSums);
        //
        // Expect "OK". The gain is small per request (microseconds): the point is less allocation and GC pressure under load.
        // ----------------------------------------------------------------------------------

        throw new NotImplementedException("TODO - open lessons/14-dbcontext-pooling/Fixed.cs (see README, 'Your turn').");
    }
}
