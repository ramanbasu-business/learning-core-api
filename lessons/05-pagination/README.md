# 05 - Pagination: offset vs keyset

**Scenario:** an "orders" screen with next/previous page. The user (or a bot crawling the API) asks for a page deep into
the data: the 50 orders after order number 190,000.

```bash
cd lessons/05-pagination
dotnet run
dotnet run -- --sql
```

## The problem

Offset pagination (`Skip(n).Take(50)`) is the first thing everyone writes - and it gets slower the deeper you go:

```sql
... ORDER BY order_number OFFSET 190000 LIMIT 50
```

Postgres cannot jump to row 190,001. It reads 190,000 index entries, **throws them away**, then returns 50. Page 1 is fast,
page 4,000 is slow, and the cost grows linearly with the page number. There is a second problem: if rows are inserted or
deleted while the user pages, items shift and are **skipped or shown twice**.

## The idea: keyset (seek) pagination

Instead of "skip N rows", say "start **after the last row I saw**":

```sql
... WHERE order_number > 'ORD-0190000' ORDER BY order_number LIMIT 50
```

With an index on `order_number` (the unique index on the order number gives you one), Postgres **seeks directly** to the
cursor and reads 50 entries. The cost is the same for page 1 and page 4,000, and the result is stable when rows are added.

```
offset:  [ read 190,000 and discard ......................][ 50 ]
keyset:  [ seek ]                                          [ 50 ]
```

## What the files do

| File | |
|---|---|
| `Naive.cs` | `OrderBy` + `Skip(190_000)` + `Take(50)`. Also defines the cursor (`PreviousPageLastOrderNumber`). |
| `Fixed.cs` | **Your turn.** `Where` greater than the cursor, then `Take(50)`. |

## Your turn

1. Read `Naive.cs` and note where the cursor comes from.
2. In `Fixed.cs`, replace `Skip` with a `Where` condition on the cursor.
   <details>
   <summary>Hint 1</summary>
   <code>.Where(o =&gt; o.OrderNumber.CompareTo(Naive.PreviousPageLastOrderNumber) &gt; 0)</code> goes before <code>OrderBy</code>/<code>Take</code>.
   EF translates <code>CompareTo(x) &gt; 0</code> on a string into <code>order_number &gt; 'x'</code>.
   </details>
   <details>
   <summary>Hint 2</summary>
   The sort column and the filter column must be the same (<code>OrderNumber</code>), and it must be unique - otherwise rows with equal values
   can be skipped at page boundaries (add a tie-breaker such as the Id: <code>ThenBy</code> + a composite condition).
   </details>
3. Finish with `return Naive.Describe(numbers);`
4. Run it. Then try `Skip(0)` in Naive (page 1) to see that offset is only slow when deep.

## What to look for

- **Rows**: the same 50 order numbers, `ORD-0190001 .. ORD-0190050`.
- **Queries: 1 -> 1.** Same number, very different amount of work inside Postgres.
- **Time**: Naive's time is dominated by skipping; Fixed's is a single index seek. The deeper the page, the bigger the gap.

## Trade-offs of keyset pagination

- You can go **next/previous**, but not jump to "page 37" directly (there is no page number, only a cursor).
- The sort column must be **indexed** and **unique** (or made unique with a tie-breaker). Sorting on a non-indexed column makes both
  approaches slow (lesson 06).
- The cursor must be passed back by the client (usually an opaque token encoding the last key).
- Offset is still fine for small tables and admin screens where users never go deep.

## Rule of thumb

> **Small data, shallow pages: `Skip/Take`. Big data, infinite scroll, APIs: keyset.**

## Interview questions

1. **Why does `Skip(100000).Take(20)` get slower for later pages?** The database must read and discard all skipped rows every time, so cost grows with the offset.
2. **How does keyset pagination work?** Filter on "greater than the last seen key" with the same ordering and `Take(n)`; an index lets the database seek straight to the start.
3. **What are its limitations?** No random page jumps, needs a unique indexed sort key (tie-breaker for non-unique), needs the cursor passed around.
4. **What goes wrong with offset pagination when data changes between page requests?** Rows get skipped or duplicated because the offsets shift.
5. **How do you paginate by a non-unique column such as `OrderDate`?** Sort by `(OrderDate, Id)` and filter with `OrderDate > d OR (OrderDate = d AND Id > lastId)`.
6. **Does `Take` without `OrderBy` make sense?** No - without a total order, "the first 50 rows" is undefined and can change between runs.

## Bonus exercise (optional)

Change both files to sort by `OrderDate` then `OrderNumber` (a non-unique primary sort key). Notice that both versions get slower - there is no index
for that sort yet. Come back after lesson 06 and add one.
