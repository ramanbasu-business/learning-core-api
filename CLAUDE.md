# CLAUDE.md - learning-core-api

Read this first in every session. It holds the goal, the working agreement, the conventions and the progress,
so work can resume at any time. **Update the "Progress" section whenever a lesson is added or reviewed.**

## Goal

A GitHub-friendly, fully runnable **EF Core performance course** (.NET 10 + PostgreSQL in Docker) that works as:
a tutorial for the owner (Raman), a quick reference, and interview prep. Every lesson shows a **Naive** version next to a
**Fixed** version and proves the difference with query count, time and a result check. Must showcase
`AsNoTracking()` and `AsSplitQuery()`, and be clear to a stranger reading it on GitHub.

Inspiration for the repo style: https://github.com/AhsanAyaz/react-in-90ish

## Working agreement (IMPORTANT)

- **Guided mode.** Claude writes the lesson README, `Naive.cs`, `Program.cs`, csproj and a `Fixed.cs` **stub**
  (a `TODO` that throws `NotImplementedException`). **The owner writes the fix in `Fixed.cs`.** Do not write the
  solution unless asked. Give hints in the README (`<details>` blocks), not the answer.
- When the owner pastes their `Fixed.cs` and output: review it (correctness, same result as Naive, idiomatic, alternatives,
  pitfalls) and explain *why* it is faster.
- **Keep code simple and readable for a learner.** Plain classes, small methods, step-by-step comments that explain *why*.
  No clever abstractions, no primary-constructor/record tricks where a normal class is clearer. The owner has said the
  infrastructure code must stay easy to understand.
- **Exactly one DbContext: `AppDbContext`.** Lessons never subclass it; they use query options (`AsNoTracking`,
  `IgnoreQueryFilters`, ...) or the `configure` hook on `LessonContext.NewDb()`.
- Keep seeding simple (plain EF Core `AddRange` + `SaveChanges`, in batches).
- No secrets in the repo. The docker-compose Postgres uses throwaway lab credentials on 127.0.0.1 only.
- Do not commit unless asked.

## Environment notes for Claude

- Claude's cloud sandbox has **no .NET SDK and no NuGet access**: it cannot compile or run the C# code. Write carefully,
  and expect the owner to build/run on their Mac and paste any compile/runtime errors back.
- The owner's files are on their computer; edit them through the linked-computer tools (shell scripts that write files), not the cloud copy.
- The Postgres side can be sanity-checked with a local Postgres in the sandbox, but EF behaviour cannot.
- Gotcha already hit: `Database.SqlQueryRaw<T>` maps columns by property name **after** the snake_case naming
  convention, so the SQL aliases must be snake_case (`AS order_products`), not PascalCase.

## Run it

```bash
docker compose up -d                 # Postgres on 127.0.0.1:5433 (once per session)
cd lessons/<NN-name>
dotnet run                           # Naive, then Fixed; first run builds the baseline DB (1-2 minutes)
dotnet run -- --sql                  # also print generated SQL
dotnet run -- --reset | --reseed [--small] | --no-warmup | --help
```

## Repository layout

```
docker-compose.yaml            Postgres 17, port 5433, volume efcorelab-data
learning-core-api.slnx         solution (add every new lesson project here)
lessons/
  Directory.Build.props        net10.0, nullable, implicit usings; references EfCoreLab.Common for every lesson project
  EfCoreLab.Common/
    Domain/                    entities (Customer, Product, Order, OrderProduct, OrderNote, User, Role, UserRole, AuditLog, MessageQueue)
    Data/AppDbContext.cs       the one DbContext (snake_case via EFCore.NamingConventions; global soft-delete filter on User)
    Infrastructure/            LabOptions (flags/connection), DatabaseManager (baseline + working DB, reset), QueryCounter (interceptor)
    Seeding/Seeder.cs          Bogus + EF Core, deterministic row counts
    Running/                   LessonDefinition, LessonContext, LessonRunner (warm-up, timing, query count, result check)
  NN-name/                     one console project per lesson
```

Old API from before the course was removed; it is preserved in git tag `api-final`.

## How the database works

