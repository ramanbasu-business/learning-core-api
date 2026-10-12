using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Observability;

// ----------------------------------------------------------------------------------
// YOUR TURN
//
// 1) Write the interceptor below. Override:
//
//      public override ValueTask<DbDataReader> ReaderExecutedAsync(
//          DbCommand command, CommandExecutedEventData eventData,
//          DbDataReader result, CancellationToken cancellationToken = default)
//
//    If eventData.Duration.TotalMilliseconds >= Naive.SlowMilliseconds, add a line to Lines:
//        $"{eventData.Duration.TotalMilliseconds:0} ms  {command.CommandText.ReplaceLineEndings(" ")}"
//    then return base.ReaderExecutedAsync(command, eventData, result, cancellationToken).
//    (You will need: using System.Data.Common;)
//
//    Note: CountAsync runs as a scalar (ExecuteScalar), not a reader. If nothing is reported,
//    also override ScalarExecutedAsync (object? result) the same way.
//
// 2) In RunAsync build the context with it:
//        var interceptor = new SlowQueryInterceptor();
//        await using var db = ctx.NewDb(b => b.AddInterceptors(interceptor));
//    run Naive.RunWorkloadAsync(db), print/return interceptor.Lines in the Note.
//
// 3) If Lines is empty, throw NotImplementedException (the interceptor is not wired up yet).
//
// Expect "OK" and a Note listing the slow SQL. In production send this to your logger, not the console.
// ----------------------------------------------------------------------------------

public sealed class SlowQueryInterceptor : DbCommandInterceptor
{
    public List<string> Lines { get; } = new();

    // TODO: override ReaderExecutedAsync / ScalarExecutedAsync here.
}

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        throw new NotImplementedException("TODO - open lessons/15-observability/Fixed.cs (see README, 'Your turn').");
    }
}
