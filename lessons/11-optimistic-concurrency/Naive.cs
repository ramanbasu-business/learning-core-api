using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace OptimisticConcurrency;

public static class Naive
{
    public const string Sku = "SKU-00000";

    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var contextA = ctx.NewDbContext();
        await using var contextB = ctx.NewDbContext();

        // Both users open the product page and see the same price.
        var a = await contextA.Products.FirstAsync(p => p.Sku == Sku);

        // Display product Sku, Price, and Version
        Console.WriteLine($"Product Sku: {a.Sku}, Price: {a.UnitPrice}, Version: {a.Version}");

        var b = await contextB.Products.FirstAsync(p => p.Sku == Sku);
        var start = a.UnitPrice;

        // Both click "raise price by 10". Each writes "start + 10" blindly.
        var newPriceA = a.UnitPrice + 10;
        await contextA.Products.Where(p => p.Id == a.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.UnitPrice, newPriceA));

        var newPriceB = b.UnitPrice + 10;
        await contextB.Products.Where(p => p.Id == b.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.UnitPrice, newPriceB));


        // Check the final price. It should be start + 20, but is only start + 10.
        await using var checkDbContext = ctx.NewDbContext();
        var final = await checkDbContext.Products
            .AsNoTracking()
            .Where(p => p.Sku == Sku)
            .Select(p => p.UnitPrice).FirstAsync();
        return Describe(start, final, conflicts: 0);
    }

    /// <summary>Two +10 increases should end at start + 20. Anything else means an update was lost.</summary>
    public static LessonOutcome Describe(decimal start, decimal final, int conflicts) =>
        new(Rows: 2,
            Fingerprint: "2 price increases of +10",
            Note: $"price {start:0.00} -> {final:0.00} (expected {start + 20:0.00}); conflicts caught: {conflicts}" +
                  (final == start + 20 ? "" : "  <-- LOST UPDATE"));
}
