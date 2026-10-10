using System.Diagnostics;
using EfCoreLab.Common.Infrastructure;

namespace EfCoreLab.Common.Running;

/// <summary>
/// Runs a lesson: makes sure the database is ready, runs Naive then Fixed (each after a discarded
/// warm-up), and prints queries / time / rows side by side, checking Fixed returns the same result.
/// </summary>
public static class LessonRunner
{
    public static async Task<int> RunAsync(string[] args, LessonDefinition lesson)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        LabOptions options;
        try
        {
            options = LabOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }

        if (options.Help)
        {
            Console.WriteLine(LabOptions.HelpText);
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine($"== {lesson.Title} ==");
        Console.WriteLine(lesson.Summary);
        Console.WriteLine();

        var database = new DatabaseManager(options);
        try
        {
            await database.PrepareAsync();

            if (options.Reset || options.Reseed)
            {
                Console.WriteLine(options.Reseed ? "Baseline rebuilt; working database reset." : "Working database reset to baseline.");
                return 0;
            }

            var counter = new QueryCounter { CaptureSql = options.ShowSql };
            var context = new LessonContext(options, counter);
            var results = new List<VariantResult>();
            try
            {
                results.Add(await MeasureAsync("Naive", lesson.Naive, null, lesson, context, database, options));
                results.Add(await MeasureAsync("Fixed", lesson.Fixed, lesson.FixedSetup, lesson, context, database, options));
            }
            finally
            {
                if (lesson.MutatesData || lesson.FixedSetup is not null) await database.ResetAsync();
            }

            var consistent = Print(results, options, lesson);
            return consistent ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(ex is InvalidOperationException ? ex.Message : ex.ToString());
            return 1;
        }
    }

    private static async Task<VariantResult> MeasureAsync(
        string name,
        Func<LessonContext, Task<LessonOutcome>> run,
        Func<LessonContext, Task>? setup,
        LessonDefinition lesson,
        LessonContext context,
        DatabaseManager database,
        LabOptions options)
    {
        try
        {
            if (lesson.MutatesData)
                await database.ResetAsync();          // identical starting data for every variant

            if (setup is not null)
                await setup(context);                 // not timed (for example: CREATE INDEX)

            if (!lesson.MutatesData && !options.NoWarmup)
                await run(context);                   // discarded warm-up: JIT, plan cache, connection pool

            context.Counter.Reset();
            var stopwatch = Stopwatch.StartNew();
            var outcome = await run(context);
            stopwatch.Stop();

            return new VariantResult(name, outcome, context.Counter.Count,
                stopwatch.Elapsed.TotalMilliseconds, context.Counter.Commands, null);
        }
        catch (NotImplementedException ex)
        {
            return VariantResult.NotImplemented(name, ex.Message);
        }
    }

    /// <returns>false if Fixed returned a different result from Naive.</returns>
    private static bool Print(List<VariantResult> results, LabOptions options, LessonDefinition lesson)
    {
        var naive = results[0];
        var consistent = true;

        Console.WriteLine($"{"Variant",-8} {"Queries",8} {"Time (ms)",12} {"Rows",12}  Result");
        foreach (var r in results)
        {
            if (r.Outcome is null)
            {
                Console.WriteLine($"{r.Name,-8} {"-",8} {"-",12} {"-",12}  {r.NotImplementedMessage}");
                continue;
            }

            string verdict;
            if (ReferenceEquals(r, naive)) verdict = "baseline";
            else if (naive.Outcome is null) verdict = "-";
            else if (r.Outcome.Rows == naive.Outcome.Rows && r.Outcome.Fingerprint == naive.Outcome.Fingerprint)
                verdict = "OK - same result as Naive";
            else if (lesson.NaiveIsWrong)
                verdict = "DIFFERENT - expected: Naive has the bug";
            else
            {
                verdict = "WRONG - different result from Naive!";
                consistent = false;
            }

            Console.WriteLine($"{r.Name,-8} {r.Queries,8:N0} {r.Millis,12:N1} {r.Outcome.Rows,12:N0}  {verdict}");
        }

        foreach (var r in results.Where(r => r.Outcome?.Note is not null))
            Console.WriteLine($"  [{r.Name}] {r.Outcome!.Note}");

        var fixedResult = results[1];
        if (naive.Outcome is not null && fixedResult.Outcome is not null && consistent)
        {
            var ratio = fixedResult.Millis > 0 ? naive.Millis / fixedResult.Millis : double.PositiveInfinity;
            Console.WriteLine();
            Console.WriteLine($"Queries: {naive.Queries:N0} -> {fixedResult.Queries:N0}    " +
                              $"Time: {naive.Millis:N1} ms -> {fixedResult.Millis:N1} ms " +
                              $"({(ratio >= 1 ? $"{ratio:0.0}x faster" : $"{1 / ratio:0.0}x slower")})");
        }

        if (options.ShowSql)
        {
            foreach (var r in results.Where(r => r.Outcome is not null))
            {
                Console.WriteLine();
                Console.WriteLine($"--- SQL sent by {r.Name} ({r.Sql.Count:N0} commands; first {Math.Min(3, r.Sql.Count)} shown) ---");
                foreach (var sql in r.Sql.Take(3))
                {
                    Console.WriteLine(sql.Trim());
                    Console.WriteLine();
                }
            }
        }

        return consistent;
    }

    private sealed record VariantResult(
        string Name, LessonOutcome? Outcome, int Queries, double Millis,
        IReadOnlyList<string> Sql, string? NotImplementedMessage)
    {
        public static VariantResult NotImplemented(string name, string message) =>
            new(name, null, 0, 0, Array.Empty<string>(), message);
    }
}
