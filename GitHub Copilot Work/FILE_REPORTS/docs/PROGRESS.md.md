# docs/PROGRESS.md

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Worktree SHA256 `0a057d65cb5a6f8cc9b66dac7ecb595b77030bec6633174dce2d0248bd02b70a`; Git blob SHA256 `0a057d65cb5a6f8cc9b66dac7ecb595b77030bec6633174dce2d0248bd02b70a`. Read lines1–110.

Status: full static document/configuration review; functions NOT_APPLICABLE.

## role

Arabic early implementation/migration history with dated test counts and architecture decisions.

## reasoning

All110lines read in2bounded chunks. Header explicitly marks history, and SQL/WinUI architecture versus retainedWPF reference remains broadly correct. Single-migration statement is stale: current source has34migration pairs plus snapshot. OldSQLite suites,170/162passes and machineC-drive/SQLExpress claims are not current environment facts. Historical TODOs for payment cancellation and menu CRUD are now implemented, so they must not trigger duplicate feature-development recommendations. Checkout pricing authority and snapshot/stock transaction design agree with current handlers in principle; assertion every successful order necessarily has inventory effects must be qualified for missing/optional recipes and current validation gaps. No inference that serializable/UPDLOCK permanently eliminates all deadlocks or financial races is justified.

## relationships

- CheckoutOrderHandler/MenuPricingService/InventoryDeductionService
- OrderSequenceService and EF migration history
- Current cancellation/menu CRUD endpoints
- Current SQL LocalDB/runtime architecture versus oldSQLite

## tests

No historical test command executed. FalahApiFactory writes/drops disposable DBs; invocation is not safe audit-only verification against active environment. Disk-full observation is historical, not current host measurement.

## recommendation

Use original historical record as provenance, maintain a separate current source map and explicit implemented/obsolete requirements list, and cite source rather than treating TODO text as present defects.

## risks

Stale feature/security/concurrency assertions can lead to wrong work priorities; hardcoded paths and DB-reset test fixtures are not safe generic startup guidance.

## Scope and effects

Inputs are human/operator/build configuration; outputs are instructions/build membership/ignore behavior as described above. No application business logic, XAML, packages, databases, services or printers modified. Runtime/hardware claims remain explicitly unverified.
