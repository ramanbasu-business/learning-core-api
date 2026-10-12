using EfCoreLab.Common.Running;
using Indexes;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "06 - Indexes",
    Summary = "The 100 most recent Shipped orders since 1 September 2026. The query is identical in both variants; " +
              "Fixed first creates an index (not timed). The database is restored afterwards.",
    FixedSetup = Fixed.SetupAsync,      // runs before the Fixed variant, not timed
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