Two databases in the Docker Postgres: `efcorelab_baseline` (golden copy: tables from the EF model + seed data,
built once, never touched by lessons) and `efcorelab` (working copy lessons query). **Reset** = drop the working DB and
`CREATE DATABASE ... TEMPLATE efcorelab_baseline` (seconds). Lessons that change data/schema set
`MutatesData = true`; the runner then resets before each variant and at the end. Schema comes from `EnsureCreated`
(no migrations - deliberate). Dataset (large): 10k customers, 1k products, 200k orders, ~600k order lines, ~400k notes,
50 users (2 soft-deleted -> 48 visible), 4 roles. Ids are `Guid.CreateVersion7()`.
Intentionally **no index** on `orders(status, order_date)` - lesson 06 adds it.

## Lesson anatomy (follow for every new lesson)

```
lessons/NN-name/
  NN-name.csproj     <OutputType>Exe</OutputType> + <RootNamespace>; nothing else (Directory.Build.props does the rest)
  Program.cs         LessonRunner.RunAsync(args, new LessonDefinition { Title, Summary, MutatesData?, Naive, Fixed })
  Naive.cs           the version most people write first; has a shared `Describe(...)` returning LessonOutcome
  Fixed.cs           STUB with a clear "YOUR TURN" comment block and `throw new NotImplementedException("TODO ...")`
  (extra files)      e.g. a DTO used by both variants
  README.md          see template below
```

- `LessonOutcome(Rows, Fingerprint, Note)`: Naive and Fixed must produce identical `Rows` + `Fingerprint`; the runner
  flags `WRONG` otherwise. `Describe(...)` in Naive is reused by Fixed so both report identically.
- Use `await using var db = ctx.NewDb();` inside each variant. The runner does a discarded warm-up for read-only lessons.
- After creating a lesson: add the project to `learning-core-api.slnx`, update the root `README.md` roadmap row, update **Progress** below.

README template (headings): title + scenario + run commands -> **The problem** -> **The idea/concept** -> **What the files do** ->
**Your turn** (steps + 2-3 `<details>` hints) -> **What to look for** (expected output shape, what each number means, what does *not* change) ->
**Things to watch for / when not to use** -> **Rule of thumb** (one quote line) -> **Interview questions** (5-6 with short answers) ->
**Bonus exercise** (optional, often a teaser for a later lesson).
Never promise specific timings; tell the reader to read their own numbers.

## Roadmap

| # | Lesson | Key idea / plan |
|---|---|---|
| 00 | Setup check | 8 COUNT queries vs 1 raw-SQL query; shows the runner; global filter caveat on raw SQL |
| 01 | Tracking / `AsNoTracking()` | 50k orders read-only; tracked entities 50,000 -> 0; same SQL |
| 02 | Projection with `Select` | orders grid DTO; Include+map vs Select; SELECT list and row size shrink |
| 03 | N+1 | naive: loop over customers, query orders per customer (1+N queries); fixed: Include / single query / projection. Show query count 1+N -> 1. No lazy-loading proxies (single AppDbContext) |
| 04 | Cartesian explosion + `AsSplitQuery()` | Include two collections (OrderProducts + OrderNotes): rows = lines x notes; fix with `AsSplitQuery()`; show rows/time, discuss consistency trade-off, `AsSingleQuery` |
| 05 | Pagination | offset (`Skip` deep pages) vs keyset (seek) on indexed `OrderNumber`/Id; test page 1 and page ~5,000 |
| 06 | Indexes | `MutatesData = true`; setup creates an index on `orders(status, order_date)`; show `EXPLAIN ANALYZE`; teardown via reset; composite / covering / partial |
| 07 | Client vs server evaluation | non-translatable method in `Where`/`OrderBy` vs translatable expression; `AsEnumerable` pitfalls |
| 08 | Compiled queries | `EF.CompileAsyncQuery` on a hot path, loop many iterations |
| 09 | Filtered Include + raw SQL | `Include(x => x.Notes.Where(...))`, `FromSql`, `SqlQuery<T>` (remember snake_case aliases) |
| 10 | Bulk writes | `MutatesData`; `AddRange`+`SaveChanges` vs `ExecuteUpdate`/`ExecuteDelete`; batching |
| 11 | Optimistic concurrency | Postgres `xmin` row version on a Product (property on entity or shadow); provoke `DbUpdateConcurrencyException` with two contexts; `MutatesData` |
| 12 | Transactional outbox | write Order + `MessageQueue` row in one transaction (table already exists); `MutatesData` |
| 13 | Soft delete / global query filters | User filter already exists; `IgnoreQueryFilters`; filter vs raw SQL; required-navigation filter warning |
| 14 | DbContext pooling / `IDbContextFactory` | concurrent-load loop |
| 15 | Observability | slow-query logging, interceptor writing `AuditLog`, `EnableSensitiveDataLogging` (dev only) |

