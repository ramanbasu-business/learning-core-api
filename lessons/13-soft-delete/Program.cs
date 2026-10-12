using EfCoreLab.Common.Running;
using SoftDelete;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "13 - Global query filters (soft delete)",
    Summary = "List notes with their author names. Soft-deleted users are hidden by a global query filter, " +
              "so Naive silently drops their notes. Fixed brings them back with IgnoreQueryFilters.",
    NaiveIsWrong = true,
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
