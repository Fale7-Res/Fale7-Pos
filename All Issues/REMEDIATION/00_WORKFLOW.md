# Remediation Loop — Workflow

**Authoritative registry:** `All Issues/` (issue records in `All Issues/ISSUES/`, plan in
`All Issues/03_PRIORITIZED_FIX_ROADMAP.md`).
**Progress state:** `All Issues/REMEDIATION/state.json` (machine-readable) and `PROGRESS.md` (human-readable).
**Status:** prepared on 2026-11-05. **No batch has been started.** The loop starts only on the owner's explicit instruction.

## Roles (project agents in `.claude/agents/`)

| Agent | File | Can edit? | Runs tests? | Job |
|---|---|---|---|---|
| Analyst | `falah-analyst.md` | No | No | Re-verify each issue against current code; produce the RED-test and fix brief; flag blocked policy decisions |
| Implementer | `falah-implementer.md` | Yes (batch scope only) | Targeted, `FalahTest_*` only | RED test → minimal fix → GREEN → regression classes → evidence |
| Independent Reviewer | `falah-reviewer.md` | No | Re-runs the tests itself | Check the diff against acceptance criteria and CLAUDE.md; APPROVE or REJECT |

The **orchestrator** (the main session) does the following:
- picks the batch;
- passes the inputs to each agent;
- records the results in the batch log;
- updates the issue files and `state.json`;
- decides retry or stop.

The orchestrator does not write application code itself.

## One batch, step by step

1. **Select.** The next `PENDING` batch in `state.json` whose dependencies are `DONE` and whose decisions are recorded.
2. **Pre-flight**, all of which must be true:
   - `git status` matches the last recorded snapshot, except for changes from earlier approved batches;
   - free space on E: is at least 5 GB (integration databases go under `E:\SqlData\Tests`);
   - no other test run is active in this tree.
3. **Analyst.** Spawn it with the batch issue IDs and save its brief to `BATCHES/<batch>.md` (section "Analysis").
   - BLOCKED → mark the batch `BLOCKED` with the reason; move on or stop.
   - "Already fixed" → send it straight to the Reviewer to confirm, then update the registry.
4. **Owner gate.** Ask the owner only if the brief raises an unrecorded policy decision, a migration, or a client-contract change.
5. **Implementer.** Spawn it with the issue IDs, the brief and the batch rules. Save its report (section "Implementation").
6. **Reviewer.** Spawn a fresh one with the brief, the report and the changed-file list. Save its report (section "Review").
   - REJECT → back to step 5 with the required changes, at most **2** rework rounds, then mark `NEEDS_OWNER`.
7. **Record.** On APPROVE, update in each issue file:
   - implementation status;
   - verification level: `CURRENT_TEST_VERIFIED` only if the Reviewer re-ran the tests green;
   - evidence date, files and tests.

   Also update:
   - `01_MASTER_ISSUE_REGISTRY.md`: the row and the totals;
   - `state.json` and `PROGRESS.md`;
   - a git status snapshot to `BATCHES/<batch>.status.txt`.
8. **Stop point.** After each batch the orchestrator reports to the owner and continues only if the owner's
   instruction covers more than one batch.

## Rules that always apply (from CLAUDE.md)
- **Database.** Never the real `Falah` database. Tests use `FalahTest_<guid>`.
- **Migrations.** Additive only; never edit an existing migration or the snapshot. Migrations are never applied to a real database by this loop.
- **Git.** No commit, push, stash, reset, clean, destructive checkout or branch change unless the owner asks. Read a file's diff before editing it.
- **Money.** Integer piastres. **Time:** UTC storage, Cairo grouping. **Commands:** replay check first.
- **Identity and permissions.** Server-side; the actor comes from `HttpContext`.
- **Business policy.** VAT, payroll proration, refund policy, permission grants and hardware are the owner's or accountant's decisions. Record the question in `docs/REMEDIATION_PLAN.md` § Decisions (owner-approved edit) and leave the behaviour unchanged.
- **Full integration suite.** About 15 minutes; only at milestones, never concurrently. On a sleep or timeout, re-run the class before calling it a failure.
- **Definition of done.** Tests ran green and the evidence is written down. A screen is done when it has been run. Hardware is done only after an on-site test.

## Resuming in another session
1. Read `All Issues/REMEDIATION/PROGRESS.md` and `state.json`.
2. Compare `git status --porcelain` with the latest `BATCHES/*.status.txt`, or with `preflight-status.txt` if no batch has run.
3. If a batch is `IN_PROGRESS`, read its `BATCHES/<batch>.md`:
   - an Implementation section without a Review → run the Reviewer;
   - no Implementation section → restart the Implementer.
4. The skill `.claude/skills/falah-remediation-loop/SKILL.md` contains this procedure in a form the session can invoke.
