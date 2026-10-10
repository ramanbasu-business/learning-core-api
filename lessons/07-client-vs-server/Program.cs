using ClientVsServer;
using EfCoreLab.Common.Running;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "07 - Client vs server evaluation",
    Summary = "Find large delivered orders. Naive pulls ALL 200,000 orders into memory and filters them with a C# method. " +
              "Fixed makes the database do the filtering.",
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
