namespace EfCoreLab.Common.Running;

/// <summary>What a lesson reports back so the runner can compare Naive and Fixed.</summary>
/// <param name="Rows">How many rows/items the variant produced.</param>
/// <param name="Fingerprint">A short string summarising the result; Naive and Fixed must produce the same one.</param>
/// <param name="Note">Optional extra line shown under the table (e.g. "tracked entities: 5000").</param>
public sealed record LessonOutcome(int Rows, string Fingerprint = "", string? Note = null);

/// <summary>Everything the runner needs to know about one lesson.</summary>
public sealed class LessonDefinition
{
    public required string Title { get; init; }
    public required string Summary { get; init; }

    /// <summary>
    /// True when the lesson writes data or changes the schema. The runner then restores the baseline
    /// before each variant (so Naive and Fixed start from identical data) and again at the end.
    /// </summary>
    public bool MutatesData { get; init; }

    /// <summary>
    /// Optional. Runs (not timed) just before the Fixed variant, for example to create an index.
    /// When it is set, the runner restores the baseline database afterwards.
    /// </summary>
    public Func<LessonContext, Task>? FixedSetup { get; init; }

    /// <summary>
    /// True for lessons where Naive has a bug (it returns wrong data), so Fixed is EXPECTED to return a different,
    /// correct result. The runner then does not report the difference as a failure.
    /// </summary>
    public bool NaiveIsWrong { get; init; }

    public required Func<LessonContext, Task<LessonOutcome>> Naive { get; init; }
    public required Func<LessonContext, Task<LessonOutcome>> Fixed { get; init; }
}
