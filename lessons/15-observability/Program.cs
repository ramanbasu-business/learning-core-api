using EfCoreLab.Common.Running;
using Observability;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "15 - Observability: find slow queries",
    Summary = "Run a 3-query workload. Naive has no visibility into which query is slow. " +
              "Fixed adds a SlowQueryInterceptor that reports every command slower than 25 ms.",
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
