using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace SoftDelete;

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDb();

        // ----------------------------------------------------------------------------------
        // YOUR TURN
        //
        // The order history screen must show EVERY note, even those written by users who have since been
        // soft-deleted. Same query as Naive, but turn the global filter off for it:
        //
        //   - Add .IgnoreQueryFilters() to db.OrderNotes (anywhere before the Select).
        //   - Keep the same Where and the same NoteRow projection.
        //
        //     return Naive.Describe(rows);
        //
        // Expected: MORE rows than Naive. The runner shows it as "different (expected)" because Naive is the wrong one here.
        // ----------------------------------------------------------------------------------

        throw new NotImplementedException("TODO - open lessons/13-soft-delete/Fixed.cs (see README, 'Your turn').");
    }
}
