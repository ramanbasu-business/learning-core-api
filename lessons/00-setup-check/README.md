# 00 - Setup check

Not a performance lesson in itself. It proves that Docker, the seeded database and the lesson runner work,
and it shows how every later lesson reports its results.

## Run it

```bash
docker compose up -d            # from the repository root, once
cd lessons/00-setup-check
dotnet run
```

The first run builds the baseline database (schema + about 1.2 million rows) and takes a minute or two.
Later runs start immediately.

## What it does

| Variant | What it does | Round trips |
|---|---|---|
| `Naive.cs` | Eight separate `CountAsync()` calls, one per table | 8 |
| `Fixed.cs` | One raw-SQL query that counts all eight tables | 1 |

Both must report the same counts. The runner compares them and prints `OK` or `WRONG`.

## What to look at

- **Queries** - the number of commands EF sent to Postgres. Every command is a network round trip, which is why
  "fewer queries" is the first thing to check in almost every EF Core performance problem.
- **Time** - measured after a discarded warm-up run, so JIT compilation and cold caches don't distort it.
- **Users count** - only 48 of the 50 seeded users are counted. `User` has a global soft-delete query filter,
  so `db.Users.CountAsync()` skips deleted users. The raw SQL in `Fixed.cs` bypasses EF, so it has to repeat
  the condition (`WHERE NOT is_deleted`) by hand. Forgetting that is a classic bug (lesson 13 goes deeper).

## Try these

```bash
dotnet run -- --sql             # see the SQL behind both variants
dotnet run -- --reseed --small  # rebuild the baseline with the small dataset (20k orders)
dotnet run -- --reset           # restore the working database from the baseline
dotnet run -- --help
```

## Expected counts (large dataset)

customers 10,000 - products 1,000 - orders 200,000 - users 48 - roles 4 - user_roles 50 -
order_products and order_notes roughly 600,000 and 400,000.
