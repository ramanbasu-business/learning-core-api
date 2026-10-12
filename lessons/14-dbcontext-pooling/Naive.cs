using EfCoreLab.Common.Data;
using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace Pooling;

public static class Naive
{
    public const int Workers = 20;
    public const int RequestsPerWorker = 250;

    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        // Build the options once (as an app would), but create a NEW DbContext for every request.
        var options = ctx.NewOptions();

        var tasks = Enumerable.Range(0, Workers).Select(async worker =>
        {
            decimal sum = 0;
            for (var r = 0; r < RequestsPerWorker; r++)
            {
                await using var db = new AppDbContext(options);
                sum += await LookupAsync(db, worker * RequestsPerWorker + r);
            }
            return sum;
        });

        var sums = await Task.WhenAll(tasks);
        return Describe(sums.Sum());
    }

    /// <summary>One tiny request: find a product by SKU and return its price.</summary>
    public static async Task<decimal> LookupAsync(AppDbContext db, int i)
    {
        var sku = $"SKU-{i % 300:D5}";
        return await db.Products.AsNoTracking()
            .Where(p => p.Sku == sku)
            .Select(p => p.UnitPrice)
            .FirstAsync();
    }

    public static LessonOutcome Describe(decimal priceSum) =>
        new(Rows: Workers * RequestsPerWorker,
            Fingerprint: priceSum.ToString("0.00"),
            Note: $"requests: {Workers * RequestsPerWorker:N0}, sum of prices: {priceSum:N2}");
}
