# 13 - Global query filters (soft delete)

**Problem:** `User` has a global query filter `!IsDeleted`. It's great for "never show deleted users", but it also applies to **navigations**: reaching `note.Author` joins `users` with the filter, so notes by deleted authors disappear. No error, just missing data.

This lesson is different: **Naive returns the wrong answer**, and the runner knows that.

## Run it

```bash
cd lessons/13-soft-delete
dotnet run
```

## Your turn

Edit `Fixed.cs` and use `IgnoreQueryFilters()`.

## Takeaways

- Global filters apply to the entity set *and* to navigations/joins to it.
- `IgnoreQueryFilters()` disables **all** filters on that query (soft delete and, e.g., multi-tenant ones). Re-add tenant conditions by hand.
- Filters are also applied to `Include`. A required relationship to a filtered entity gives the "missing parent" warning.
- Filter columns need an index (or a partial index) or every query pays for it.

## Interview questions

- What are global query filters used for? (soft delete, multi-tenancy)
- What are their pitfalls? (hidden joins, forgotten `IgnoreQueryFilters`, `ExecuteDelete` bypassing soft delete logic)
- Soft delete vs hard delete trade-offs?
