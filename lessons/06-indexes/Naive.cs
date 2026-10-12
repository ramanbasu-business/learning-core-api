using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace Indexes;

public static class Naive
{
    // "Today" inside the seeded dataset is 1 October 2026.
    public static readonly DateTime Since = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    public static async Task<LessonOutcome> RunAsync(LessonContext ctx) => await QueryAsync(ctx);

    /// <summary>The query under test. Fixed.cs runs exactly the same query - only the database around it differs.</summary>
    public static async Task<LessonOutcome> QueryAsync(LessonContext ctx)
    {
        await using var context = ctx.NewDbContext();

        var numbers = await context.Orders
            .AsNoTracking()
            .Where(o => o.Status == "Shipped" && o.OrderDate >= Since)
            .OrderByDescending(o => o.OrderDate)
            .ThenBy(o => o.OrderNumber)
            .Take(100)
            .Select(o => o.OrderNumber)
            .ToListAsync();

        return new LessonOutcome(
            Rows: numbers.Count,
            Fingerprint: numbers.Count == 0 ? "" : $"{numbers[0]}|{numbers[^1]}",
            Note: numbers.Count == 0 ? "no rows" : $"most recent: {numbers[0]}, 100th: {numbers[^1]}");
    }
}
