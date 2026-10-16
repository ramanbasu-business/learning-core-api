using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace OptimisticConcurrency;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var contextA = ctx.NewDbContext();
        await using var contextB = ctx.NewDbContext();

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

        // Both users open the product page and see the same price.
        var a = await contextA.Products.FirstAsync(p => p.Sku == Naive.Sku);
        var b = await contextB.Products.FirstAsync(p => p.Sku == Naive.Sku);

        var start = a.UnitPrice;

        // A saves first through the change tracker, so its UPDATE is also guarded by "AND xmin = @version".
        a.UnitPrice += 10m;
        await contextA.SaveChangesAsync();

        int conflicts = 0;
        try
        {
            // B still holds the old xmin, so this UPDATE matches 0 rows and EF throws.
            b.UnitPrice += 10m;
            await contextB.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            conflicts++;

            // Reload the entity from the database to get the latest values.
            // The point of reloading is to base your change on the latest data.
            await ex.Entries.Single().ReloadAsync();

            // Re-apply the change and save again.
            b.UnitPrice += 10m;
            await contextB.SaveChangesAsync();
        }

        // Check the final price. Both increases survived: start + 20.
        await using var checkDbContext = ctx.NewDbContext();
        var final = await checkDbContext.Products
            .AsNoTracking()
            .Where(p => p.Sku == Naive.Sku)
            .Select(p => p.UnitPrice).FirstAsync();

        return Naive.Describe(start, final, conflicts);

    }
}
