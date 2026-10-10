using EfCoreLab.Common.Running;
using Projection;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "02 - Projection with Select",
    Summary = "An orders grid (20,000 rows, 5 columns). Naive loads whole entities and maps them in memory. " +
              "Fixed asks the database for just the 5 values.",
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
