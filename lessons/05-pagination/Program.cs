using EfCoreLab.Common.Running;
using Pagination;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "05 - Pagination: offset vs keyset",
    Summary = "Show the page of 50 orders that starts after order #190,000. " +
              "Naive skips 190,000 rows with Skip(). Fixed jumps straight there with a keyset (seek) condition.",
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
