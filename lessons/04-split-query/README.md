# 04 - Cartesian explosion and `AsSplitQuery()`

**Scenario:** an export of 5,000 orders, each with all its lines **and** all its notes.

```bash
cd lessons/04-split-query
dotnet run
dotnet run -- --sql
```

## The problem

`Include` of **two collections** in one query makes the database join both children to the parent:

```
order A has 3 lines (L1 L2 L3) and 2 notes (N1 N2)

what the single JOINed query returns:        3 x 2 = 6 rows
  A | L1 | N1        <- every row repeats ALL of A's columns
  A | L1 | N2
  A | L2 | N1
  A | L2 | N2
  A | L3 | N1
  A | L3 | N2
```

Lines and notes have nothing to do with each other, yet they are multiplied: **rows = lines x notes**. An order with 5 lines
and 4 notes returns 20 rows; the same data could be sent as 5 + 4 = 9 small rows. This is the **cartesian explosion**. The
result set grows multiplicatively, so more data crosses the network and EF has to de-duplicate it again.

(EF Core warns about this in its logs when you `Include` several collections without choosing a mode.)

## The idea: split the query

`AsSplitQuery()` tells EF to run **one SQL query per included collection**:

```
1. SELECT ... FROM orders ... ORDER BY order_number LIMIT 5000
2. SELECT ... FROM order_products ... (joined to those orders)
3. SELECT ... FROM order_notes    ... (joined to those orders)
```

Rows travelling = orders + lines + notes (additive) instead of orders x lines x notes (multiplicative).
The cost is **extra round trips** and a weaker consistency guarantee (the three queries are not one atomic snapshot - another
transaction could change data between them, unless you wrap them in a transaction).

## What the files do

| File | |
|---|---|
| `Naive.cs` | Two `Include`s in the default single-query mode. |
| `Fixed.cs` | **Your turn.** Same query, split. |

## Your turn

1. Read `Naive.cs`. The `Describe` helper estimates how many rows the JOIN would produce - compare it with `--sql` output.
2. In `Fixed.cs`, write the same query with `AsSplitQuery()`.
   <details>
   <summary>Hint 1</summary>
   It is one extra call in the chain, before <code>ToListAsync()</code>. Like <code>AsNoTracking()</code>, it is a query option, not a LINQ operator on the data.
   </details>
   <details>
   <summary>Hint 2</summary>
   You can also make it the default for a whole context (<code>UseNpgsql(..., o =&gt; o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))</code>)
   and opt out with <code>AsSingleQuery()</code> where one query is better.
   </details>
3. Finish with `return Naive.Describe(orders);`
4. Run it, then run with `--sql`.

## What to look for

- **Queries: 1 -> 3.** This time the query count goes **up** - and that is the correct trade. Fewer queries is not always better;
  *less data moved* is what matters here.
- **Rows** are 5,000 in both; the fingerprint (lines and notes loaded) matches.
- **Time**: measure it. Splitting wins when the explosion is big (many lines x many notes). With a single small collection, or with
  few children per parent, the single query can be faster because it avoids extra round trips. Always measure on realistic data.

## Things to watch for

- **One collection `Include` is fine** in single-query mode - there is no multiplication with only one child list.
  Reach for `AsSplitQuery()` when you include **two or more sibling collections** (or deep collection chains).
- **Always use `OrderBy` with `Take`/`Skip` and split queries**, otherwise the pieces may not line up deterministically.
- **Consistency**: three queries, three moments in time. If you need a consistent snapshot, run them in a transaction
  (repeatable read/serializable isolation).
- **`Select` projections avoid the problem** entirely when you do not need whole entities (lesson 02).

## Rule of thumb

> **Two or more collection `Include`s => use `AsSplitQuery()`.** Single collection or a projection => leave it alone.

## Interview questions

1. **What is the cartesian explosion?** Joining two independent child collections to a parent multiplies their row counts per parent (lines x notes), so the query returns far more rows than there is data.
2. **What does `AsSplitQuery()` do and what does it cost?** It sends one SQL query per included collection. It avoids the multiplication, but adds round trips and loses single-query atomicity.
3. **When is split query slower?** With small child collections or when network latency dominates: extra round trips outweigh the saved rows.
4. **How can you make split queries the default?** `UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)` in the provider options, then `AsSingleQuery()` to opt out per query.
5. **Why does ordering matter with `Take` and split queries?** Each split query re-applies the parent's filter/limit; without a deterministic `OrderBy` they can pick different parents.
6. **What is the alternative to including collections at all?** Project what you need with `Select`, or load the children in a separate query and group in memory.

## Bonus exercise (optional)

Change `Take(5_000)` to `Take(500)` in both files and re-run: does the winner change? Then remove the `Include(o => o.OrderNotes)`
from both and run again - with a single collection, which mode do you expect to win, and does it?
