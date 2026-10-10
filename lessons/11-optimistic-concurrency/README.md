# 11 - Optimistic concurrency

**Problem:** two users edit the same row at the same time. The last writer silently overwrites the first: a **lost update**. No exception, no log, just wrong data.

## Run it

```bash
cd lessons/11-optimistic-concurrency
dotnet run
```

## Naive

Both contexts read the price, then each writes `price + 10` without checking whether the row changed. The note under the table shows the price ended at `start + 10` instead of `start + 20`.

## How the fix works

`Product.Version` maps to Postgres' hidden `xmin` system column (the transaction id of the last update) and is flagged as a concurrency token:

```csharp
e.Property(x => x.Version).HasColumnName("xmin").HasColumnType("xid")
 .ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
```

EF now generates `UPDATE ... WHERE id = @id AND xmin = @version`. If another transaction changed the row, nothing matches and EF throws `DbUpdateConcurrencyException`.

## Your turn

Edit `Fixed.cs` following the numbered steps in the comment: load tracked, save A, catch the conflict on B, reload, re-apply, save.

## Takeaways

- Optimistic = assume no conflict, detect it cheaply. Pessimistic = lock rows up front.
- Other providers use a `rowversion`/`[Timestamp]` column; Postgres uses `xmin`.
- On conflict you choose: retry with fresh data, overwrite, or tell the user.
- `ExecuteUpdate` bypasses the token, which is why Naive loses data.

## Interview questions

- Optimistic vs pessimistic concurrency?
- What does `DbUpdateConcurrencyException` mean and how do you handle it?
- How is a concurrency token different from a transaction?
