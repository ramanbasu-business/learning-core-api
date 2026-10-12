using CompiledQueries;
using EfCoreLab.Common.Running;

return await LessonRunner.RunAsync(args, new LessonDefinition
{
    Title = "08 - Compiled queries",
    Summary = "A hot path: look up an order by its number 5,000 times. " +
              "Naive writes the LINQ query each time. Fixed compiles it once with EF.CompileAsyncQuery.",
    Naive = Naive.RunAsync,
    Fixed = Fixed.RunAsync
});
