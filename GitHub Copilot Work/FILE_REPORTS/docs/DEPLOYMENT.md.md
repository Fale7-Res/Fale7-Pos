# docs/DEPLOYMENT.md

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Worktree SHA256 `fa64025f84e004290ce67397417500d8e93c5f2e4744a72858ccc69562f0e0b5`; Git blob SHA256 `fa64025f84e004290ce67397417500d8e93c5f2e4744a72858ccc69562f0e0b5`. Read lines1–90.

Status: full static document/configuration review; functions NOT_APPLICABLE.

## role

Arabic restaurant installation, service/backup operation, owner provisioning and opening-stock instructions.

## reasoning

All90lines read, with seed credential literals masked in display if present. Instructions require elevated installer/service/SQL effects and are not audit probes. Hardcoded E-drive paths and SQL service dependency differ from the approved isolated LocalDB runtime, so copying the command here would not safely reproduce this session. ScheduledBackupService source settings confirm20h/30min/60/90s defaults; their presence is legitimate backup scheduling, not evidence of a malicious timer. SQL service writes backup files, so app/SQL account filesystem permissions differ. Off-device backup is explicitly outside current app. First-owner UI choice and server allow-first-owner branch actually exist; stale Application DTO commentary saying no owner API must not override current handler. Concurrent uniqueness and SEC001 identity checks still require security review; documentation alone does not prove guarantees.

## relationships

- tools/install-falah-service.ps1
- ScheduledBackupService configuration and backup handlers
- CreateUserHandler.HandleAsync lines37-55
- UsersViewModel.Actions RoleOption owner choice
- OPENING_STOCK_CHECKLIST.md

## tests

No installer, service change, backup, restore, owner creation, PIN login or inventory write executed. SQL permissions and last-admin guarantees not proven by instructions.

## recommendation

Create environment-specific reviewed deployment instructions, explicit dry-run/safe test targets and off-device restore-drill policy. Record service identity and real backup verification beyond VERIFYONLY. Do not execute these production instructions during audit.

## risks

Elevated service installation, automatic DB migration and scheduled backup side effects; first-owner workflow must not be confused with a safe default production setup.

## Scope and effects

Inputs are human/operator/build configuration; outputs are instructions/build membership/ignore behavior as described above. No application business logic, XAML, packages, databases, services or printers modified. Runtime/hardware claims remain explicitly unverified.
