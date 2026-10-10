using EfCoreLab.Common.Running;
using NPlusOne;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "03 - The N+1 problem",
    Summary = "A report for the first 500 customers: name, number of orders, total spent. " +
              "Naive runs one query for the customers and then one more per customer. Fixed asks once.",
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
