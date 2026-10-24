# docs/DEMO_READINESS.md

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Worktree SHA256 `33080a5efc484a417e57783a879a0c07f7eec2f42eef72c99debb0e5b346baca`; Git blob SHA256 `33080a5efc484a417e57783a879a0c07f7eec2f42eef72c99debb0e5b346baca`. Read lines1–199.

Status: full static document/configuration review; functions NOT_APPLICABLE.

## role

Dated management-demo operating runbook, existing data classification, startup and performance/security caveats.

## reasoning

All199lines read in bounded/redacted chunks. Explicit localhost-only demo scope is not production approval. Its SQLExpress/Falah/E-drive/memory assumptions describe a different historical machine from current isolated LocalDB setup. Runbook read-only pre-demo check actually authenticates, creating sessions and potentially sliding LastSeen/audit writes; SQL restart, restore/cancel/settle/close/open actions are state changes requiring separate approval and were not run. Historical integrity-clean claims are dataset-specific and do not counter FIN001-013 concurrency/logic findings. Statement every request actor matches session is contradicted by SEC001. The tracked document itself includes demo credential and operational/person identifiers; this audit deliberately does not reproduce those values. Demo seed is documented, not proof of a hidden malicious backdoor, but automatic fresh-DB seeding and production account changes need explicit safety policy.

## relationships

- pre-demo-check.ps1
- SessionService and login endpoint
- DemoDataSeeder startup
- FIN001-013 and SEC001
- Production cleanup checklist and isolated runtime approval

## tests

No demo data, SQL query, session, server restart, restore, cancellation, custody settlement, backup or printer operation executed. Historical timings and account/session counts were not remeasured.

## recommendation

Separate a sanitized current runbook from private operational inventory; remove stale universal security assertions in a later approved documentation change, require approved disposable demo environment and never cosmetically alter real accounting for a demonstration.

## risks

Already tracked baseline contains documented demo credentials/operational data. Preserve source as required but never copy raw values into new audit artifacts. SQL memory tuning is an unvalidated recommendation for current hardware, not an automatic optimization.

## Scope and effects

Inputs are human/operator/build configuration; outputs are instructions/build membership/ignore behavior as described above. No application business logic, XAML, packages, databases, services or printers modified. Runtime/hardware claims remain explicitly unverified.
