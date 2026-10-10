using Npgsql;

namespace EfCoreLab.Common.Infrastructure;

/// <summary>
/// Settings for a lesson run: which database to use, and the flags typed after `--`
/// (for example `dotnet run -- --sql`).
/// </summary>
public class LabOptions
{
    // Matches docker-compose.yaml. Override with the EFLAB_CONNECTION environment variable.
    private const string DefaultConnection =
        "Host=127.0.0.1;Port=5433;Username=lab;Password=lab;Database=efcorelab";

    // --- The three databases we talk to ---------------------------------------------
    public string WorkingName { get; private set; } = "";        // lessons run against this one
    public string BaselineName { get; private set; } = "";       // golden copy; resets copy from it

    public string WorkingConnection { get; private set; } = "";
    public string BaselineConnection { get; private set; } = "";
    public string PostgresConnection { get; private set; } = ""; // the built-in 'postgres' database, used to create/drop the others

    // --- Flags ------------------------------------------------------------------------
    public bool ShowSql { get; private set; }       // --sql
    public bool Reset { get; private set; }         // --reset
    public bool Reseed { get; private set; }        // --reseed
    public bool SmallDataset { get; private set; }  // --small  (used when the baseline is built)
    public bool NoWarmup { get; private set; }      // --no-warmup
    public bool Help { get; private set; }          // --help

    public static LabOptions Parse(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("EFLAB_CONNECTION") ?? DefaultConnection;
        var databaseName = new NpgsqlConnectionStringBuilder(connection).Database ?? "efcorelab";

        var options = new LabOptions
        {
            WorkingName = databaseName,
            BaselineName = databaseName + "_baseline",
            WorkingConnection = connection,
            BaselineConnection = WithDatabase(connection, databaseName + "_baseline"),
            PostgresConnection = WithDatabase(connection, "postgres")
        };

        foreach (var arg in args)
        {
            switch (arg)
            {
                case "--sql": options.ShowSql = true; break;
                case "--reset": options.Reset = true; break;
                case "--reseed": options.Reseed = true; break;
                case "--small": options.SmallDataset = true; break;
                case "--no-warmup": options.NoWarmup = true; break;
                case "-h":
                case "--help": options.Help = true; break;
                default: throw new ArgumentException($"Unknown option '{arg}'. Try --help.");
            }
        }

        return options;
    }

    // Same connection string, different database. Pooling is off so no connection stays open
    // (Postgres refuses to copy or drop a database while anyone is connected to it).
    private static string WithDatabase(string connection, string database)
    {
        var builder = new NpgsqlConnectionStringBuilder(connection);
        builder.Database = database;
        builder.Pooling = false;
        return builder.ConnectionString;
    }

    public const string HelpText = """
        Options (put them after `--`, for example: dotnet run -- --sql)
          --sql          also print the SQL each variant sent to the database
          --reset        put the working database back to the baseline, then exit
          --reseed       delete and rebuild the baseline from scratch, then exit
          --small        with --reseed: build the small dataset (20k orders) instead of the large one (200k)
          --no-warmup    skip the discarded warm-up run (timings then include JIT and cold caches)
          -h, --help     show this help

        Environment variable:
          EFLAB_CONNECTION   Postgres connection string (default: the docker-compose database on port 5433)
        """;
}
