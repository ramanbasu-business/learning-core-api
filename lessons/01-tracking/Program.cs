using EfCoreLab.Common.Running;
using Tracking;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "01 - Change tracking and AsNoTracking",
    Summary = "A read-only export of 50,000 orders. Naive lets EF track every row. Fixed turns tracking off.",
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
