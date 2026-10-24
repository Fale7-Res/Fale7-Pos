# Fale7C# — persistent Codex workspace

Audit baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`, existing `main`. Audit is **IN PROGRESS**. No application fixes are authorized.

- [Resume handoff](Conversation-Recovery/RESUME-HANDOFF.md)
- [Requirements and decisions](Conversation-Recovery/Requirements-and-Decisions.md)
- [Recovery limits](Conversation-Recovery/Recovery-Limitations.md)
- [Audit progress](Deep-Audit/audit-progress.json)
- [Agent state](Sub-Agents/Agent-Registry/agent-state.json)
- [Relocation manifest](Workspace-Manifest.json)
- [Verified baseline hashes](Deep-Audit/Source-Inventory/verified-baseline-hashes.json)

Recovered reports retain their original bytes and historical paths for provenance. Original `artifacts/` paths still exist at the repository root; resolve them from there, not relative to a report. New navigation uses this workspace. Reports under Recovered-Checkpoint and Recovered-Evidence are historical, not validated current inventories. Runtime script copies ending `.source.txt` are reference material; live helpers remain under ignored `artifacts/local-runtime/`.

No database files, printer images, raw runtime logs, build binaries or credentials belong here. The latest user explicitly authorized a normal audit-documentation commit and push on existing main; see Conversation-Recovery/Publication-Authorization.md. Existing artifacts ignore rule remains unchanged.

Latest measured status: [15-phase progress assessment](Deep-Audit/PHASE_PROGRESS_REPORT.md). This remains an incomplete audit, with honest per-file/function and independent-verification limits.
