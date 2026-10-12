using EfCoreLab.Common.Running;
using SetupCheck;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "00 - Setup check",
    Summary = "Proves Docker, the seeded database and the runner work. " +
              "Naive = 8 separate COUNT queries. Fixed = 1 raw-SQL query.",
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
