using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace SoftDelete;

public static class Naive
{
    public const string LastOrderNumber = "ORD-0002000";

    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDbContext();

        // Navigating to n.Author applies User's global filter (IsDeleted = false) as an INNER JOIN.
        // Notes whose author was soft-deleted vanish from the result without any error.
        var rows = await db.OrderNotes
            .AsNoTracking()
            .Where(n => n.Order.OrderNumber.CompareTo(LastOrderNumber) <= 0)
            .Select(n => new NoteRow
            {
                OrderNumber = n.Order.OrderNumber,
                Text = n.Text,
                Author = n.Author.Name
            })
            .ToListAsync();

        return Describe(rows);
    }

    public static LessonOutcome Describe(List<NoteRow> rows) =>
        new(Rows: rows.Count,
            Fingerprint: rows.Count.ToString(),
            Note: $"notes returned: {rows.Count:N0}, by deleted authors: {rows.Count(r => r.Author is null):N0}");
}
