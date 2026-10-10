using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreLab.Common.Infrastructure;

/// <summary>
/// Counts how many commands EF Core sends to the database. Every command is one network round trip,
/// so this is how the lessons prove "50 queries became 1".
///
/// It works by hooking into EF Core: an "interceptor" is called just before each command runs.
/// EF has separate hooks for reading rows, for INSERT/UPDATE/DELETE, and for single values,
/// each with a sync and an async version. They all do the same thing: call Record().
/// </summary>
public class QueryCounter : DbCommandInterceptor
{
    private readonly object _lock = new();
    private readonly List<string> _sql = new();

    /// <summary>Number of commands since the last Reset().</summary>
    public int Count { get; private set; }

    /// <summary>When true, the SQL text of every command is also kept (shown by --sql).</summary>
    public bool CaptureSql { get; set; }

    /// <summary>The SQL of the commands since the last Reset() (only filled when CaptureSql is true).</summary>
    public List<string> Commands
    {
        get { lock (_lock) return _sql.ToList(); }
    }

    public void Reset()
    {
        lock (_lock)
        {
            Count = 0;
            _sql.Clear();
        }
    }

    private void Record(DbCommand command)
    {
        lock (_lock)
        {
            Count++;
            if (CaptureSql) _sql.Add(command.CommandText);
        }
    }

    // ---- Reading rows (SELECT) -------------------------------------------------------
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Record(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return ValueTask.FromResult(result);
    }

    // ---- INSERT / UPDATE / DELETE ----------------------------------------------------
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Record(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return ValueTask.FromResult(result);
    }

    // ---- Single value (for example SELECT COUNT(*)) ----------------------------------
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Record(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return ValueTask.FromResult(result);
    }
}
