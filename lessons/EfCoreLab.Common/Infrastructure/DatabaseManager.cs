using EfCoreLab.Common.Data;
using EfCoreLab.Common.Seeding;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EfCoreLab.Common.Infrastructure;

/// <summary>
/// Looks after the two databases inside the Docker Postgres:
///
///   efcorelab_baseline   the golden copy: tables + seed data. Built once. Lessons never touch it.
///   efcorelab            the working copy. Lessons run against this one.
///
/// "Reset" simply means: delete the working copy and make a fresh copy of the baseline.
/// Postgres copies a database by copying its files, so this takes seconds even with 200k orders.
/// </summary>
public class DatabaseManager
{
    private readonly LabOptions _options;

    public DatabaseManager(LabOptions options)
    {
        _options = options;
    }

    /// <summary>Called before every lesson run. Makes sure both databases exist.</summary>
    public async Task PrepareAsync()
    {
        await using var postgres = await ConnectAsync();

        var baselineExists = await DatabaseExistsAsync(postgres, _options.BaselineName);
        var workingExists = await DatabaseExistsAsync(postgres, _options.WorkingName);

        if (_options.Reseed || !baselineExists)
        {
            // First run ever (or --reseed): build the baseline, then copy it.
            await BuildBaselineAsync(postgres);
            await CopyBaselineToWorkingAsync(postgres);
        }
        else if (_options.Reset || !workingExists)
        {
            // Baseline is fine; just give the lesson a fresh working copy.
            await CopyBaselineToWorkingAsync(postgres);
        }
        // Otherwise both exist and nothing needs doing.
    }

    /// <summary>Put the working database back to the baseline (used by lessons that change data).</summary>
    public async Task ResetAsync()
    {
        await using var postgres = await ConnectAsync();
        await CopyBaselineToWorkingAsync(postgres);
    }

    // ---------------------------------------------------------------------------------

    private async Task BuildBaselineAsync(NpgsqlConnection postgres)
    {
        var size = _options.SmallDataset ? "small" : "large";
        Console.WriteLine($"Building the baseline database ({size} dataset). This happens once and takes a minute or two...");

        try
        {
            // Step 1: start from an empty database.
            await RunSqlAsync(postgres, $"DROP DATABASE IF EXISTS \"{_options.BaselineName}\" WITH (FORCE)");
            await RunSqlAsync(postgres, $"CREATE DATABASE \"{_options.BaselineName}\"");

            // Steps 2 and 3 use the one AppDbContext: create the tables from the model, then fill them.
            var efOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_options.BaselineConnection)
                .UseSnakeCaseNamingConvention()
                .Options;
            await using (var db = new AppDbContext(efOptions))
            {
                await db.Database.EnsureCreatedAsync();                    // CREATE TABLE ... from the entity classes
                await Seeder.SeedAsync(db, _options.SmallDataset);         // INSERT the fake data
            }

            // Step 4: let Postgres collect statistics so it picks good query plans.
            await using (var baseline = new NpgsqlConnection(_options.BaselineConnection))
            {
                await baseline.OpenAsync();
                await RunSqlAsync(baseline, "VACUUM ANALYZE");
            }
        }
        catch
        {
            // Don't leave a half-built baseline behind, or the next run would think it is ready.
            await RunSqlAsync(postgres, $"DROP DATABASE IF EXISTS \"{_options.BaselineName}\" WITH (FORCE)");
            throw;
        }

        Console.WriteLine("Baseline ready.");
    }

    private async Task CopyBaselineToWorkingAsync(NpgsqlConnection postgres)
    {
        // Close our own pooled connections to the working database, otherwise it cannot be dropped.
        NpgsqlConnection.ClearAllPools();

        // Step 1: delete the old working copy (WITH FORCE disconnects anyone still using it).
        await RunSqlAsync(postgres, $"DROP DATABASE IF EXISTS \"{_options.WorkingName}\" WITH (FORCE)");

        // Step 2: Postgres will not copy a database someone is connected to, so disconnect everyone.
        await RunSqlAsync(postgres,
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity " +
            $"WHERE datname = '{_options.BaselineName}' AND pid <> pg_backend_pid()");

        // Step 3: make the new working copy by copying the baseline ("template").
        await RunSqlAsync(postgres,
            $"CREATE DATABASE \"{_options.WorkingName}\" TEMPLATE \"{_options.BaselineName}\"");
    }

    // ---------------------------------------------------------------------------------
    // Small helpers

    /// <summary>Connects to Postgres. Retries for ~30 seconds because the container may still be starting.</summary>
    private async Task<NpgsqlConnection> ConnectAsync()
    {
        for (var attempt = 1; attempt <= 30; attempt++)
        {
            var connection = new NpgsqlConnection(_options.PostgresConnection);
            try
            {
                await connection.OpenAsync();
                return connection;
            }
            catch (NpgsqlException)
            {
                await connection.DisposeAsync();
                if (attempt == 1)
                    Console.WriteLine("Waiting for Postgres... (did you run `docker compose up -d`?)");
                await Task.Delay(1000);
            }
        }

        throw new InvalidOperationException(
            "Cannot reach Postgres. Start it from the repository root with:  docker compose up -d");
    }

    private static async Task<bool> DatabaseExistsAsync(NpgsqlConnection postgres, string name)
    {
        await using var command = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", postgres);
        command.Parameters.AddWithValue("name", name);
        return await command.ExecuteScalarAsync() is not null;
    }

    private static async Task RunSqlAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.CommandTimeout = 600;   // building indexes / VACUUM on big tables can be slow
        await command.ExecuteNonQueryAsync();
    }
}
