using BulkWrites;
using EfCoreLab.Common.Running;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "10 - Bulk writes",
    Summary = "Cancel every Pending order placed before 2025-10-01. " +
              "Naive loads each order, changes it and saves. Fixed sends one UPDATE with ExecuteUpdateAsync.",
    MutatesData = true,
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
