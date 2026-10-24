# Production source SQL boundary review

Read-only independent review at baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. **32 explicitly located SQL/write-construction operations** have manual per-call evidence across five saved batches. This is 31 explicit raw/ADO/migration SQL constructions plus one provider-generated ExecuteUpdate. All32 candidate locations reconcile to saved records; actual baseline blob hashes and normalized worktree equality checked for 23 affected source files.

No injectable SQL-text path was established in these inspected calls.21 FromSqlRaw predicates use fixed templates plus separate bound values. Backup identifiers are configured and guarded by BackupRules allowlist; disk/name values are bound parameters. Startup and sequence commands, and migration Up/Down, use constant SQL. This is not a complete security-phase clearance or runtime proof.

- [Common/customer/printing/sequence](batch-01-common-customer-print.json)
- [Employee and order lock calls](batch-02-employee-order.json)
- [Delivery and shift calls](batch-03-delivery-shift.json)
- [Backup/restore identifier and disk path boundaries](batch-04-backup.json)
- [Startup, migration and generated update](batch-05-startup-migration-generated.json)
- [Backup policy and identifier limitations](backup-boundary-notes.json)
- [Exact candidate/receipt reconciliation](candidate-reconciliation.json)
- [Identity/hash validation](validation.json)

Parameterization does not certify actor/session authorization, transaction scope, deadlock ordering, financial invariants, backup-path policy or safe restore concurrency. Existing FIN/SEC findings remain separate. Backup destination validation is not an approved-root/UNC restriction; this is a policy boundary to assess, not a demonstrated exfiltration exploit. Startup ROLLBACK IMMEDIATE is potentially disruptive to other DB clients even with constant SQL. No SQL, migration, server startup, login, physical printing or operational database access was executed.

Stable `sqlcall-` identities distinguish these manually verified operations; existing enclosing callable `sym-` identities retained wherever indexed. Startup top-level SQL has no indexed enclosing callable and is explicitly not counted as an invented function.
