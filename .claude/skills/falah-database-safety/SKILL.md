---
name: falah-database-safety
description: Use before running integration tests, touching EF Core entities/DbContext/migrations, generating a migration, or planning a Falah deployment, backup or restore. Keeps every command away from the real Falah database and every schema change reviewable.
---

# Falah database safety

The restaurant's data lives in the `Falah` database on `.\SQLEXPRESS`. Nothing in development or
testing connects to it.

## Tests

- Integration tests get their own database from `FalahApiFactory`: `FalahTest_<guid>`, files in
  `E:\SqlData\Tests`, created in the constructor, dropped on dispose (`SET SINGLE_USER ... DROP`),
  connection string overridden in configuration (the server resolves it lazily, so the override
  wins). Automatic backup is forced off in test hosts.
- Before adding a test that touches the database directly, assert the name:
  `Assert.StartsWith("FalahTest_", db.Database.GetDbConnection().Database)` (see `PrintJobClaimTests`).
- A "restart" factory reuses another's database (`reuseDatabaseName`) and never drops it.
- After an interrupted run, leftover `FalahTest_*` databases are safe to drop; nothing else is.

## Changing the schema

1. Change the entity / `FalahDbContext`.
2. Generate with the pinned tool, pointed at a database that does not exist:
   ```powershell
   $env:ConnectionStrings__Falah = 'Server=.\SQLEXPRESS;Database=FalahDesignTime_DoNotConnect;Trusted_Connection=True;TrustServerCertificate=True;'
   dotnet ef migrations add <Name> --project src/Falah.Infrastructure --startup-project src/Falah.Server --output-dir Persistence/Migrations
   Remove-Item Env:ConnectionStrings__Falah
   ```
   (`migrations add` builds the host and stops before `Migrate()`; it does not connect.)
3. Review: the migration does only what you meant; `Down` reverses it; the snapshot diff against the
   pre-change snapshot is exactly the new properties/indexes (diff against a saved copy — `git diff`
   also shows the owner's earlier uncommitted migrations).
4. Additive and nullable unless a backfill is designed, tested and documented. Never edit an existing
   migration, the history table, or the snapshot by hand; never generate a migration to silence an
   EF warning.
5. Record it in `docs/REMEDIATION_PROGRESS.md` and in the deployment plan (`docs/DEPLOYMENT.md`).

**The server runs `Database.Migrate()` at startup.** Shipping a build is applying its migrations to
the restaurant's database. Before that: a verified backup (Backup screen → verify), a restore drill
of that backup into a *copy* database, and the owner's go-ahead.

## Queries

- Stored enum-like values are lower-case (`"cancelled"`, `"pending"`); several old queries rely on the
  case-insensitive collation (D2). New queries compare the stored form.
- Money sums are `long`; date filters are UTC ranges from `BusinessClock`.
- Concurrency: `Orders.Version`, `Shifts.Version`, `ManagerApprovals.Consumed` are concurrency tokens;
  refund / close use `UPDLOCK, ROWLOCK, HOLDLOCK` reads inside a transaction.

## Backup and restore

Backups go to `Backup:DefaultDirectory` (`E:/falah-restaurant/Backups`); 60 kept; automatic when the
newest good backup is older than 20 h. Restore drills only into a differently named database, never
over `Falah`.

## Done means

No command touched `Falah`; migration reviewed with its snapshot diff recorded; deployment step and
backup requirement written down.
