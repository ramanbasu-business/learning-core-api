# 06 - Indexes

**Scenario:** a dashboard widget: "the 100 most recent shipped orders since 1 September". The table has 200,000 orders.

```bash
cd lessons/06-indexes
dotnet run
```

## The problem

There is no index on `orders.status` or `orders.order_date`. To answer the query Postgres has to read **every row** of the table
(a *sequential scan*), keep the Shipped ones from September, sort them, and return 100.

An **index** is a separate, sorted structure (a B-tree) that lets Postgres jump to the matching rows instead of reading everything -
like the index at the back of a book.

## The idea: a composite index that matches the query

For `WHERE status = 'Shipped' AND order_date >= ... ORDER BY order_date DESC`, a B-tree on **(status, order_date)** works well:

```
index sorted by status first, then order_date inside each status:

  Cancelled  | 2023-10-01 ... 2026-09-30
  Delivered  | ...
  Shipped    | 2023-10-02 ... 2026-09-30   <- jump to 'Shipped', then read from the end backwards
  ...
```

Rules of thumb for column order: **equality columns first, then the range/sort column.** The index delivers rows already sorted,
so Postgres can stop after 100 rows (no sort of thousands of rows needed).

## What the files do

| File | |
|---|---|
| `Naive.cs` | Holds the query (`QueryAsync`). Runs it against the table with no helpful index. |
| `Fixed.cs` | **Your turn:** `SetupAsync` creates the index. `RunAsync` already runs the same query. |
| `Program.cs` | Registers `SetupAsync` as `FixedSetup`: it runs before the timed Fixed run, and the baseline database is restored afterwards. |

Note that the C# query is **identical** in both variants. EF Core code did not change; the *database* did.

## Your turn

1. Read the query in `Naive.cs`.
2. In `Fixed.cs`, fill in `SetupAsync` with a `CREATE INDEX` statement, then `ANALYZE orders`.
   <details>
   <summary>Hint 1</summary>
   <code>CREATE INDEX idx_orders_status_order_date ON orders (...two columns...)</code>
   </details>
   <details>
   <summary>Hint 2</summary>
   The first column is <code>status</code> (compared with =). The second is <code>order_date</code>; the query sorts it <code>DESC</code>, so you can declare it <code>order_date DESC</code>
   (Postgres can also scan an ascending index backwards, so both work).
   </details>
3. Run `dotnet run`.

## What to look for

- **Queries: 1 -> 1** and the same 100 rows (`OK - same result as Naive`).
- **Time** drops a lot: from scanning the whole table to reading ~100 index entries.
- Afterwards the database is reset, so Naive on the next run is slow again.

## See it with `EXPLAIN ANALYZE` (do this once)

Open a SQL prompt in the container:

```bash
docker exec -it efcorelab-postgres psql -U lab -d efcorelab
```

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT order_number FROM orders
WHERE status = 'Shipped' AND order_date >= '2026-09-01'
ORDER BY order_date DESC, order_number
LIMIT 100;
```

Without the index you will see `Seq Scan on orders` (and a sort). Now create your index in this prompt, run `ANALYZE orders;`, and run the `EXPLAIN` again:
you should see an `Index Scan` (or `Bitmap Index Scan`) and a far smaller "Execution Time". Drop it again with `DROP INDEX idx_orders_status_order_date;`
or just run `dotnet run -- --reset`.

## Things to watch for

- **Indexes are not free.** Every INSERT/UPDATE/DELETE must also update every index on the table, and indexes take disk space. Add the ones your real queries need, not "one per column".
- **Column order matters.** An index on `(order_date, status)` would not serve this query as well as `(status, order_date)`.
- **Functions on the column defeat the index:** `WHERE lower(email) = ...` or `WHERE order_date::date = ...` cannot use a plain index on that column (use an expression index).
- **Low-selectivity columns** (a boolean, a status with 5 values) are poor indexes by themselves - the combination with the date is what makes this one good.
- **Partial indexes** (`CREATE INDEX ... WHERE status = 'Shipped'`) are smaller when you only ever query one value.
- **Covering indexes** (`INCLUDE (order_number)`) can answer the query from the index alone (an *index-only scan*).

## Rule of thumb

> **Index the columns you filter and sort by, equality columns first.** Prove it with `EXPLAIN ANALYZE` - never guess.

## Interview questions

1. **What is a composite index and why does column order matter?** An index on several columns, sorted by the first, then the second, ... It helps queries that filter on a leftmost prefix; equality columns go before range/sort columns.
2. **What are the costs of an index?** Slower writes, more storage and maintenance, and more choices for the planner to get wrong.
3. **How do you check whether a query uses an index?** `EXPLAIN (ANALYZE, BUFFERS)`; look for Seq Scan vs Index Scan and the actual time.
4. **Why might Postgres ignore an index?** The table is small, the predicate matches a large part of the table (a seq scan is cheaper), the statistics are stale, or a function/cast wraps the column.
5. **Partial vs covering index?** Partial: indexes only rows matching a condition (smaller). Covering (`INCLUDE`): stores extra columns so the query never needs to visit the table.
6. **Can EF Core create indexes?** Yes: `HasIndex(...)` in the model (and migrations). In this lab the index is created by raw SQL only because the lesson needs it temporarily.

## Bonus exercise (optional)

Try a **partial index** (`... ON orders (order_date DESC) WHERE status = 'Shipped'`) and compare its size (`\di+` in psql) with the composite one.
