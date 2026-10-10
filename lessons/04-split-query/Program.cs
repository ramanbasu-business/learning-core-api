using EfCoreLab.Common.Running;
using SplitQueryLesson;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "04 - Cartesian explosion and AsSplitQuery",
    Summary = "5,000 orders with their lines AND their notes. Naive loads both collections in one big JOIN. " +
              "Fixed splits it into one query per collection.",
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
