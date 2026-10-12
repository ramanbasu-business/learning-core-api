using EfCoreLab.Common.Running;
using Pooling;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "14 - DbContext pooling",
    Summary = "20 parallel workers each handle 250 tiny requests (5,000 total). " +
              "Naive builds a brand new DbContext for every request. Fixed reuses contexts from a pool.",
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
