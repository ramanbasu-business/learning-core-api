using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace OptimisticConcurrency;

public static class Naive
{
    public const string Sku = "SKU-00000";

    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var dbA = ctx.NewDb();
        await using var dbB = ctx.NewDb();

        // Both users open the product page and see the same price.
        var a = await dbA.Products.FirstAsync(p => p.Sku == Sku);
        var b = await dbB.Products.FirstAsync(p => p.Sku == Sku);
        var start = a.UnitPrice;

        // Both click "raise price by 10". Each writes "start + 10" blindly.
        var newPriceA = a.UnitPrice + 10;
        await dbA.Products.Where(p => p.Id == a.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.UnitPrice, newPriceA));

        var newPriceB = b.UnitPrice + 10;
        await dbB.Products.Where(p => p.Id == b.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.UnitPrice, newPriceB));

        await using var check = ctx.NewDb();
        var final = await check.Products.AsNoTracking().Where(p => p.Sku == Sku).Select(p => p.UnitPrice).FirstAsync();
        return Describe(start, final, conflicts: 0);
    }

    /// <summary>Two +10 increases should end at start + 20. Anything else means an update was lost.</summary>
    public static LessonOutcome Describe(decimal start, decimal final, int conflicts) =>
        new(Rows: 2,
            Fingerprint: "2 price increases of +10",
            Note: $"price {start:0.00} -> {final:0.00} (expected {start + 20:0.00}); conflicts caught: {conflicts}" +
                  (final == start + 20 ? "" : "  <-- LOST UPDATE"));
}
