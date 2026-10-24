# Phased Fix Implementation Roadmap

- **Execution Mode:** Documentation-Ready for OpenAI Codex Independent Fix Phase
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`

---

## Phase I: Critical Accounting & Security Hardening (Days 1–2)
1. Patch `FIN-001` (Split payment journal key collision).
2. Patch `FIN-003` (Unpaid order refund prevention).
3. Patch `FIN-002` (Mixed payment cancellation cash accounting).
4. Patch `SEC-001` (Remove body actor binding).
5. Patch `FIN-013` (Dashboard stock subtraction).
*Gate:* Run vertical slice integration test suite; verify 100% pass rate.

## Phase II: Printing Reliability & Operational Flows (Days 3–4)
1. Patch `PRT-001` (Duplicate kitchen job prevention).
2. Patch `PRT-004` (PrintAgent re-authentication loop).
3. Implement `FLOW-001` (Two-stage pickup workflow).
4. Patch `FIN-016` (Delivery service fee server validation).
*Gate:* Execute mock printer spooler integration tests.

## Phase III: UI Ergonomics & State Management (Days 5–6)
1. Patch `UI-002` (Dark theme contrast fixes).
2. Patch `UI-007` (Call center async lookup race condition).
3. Patch `UI-018` (Recipe selection async race condition).
4. Patch `UI-013` (WPF piastre parsing bug).
*Gate:* Manual UI smoke testing on 1024x768 touch monitor.

## Phase IV: Final Regression & Acceptance (Day 7)
1. Run full end-to-end integration test suite.
2. Verify financial ledger zero-balance reconciliation.
3. Owner sign-off and production deployment.
