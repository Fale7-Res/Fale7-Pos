# Financial, state and persistence review

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`. Audit only; no source/config/database modifications or tests executed.

Coverage: 269 complete files statically reviewed of 327 assigned; 58 explicitly NOT_ANALYZED. This is incomplete exhaustive coverage. Full remaining paths and per-file SHA256/line spans: [ledger](../../Sub-Agents/Results/financial-ledger.json). Worktree hashes and Git-blob hashes differ only CRLF/LF, verified normalized source equality for every reviewed file.

- **FIN-001 HIGH**: Mixed cash/electronic checkout cannot confirm its electronic leg — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-002 HIGH**: Cancelling pending mixed electronic payment hides collected cash without reversal — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-003 HIGH**: Refund can debit money for an order that was never collected — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-004 HIGH**: Historical cash refund omitted from current-shift expected cash — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-005 HIGH**: Concurrent distinct invoice settlements can overpay supplier and corrupt invoice cache — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-006 HIGH**: Generic payroll transition marks run Paid without paying treasury or employee ledger — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-007 MEDIUM**: Backup filenames collide within a second and WITH FORMAT can overwrite prior backup — CANDIDATE_PENDING_INDEPENDENT_CHALLENGE
- **FIN-008 HIGH**: Restore pending-journal check does not exclude active or newly accepted financial work — CANDIDATE_PENDING_INDEPENDENT_CHALLENGE
- **FIN-009 MEDIUM**: Concurrent different open-day requests create multiple active business days — CANDIDATE_PENDING_INDEPENDENT_CHALLENGE
- **FIN-010 MEDIUM**: Collected sales report drops immediate money from pending mixed payments — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-011 HIGH**: Shift opening and purchase receipt check protected account balance before their transactions — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-012 HIGH**: Attendance correction credits do not enter calculated payroll — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-013 MEDIUM**: Owner dashboard adds outgoing stock instead of subtracting it — CONFIRMED_STATIC_INDEPENDENT_REVIEW

[Detailed findings, exact excerpts, correction designs and proposed regressions](../../Sub-Agents/Results/financial-findings.json). High severity pending independent challenge is candidate, not runtime-proven. FIN-001 through FIN-006 independently traced by printing/workflows reviewer via parent.

Tests not run: FalahApiFactory constructor creates SQL databases at a fixed instance/E drive and Dispose force-disconnects and drops them. User-active Fale7CSharp_Test remains untouched.

Important unpromoted observations: multiple electronic split methods collapse to first leg; negative checkout fee/discount inputs; unchecked Money overflow; generic payroll cancellation/payment races; payroll period posting UTC vs work date and standalone payout double recognition; business-day close snapshot races; legacy PIN migration conversion absence requires complete chain search; restore success flag set after MULTI_USER can misreport replacement; historical dine-in migration is irreversible; SQL/app filesystem mismatch for backups. These are leads, not validated reportable findings.

Rejected/limited claims: account locks do protect aggregate treasury liquidity, but cannot replace invoice/operational state locks; no claim that stock nonreversal is inherently a bug because existing code expressly makes it legacy policy; tax midpoint rounding difference needs declared accounting policy; VERIFYONLY does not establish application restore readiness.