Also still to do after the lessons: `docs/cheatsheet.md` (one-page rules of thumb collected from every lesson),
`docs/interview-qa.md` (all Q&A in one place), a results table in the root README, a CI workflow that builds all projects,
and (optional) a `tests/` project.

## Progress

Update this table as work happens. "Owner verified" means the owner ran it and/or completed `Fixed.cs`.

| # | Lesson | Written by Claude | Owner's Fixed.cs | Notes |
|---|---|---|---|---|
| 00 | setup-check | done | n/a (complete lesson) | owner ran it successfully |
| 01 | tracking | done (README, Naive, Fixed stub) | pending | not yet reviewed |
| 02 | projection | done (README, Naive, DTO, Fixed stub) | pending | not yet reviewed |
| 03 | n-plus-one | done | done, reviewed | 501 -> 1 query (correlated COUNT/SUM subqueries); owner also tried a GroupBy-on-customers variant (correct but slower; grouping orders is the real alternative) |
| 04 | split-query | done | done, reviewed | AsSplitQuery: 1 -> 3 queries, ~2x faster; small fan-out (~3 lines x 2 notes per order) so gain is modest |
| 05 | pagination | written, Fixed.cs pending (owner) | uncompiled | owner builds, writes Fixed.cs, pastes for review |
| 06 | indexes | done | done, reviewed | index (status, order_date DESC, order_number) + ANALYZE; DESC matches the sort exactly (no Incremental Sort); covering -> index-only scan possible. Timings not yet recorded |
| 07 | client-vs-server | written, Fixed.cs pending (owner) | uncompiled | owner builds, writes Fixed.cs, pastes for review |
| 08 | compiled-queries | written, Fixed.cs pending (owner) | uncompiled | owner builds, writes Fixed.cs, pastes for review |
| 09 | filtered-include | written, Fixed.cs pending (owner) | uncompiled | owner builds, writes Fixed.cs, pastes for review |
| 10 | bulk-writes | written, Fixed.cs pending (owner) | uncompiled | owner builds, writes Fixed.cs, pastes for review |
| 11 | optimistic-concurrency | written, Fixed.cs pending (owner) | uncompiled | owner builds, writes Fixed.cs, pastes for review |
| 12 | transactional-outbox | written, Fixed.cs pending (owner) | uncompiled | owner builds, writes Fixed.cs, pastes for review |
| 13 | soft-delete | written, Fixed.cs pending (owner) | uncompiled | owner builds, writes Fixed.cs, pastes for review |
| 14 | dbcontext-pooling | written, Fixed.cs pending (owner) | uncompiled | owner builds, writes Fixed.cs, pastes for review |
| 15 | observability | written, Fixed.cs pending (owner) | uncompiled | owner builds, writes Fixed.cs, pastes for review |

## How to resume

Tell Claude: **"resume"** (or run the `/next-lesson` command). Claude should: read this file; check the Progress table;
if the owner pasted a `Fixed.cs`, review it first; otherwise create the next lesson following the anatomy above; then update
the solution file, root README roadmap row and the Progress table, and finish with a short summary of what the owner should do.

## Runner notes added with lessons 03-15
- `LessonDefinition.FixedSetup` (untimed, DB reset afterwards; lesson 06) and `NaiveIsWrong` (lesson 13).
- `LessonContext.NewOptions(configure?)` for pooling (14) and interceptors via `NewDb(b => ...)` (15).
- `Product.Version` is mapped to Postgres `xmin` (lesson 11). UNVERIFIED: if `--reseed` or lesson 11 fails mentioning xmin, fix the mapping in AppDbContext.
- Lessons 03-15 were written without a compiler: first action next session is `dotnet build` and fix errors the owner pastes.
- Next after the owner's Fixed.cs reviews: docs/cheatsheet.md, docs/interview-qa.md, results table, CI.
