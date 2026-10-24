# Central Unfinished Callable Review Queue

- **Total Indexed Callables:** **11653**
- **Total Pending Substantive Review:** **6634**
- **Date & UTC:** `2026-10-24T19:50:00Z`

---

## 1. Pending Substantive Reviews by Specialist Worker

| Assigned Specialist Worker | Pending Callables | Priority Queue Domain |
| :--- | :--- | :--- |
| **`financial-auditor`** | **3508** | Orders, Checkout, Cash Drawer, Shifts, Treasury, Purchasing, Payroll |
| **`mirrored-code-auditor`** | **1386** | WPF / WinUI ViewModels, Services, Parity & Validation Divergence |
| **`architecture-coordinator`** | **880** | Domain Entities, Application Requests, Cross-Cutting Services |
| **`ui-maintainability-auditor`** | **378** | UI Views, Ergonomics, Virtualization, Async Void Handlers |
| **`security-auditor`** | **263** | Security, Middleware, Session Tokens, User Roles |
| **`printing-workflow-auditor`** | **219** | Print Agents, Win32 Spooler, Bitmap Rasterizer, ESC/POS |

---

## 2. Review Execution Standard

To promote a callable from `PRIOR_REASONING_UNVERIFIED` to `SUBSTANTIVE_SOURCE_REVIEWED`:
1. The callable must receive dedicated source-grounded analysis in its corresponding `File-Reviews/<path>.md`.
2. Behavior, trust boundaries, state mutations, error paths, and concurrency implications must be explicitly stated.
3. For any defect or vulnerability, verbatim original code excerpts with exact line numbers and SHA-256 must be provided.
4. No mass-generated generic templates are permitted.
