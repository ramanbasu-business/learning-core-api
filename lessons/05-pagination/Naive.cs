using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace Pagination;

public static class Naive
{
    public const int PageSize = 50;

    /// <summary>
    /// In a real API this "cursor" comes from the client: it is the order number of the LAST row of the page
    /// the user was looking at before. Order numbers are ORD-0000001, ORD-0000002, ...
    /// </summary>
    public const string PreviousPageLastOrderNumber = "ORD-0190000";

    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDb();

        // Offset pagination: "skip the first 190,000 rows, then give me 50".
        // The database still has to walk past all 190,000 rows before it can return the first one we want.
        var numbers = await db.Orders
            .AsNoTracking()
            .OrderBy(o => o.OrderNumber)
            .Skip(190_000)
            .Take(PageSize)
            .Select(o => o.OrderNumber)
            .ToListAsync();

        return Describe(numbers);
    }

    /// <summary>Used by both Naive and Fixed, so they report their results in exactly the same way.</summary>
    public static LessonOutcome Describe(List<string> numbers) =>
        new(
            Rows: numbers.Count,
            Fingerprint: numbers.Count == 0 ? "" : $"{numbers[0]}..{numbers[^1]}",
            Note: numbers.Count == 0 ? "no rows (use the large dataset: --reseed)" : $"page runs from {numbers[0]} to {numbers[^1]}");
}
