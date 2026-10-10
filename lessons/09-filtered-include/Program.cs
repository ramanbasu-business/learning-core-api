using EfCoreLab.Common.Running;
using FilteredInclude;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "09 - Filtered Include (and raw SQL)",
    Summary = "1,000 customer cards, each with their 3 most recent orders. " +
              "Naive includes ALL orders and trims them in memory. Fixed filters inside the Include.",
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
