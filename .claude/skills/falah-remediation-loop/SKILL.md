---
name: falah-remediation-loop
description: Use when the owner instructs Claude to start, continue or resume the Falah multi-agent remediation loop (Analyst → Implementer → Independent Reviewer) driven by the `All Issues/` registry. Covers batch selection, pre-flight checks, agent hand-offs, recording evidence, and resuming from `All Issues/REMEDIATION/state.json`. Do not use to start the loop without an explicit owner instruction.
---

# Falah remediation loop (orchestrator procedure)

**Start condition:** the owner has explicitly said to start or continue the loop, and named either one batch or a range.
If not, stop and ask.

## 0. Load state
1. Read `CLAUDE.md` and `All Issues/REMEDIATION/00_WORKFLOW.md`.
2. Read `All Issues/REMEDIATION/state.json` and `All Issues/REMEDIATION/PROGRESS.md`.
3. Run `git status --porcelain`. Compare it with the latest `All Issues/REMEDIATION/BATCHES/*.status.txt`, or with `preflight-status.txt` if no batch has run. If files changed outside recorded batches, **stop and report**: that is owner work, and it must not be overwritten.
4. Check that free space on E: is at least 5 GB.
5. Check that no other `dotnet test` is running in this tree.

## 1. Pick the batch
- **Choice:** the owner-named batch, or else the first `PENDING` batch whose `depends_on` are all `DONE`.
- **Policy gate:** if `blocked_by_decision` is set and the decision is not recorded as answered in `docs/REMEDIATION_PLAN.md` § Decisions, mark the batch `BLOCKED` and ask the owner.
- **Mark it in progress:** set `status = IN_PROGRESS` in `state.json` and create `All Issues/REMEDIATION/BATCHES/<id>.md` with a header listing its issues.

## 2. Analyst
Spawn the agent `falah-analyst` with the batch id and issue IDs. Paste its final message under `## Analysis` in the batch file.
- BLOCKED → set `BLOCKED` plus the reason in `state.json`; report and stop.
- All issues already fixed → skip to step 4 (the Reviewer confirms).
- Migration or client-contract change → ask the owner before continuing.

## 3. Implementer
Spawn `falah-implementer` with the issue IDs and the full Analysis section. Paste its report under `## Implementation`.

## 4. Independent Reviewer
Spawn a **new** `falah-reviewer`. Give it:
- the batch id and issue IDs;
- the Analysis and Implementation sections;
- the changed-file list.

Paste its report under `## Review`.
- REJECT → give the required changes to `falah-implementer` (round 2). After 2 rejected rounds, set `NEEDS_OWNER` and stop.

## 5. Record (only on APPROVE)
For each issue in the batch:
1. Update `All Issues/ISSUES/<ID>.md`:
   - implementation status;
   - verification level: `CURRENT_TEST_VERIFIED` only when the Reviewer re-ran the tests green; otherwise `STATIC_CODE_REVIEW`;
   - an appended **"Remediation record"** section: date, batch, files, tests, RED and GREEN excerpts, Reviewer verdict.
2. Update the row and the totals in `All Issues/01_MASTER_ISSUE_REGISTRY.md`. Keep the counts reconciled.
3. In `state.json`: set the batch to `DONE`, with `completed` (date) and `changed_files`.
4. Add a row to `PROGRESS.md`.
5. Write `git status --porcelain` to `All Issues/REMEDIATION/BATCHES/<id>.status.txt`.

## 6. Report and stop
Tell the owner:
- the batch result and the issues fixed;
- the tests run and their results;
- anything blocked;
- the next batch.

Continue only if the owner's instruction covered more batches.

## Never
- Commit, push, stash, reset, clean, or change branches (unless the owner asks).
- Touch the real `Falah` database, or apply migrations.
- Edit existing migrations or the model snapshot.
- Decide a business policy.
- Run two integration test runs at once.
- Print PINs, tokens or secrets.
