# Database migration review — 2026-11-07

The server now defaults Database:MigrateOnStartup to false in Production. Development retains automatic provisioning; test factories explicitly opt in for their unique FalahTest databases. A production host with pending migrations stops with an actionable message. Routine startup no longer applies an unreviewed schema/data update.

## Reviewed changes

| Migration | Up | Down / rollback assessment |
|---|---|---|
| 20260911173753 RenameDineInOrdersToPickup | Normalizes only dine_in/dinein aliases to pickup; amounts/order count unchanged | Forward-only; now explicitly refuses the lossy downgrade that rewrote genuine pickups |
| 20260911224157 AddOrderRequestedPickupTime | Nullable UTC pickup timestamp; existing orders remain null | Drops recorded pickup timestamps; restore a verified backup for a full rollback |
| 20260919003557 AddSettingsMenuUrl | Nullable menu URL | Drops that setting |
| 20260919212356 AddPrintJobDocumentKind | Nullable slip/document discriminator; old order tickets stay null | Drops discriminator; old binary must not process new slip jobs |
| 20260919234725 AddSettingsDefaultOpeningCash | Adds default100000 piastres, backfills existing settings; explicit existing owner-approved default | Drops the setting; does not move money or rewrite open shifts |
| 20261024162329 AddShiftBusinessDayId | Nullable association/index; historical shifts not guessed/backfilled | Drops association/index |
| 20261024185051 AddPrintJobClaim | Nullable owner/expiry, owner length200 | Drops claim state; stop agents before changing versions |
| 20261026131805 AddManagerApprovalRequester | Nullable requesting user on approvals | Drops requester binding; do not roll authorization backwards on a live system |
| 20261107010000 AddVariantRetirement | Active defaults true for all existing sizes; no deletion of sold sizes | Drops retirement state; old software could expose retired sizes, so use verified-backup rollback |

## Reviewable script

Generated idempotent SQL: artifacts/codex-remediation-20261107/reviewed-migrations.sql

SHA256: 5848f83783ec84b4f73ff62592fde782085a3c5fd3f8c0c54abed445f2a8d4ca

The generation command was run with Database:MigrationScript and a FalahTest_ScriptGenerationOnly connection target. It exited before any database connection. A connection-rejecting interceptor test independently proves script generation performs no connection. No production database was read or changed.

The same command is available on the compiled server: pass --Database:MigrationScript=<absolute-output.sql>. Script generation takes precedence over startup migration/seeding and exits. It does not execute SQL.

## Deployment procedure

1. Stop the production server and print agents for the maintenance window.
2. Take and verify a current backup; preserve it outside the folder being pruned. Rehearse the reviewed script on a restored copy of that actual backup and compare business row counts and totals.
3. Review the generated SQL against the exact released source. Apply it explicitly using the database administration tool. Keep Database:MigrateOnStartup=false for normal production runs.
4. Start the server and matching agents; confirm health, login and first tickets. The new Sending/claim-token protocol requires server and agents from the same release.
5. Do not issue a migration downgrade through the normalization boundary. Recover using the verified pre-upgrade backup if rollback is needed.

## Evidence and limits

MigrationSafetyTests and DemoCredentialsTests:11/11. Explicit production flag=false leaves a fresh test database with zero application tables. Normalization rehearsal preserves three order rows, their financial values and the genuine pickup. The retirement model snapshot matches the current model. Test factories repeatedly apply the complete migration chain to unique test databases.

A rehearsal on the owner's actual restored production backup and the eventual commit/release remain owner operations. Source changes are deliberately uncommitted per the standing instruction; no claim is made about unknown production migration state.
