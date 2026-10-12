using EfCoreLab.Common.Running;
using OptimisticConcurrency;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "11 - Optimistic concurrency",
    Summary = "Two users raise the same product's price by 10 at the same time. " +
              "Naive loses one update. Fixed detects the conflict (xmin row version) and retries.",
    MutatesData = true,
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
