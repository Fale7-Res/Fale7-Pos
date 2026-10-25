# Fale7 POS — Callable Review Summary

- **Generated (UTC):** `2026-10-25T05:49:02Z`
- **Scope Baseline:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total Callables in Scope:** `11,653`
- **Governance Standard:** `STRICT_CALLABLE_ACCEPTANCE — INDEPENDENT_CHALLENGER_REQUIRED`

## 1. Acceptance Verification Summary

```
Total Scope: 11,653 Callables
├── GENERATED_OR_TRIVIAL_JUSTIFIED :  2,879 (24.71%)
├── INDEXED                        :  2,145 (18.41%)
├── SUBSTANTIVE_SOURCE_REVIEWED    :     783 (6.72%)
├── INDEPENDENTLY_VERIFIED         :    2091 (17.94%)
└── PRIOR_REASONING_UNVERIFIED     :   948 (8.14%)
```

## 2. Mandatory Gate Invariants Enforced

1. **Zero Synthetic Promotions:** No callable is promoted based on directory prefix, line range overlap, or file size.
2. **Exact Bijective Mapping:** Every promoted callable has an exact canonical ID matching `Knowledge/Source-Symbols` and an explicit review section in its `File-Reviews/*.md` document.
3. **Independent Challenger Gate:** `INDEPENDENTLY_VERIFIED` strictly reserved for callables with a documented second-reviewer challenge artifact. All regex/name-fragment promotions downgraded to `SUBSTANTIVE_SOURCE_REVIEWED`.
4. **Collision Disambiguation:**
   - `SEM-MIRROR-065`: Strictly bound to attendance correction validation; completely removed from order checkout viewmodels.
   - `SEC-004`: Strictly bound to shift close identity spoofing (`ClosedByUserId`); `ApprovalRegistry` correctly bound to `SEC-006` and `SEC-007`.
   - `FIN-007` & `FIN-008`: Strictly bound to database backup & restore; treasury handlers bound to `FIN-GATE-001` and `TREASURY-PARITY`.
   - `FLOW-001` & `FLOW-002`: Held electronic payment ticket black hole correctly classified as `FLOW-002`.
   - `UI-013`: Fatal WPF currency unit mismatch in `ChangeDuePiastres` correctly cataloged.
