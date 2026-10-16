# FALE7 POS — FORENSIC-GRADE COMPLETE SYSTEM DEEP AUDIT

## 0. Mission and non-negotiable rules

You are acting as the Lead Principal Software Engineer, Security Auditor, Software Architect, Database Integrity Auditor, Performance Engineer, Windows Desktop UI/UX Auditor, and Printing Systems Reliability Engineer for the Fale7 POS restaurant management system.

Repository: `https://github.com/Fale7-Res/Fale7-Pos`

Start by verifying the exact current Git commit, branch, working-tree state, and local runtime environment.

The intended clean source baseline is:

`c35816ecae49633674775dd32061c6ee92f94aaf` — `upload new project`

**THIS IS AN EXHAUSTIVE DEEP AUDIT AND RECOMMENDATION PHASE. IT IS NOT A CODE IMPLEMENTATION OR BUG-FIXING PHASE.**

Your mission is to understand every component of this system, find actual and plausible defects, prove findings wherever safely possible, and create exceptionally detailed, navigable reports showing precisely what needs to change, why, where, and how.

**DO NOT implement fixes, refactor code, update packages, alter application XAML, change configuration, apply schema migrations, create production data, or implement new features.**

Do not create commits or push changes without explicit authorization.

Audit documentation may be stored in a dedicated audit output directory, separate from the application source code. Preserve any existing user or runtime files.

You are free to choose the most reliable audit strategy, including specialized sub-agents, multiple independent review passes, Roslyn tools, static analyzers, test inspection, runtime observation, or manual code reasoning.

**Prioritize engineering quality and depth over speed.**

Do not claim the system is flawless, secure, production-ready, or 100% bug-free merely because automated checks pass.

Do not treat a successful build as proof of correct runtime behavior.

Do not trust historical documentation, previous agents, existing tests, or your own generated reports without verification.

---

# PHASE 1 — Establish the verified source of truth

Before auditing individual files:

1. Verify the repository's commit SHA and Git status.
2. Enumerate every Git-tracked file from the exact baseline.
3. Identify actual files, file types, code projects, namespaces, classes, interfaces, records, methods, constructors, properties, local functions, lambdas, and source line ranges.
4. Identify runtime-generated files, untracked files, external binaries, compiled resources, and test data separately.
5. Read existing technical documentation and reconcile its claims against actual source code.
6. Map projects, services, dependency injection, API endpoints, SQL models, migrations, background services, printing workers, UI views, view models, styles, and resource dictionaries.
7. Determine which functionality is implemented, partially implemented, obsolete, unreachable, or missing.

Use Roslyn syntax trees and semantic models for C# wherever appropriate. Use proper XAML/XML parsing for UI files. Do not represent regex extraction as semantic analysis.

Create a reliable, machine-readable source inventory with accurate source hashes and line references.

Every source declaration and every tracked file must be accounted for.

For all counts, distinguish `ZERO`, `NOT_APPLICABLE`, `NOT_ANALYZED`, `UNKNOWN`, and `PARSER_ERROR`.

Never silently turn an extraction failure into zero.

Verify aggregate statistics independently and make the consistency checker fail when data disagrees.

---

# PHASE 2 — Per-file exhaustive review

**Every relevant file in the repository must receive an individual audit record.**

This includes C#, XAML, configuration files, project files, scripts, migrations, documentation, and security-relevant metadata.

The default reviewing unit should be ONE file.

For closely coupled components, two or three files may be investigated together, but preserve a separate detailed report for each file.

Use specialized review passes on large or security-critical files rather than compressing their assessment into a shallow summary.

For every file, provide:

- Exact path, Git SHA, source hash, and file type.
- What the file actually does.
- Classes, methods, functions, properties, and significant symbols.
- How it interacts with other files and runtime services.
- Security responsibilities and attack surfaces.
- Business rules and potential logic defects.
- Data consistency and transaction risks.
- Performance and resource-management concerns.
- Thread safety, concurrency, and cancellation behavior.
- UI, accessibility, and localization responsibilities, if applicable.
- Printing behavior, if applicable.
- External dependencies and their associated risk.
- Duplicate logic or unnecessary abstractions.
- Error handling and recovery behavior.
- Tests that cover it and tests that are missing.
- Confirmed findings, plausible risks, and unresolved questions.
- Recommended immediate fixes.
- Recommended improvements.
- Optional future features.

Review every applicable function for input validation, authorization, output correctness, side effects, exceptional conditions, resource disposal, concurrency, complexity, data flow, error recovery, and test coverage.

Do not mark a function as audited merely because it was listed in an index.

A completed file report should have sufficient evidence for a different engineer to locate the issue and begin work without rediscovering the problem.

---

# PHASE 3 — Deep adversarial security investigation

**Security is a top priority.**

Assume an adversarial threat model in which a malicious user, employee, attacker on the restaurant LAN, compromised workstation, malicious dependency, or deliberately inserted code attempts to exploit the POS.

Search for:

- SQL injection and unsafe SQL composition.
- Authentication bypass.
- Authorization bypass and privilege escalation.
- Insecure direct object references.
- Forged user identities and manager approvals.
- Session hijacking, fixation, token replay, and weak revocation.
- Insecure PIN handling and credential storage.
- Hardcoded credentials, test accounts, and development backdoors.
- Unsafe administrative, backup, and restore endpoints.
- Remote command execution, command injection, and path traversal.
- Deserialization risks.
- Insecure file handling and exposed sensitive data.
- Secrets in logs, configuration, exceptions, and build artifacts.
- Missing server-side validation.
- Race conditions and time-of-check/time-of-use vulnerabilities.
- Replayable payment, expense, inventory, or refund requests.
- Insufficient rate limiting.
- Insecure LAN deployment assumptions.
- Open network services, permissive bindings, and weak transport protection.
- Dangerous debug functionality shipped into production.
- Logging and audit-trail manipulation.
- Unauthorized database or filesystem access.
- Hidden administrative access.
- Unexpected external connections or data exfiltration.
- Suspicious reflection, assembly loading, process execution, native calls, and interop.
- Malicious or unexplained startup hooks, scheduled tasks, background processes, and persistence mechanisms.

## Deliberately hidden or time-triggered functionality

Investigate code and configuration that can deliberately misbehave under unusual conditions.

Examples include:

- Date-based and time-based triggers.
- Timers and delayed execution.
- Scheduled destructive operations.
- Environment-dependent logic.
- Hardcoded activation or expiration dates.
- Hidden kill switches.
- Unexpected feature flags.
- Remote configuration that changes behavior.
- Hidden data deletion or data modification.
- Suspicious maintenance jobs or startup behavior.
- Secret developer-only bypasses.

Distinguish legitimate scheduled operations, such as automatic backups, from suspicious or unsafe behavior.

Do not accuse any component of being a backdoor without supporting evidence.

Perform cross-file security data-flow analysis, including request entry points, authorization, handlers, persistence, and side effects.

Check both anonymous and authenticated attack surfaces.

Never conduct intrusive testing against production systems, real accounts, or business data.

Use safe, isolated validation only.

For each potential vulnerability, specify whether exploitability is confirmed, probable, or unverified.

---

# PHASE 4 — Third-party libraries and supply-chain security

Audit every direct and transitive dependency.

Include NuGet, native binaries, Windows App SDK components, build tools, CI scripts, external packages, unmanaged DLLs, and any downloaded executables.

Produce a Software Bill of Materials when possible.

Check authoritative security advisories, including NuGet advisories, GitHub Security Advisories, OSV, Microsoft security updates, and applicable CVEs.

For every meaningful risk, identify:

- Exact package and version.
- Direct or transitive relationship.
- Security advisory identifier.
- Affected and patched versions.
- Whether the vulnerable functionality is reachable in this project.
- Impact on this POS system.
- Recommended mitigation.
- Compatibility and regression risks.
- Verification tests.
- Confidence and evidence.

**Do not assume that a dependency is secure merely because no vulnerability appears in one scanner.**

Also investigate insecure usage patterns even when a dependency itself has no public CVE.

Where a library is vulnerable, compare mitigation options:

1. Upgrade to a fixed upstream version.
2. Remove or replace the dependency.
3. Disable or restrict the vulnerable feature.
4. Apply compensating controls.
5. Maintain a reviewed patched fork if absolutely necessary.

If a source patch or fork is proposed, assess its licensing, maintenance burden, update strategy, ABI/API compatibility, and long-term support implications.

Do not modify dependency source code during this audit.

Document inaccessible advisory feeds as incomplete coverage, not as evidence of safety.

---

# PHASE 5 — Critical order workflow investigation: branch pickup

The restaurant owner has described an important intended operational workflow.

**Branch pickup orders are supposed to involve TWO separate print stages.**

## Stage A — Kitchen preparation ticket

When a pickup order is selected and its initial kitchen ticket has not yet been printed:

The primary action should indicate:

`طباعة فاتورة المطبخ`

This first ticket is sent to the kitchen for preparation and should not be treated as final payment or financial settlement.

The audit must establish exactly when the order is created and persisted, whether the kitchen ticket has a stable order identity, and whether the business flow supports preparing an order before final payment.

## Stage B — Final checkout and customer receipt

When the initial kitchen ticket has already been handled, the relevant action should instead indicate:

`متابعة إلى الدفع`

The order should proceed to the actual final-payment workflow.

Once the final transaction is successfully completed, the customer-facing receipt should be generated and printed or handled by the appropriate safe fallback.

The order should then leave the active pending-order table when the correct business conditions are met, without being deleted from sales history, payments, audit logs, or reporting records.

## Investigation requirements

Trace the existing implementation end-to-end and determine whether this intended workflow exists at all, whether it is partially implemented, or whether it conflicts with the current checkout-and-auto-print architecture.

Examine:

- Pickup order creation and persistence.
- Draft, submitted, preparing, awaiting-payment, paid, completed, and cancelled states, where applicable.
- Button labels and state transitions.
- Kitchen ticket generation.
- Payment confirmation.
- Final receipt generation.
- Order-table removal or state changes.
- Print job tracking and status.
- Reprints.
- Double-clicks and repeated requests.
- Client crashes and restarts.
- Backend restarts.
- Offline printers.
- Failed print reports.
- Transaction failures.
- Customer data and order retention.
- Audit history.
- Idempotency and concurrency.

Do not equate a queued ticket with a physically printed ticket.

Investigate how the UI should represent `NotRequested`, `Queued`, `Rendering`, `SpoolerAccepted`, `Failed`, `Unknown`, and `Confirmed` states, based on what the hardware and software can genuinely prove.

Do not invent physical-print confirmation capabilities.

Recommend a correct state machine, API contract changes, persistence design, UI behavior, and safe failure recovery if the intended workflow is not currently supported.

**Audit and report only. Do not implement this workflow.**

---

# PHASE 6 — Delivery printing and payment workflow

Investigate whether delivery orders are also intended to have:

1. An initial kitchen preparation ticket.
2. A later delivery/customer-facing ticket containing the appropriate order and delivery details.

The owner expects two distinct stages, but has not yet confirmed every detail of the currently observed delivery behavior.

Therefore, do not assume the implementation is defective before tracing it.

Identify exactly:

- When kitchen printing occurs.
- When delivery ticket printing occurs.
- Whether payment must already be received.
- What happens with cash on delivery.
- What happens with pending electronic transfers.
- Whether print jobs are missing, duplicated, or incorrectly routed.
- Whether customer address, phone, driver, and amounts appear correctly.
- Whether a failed print changes order or delivery status incorrectly.

Produce a current-versus-required workflow comparison and enumerate any missing decisions needing the owner's confirmation.

---

# PHASE 7 — Printer connectivity and true-to-paper invoice preview

This is a critical operational and UI requirement.

The POS should detect printer availability through the appropriate software and hardware channels.

When a correctly configured printer is available, the workflow should behave normally.

When no printer is connected, configured, accessible, or ready, the user should receive a useful, clearly identified printable preview of the ticket, rather than a raw programming representation.

**The preview must represent the actual intended paper output.**

It must not simply display JSON, DTO fields, debugging strings, or a loosely reconstructed receipt.

Investigate:

- Printer discovery.
- Exact installed printer queue names.
- Route-to-device configuration.
- Spooler status.
- Print Agent availability.
- USB, serial, TCP, and file transports.
- Actual physical printer availability versus misleading operating-system statuses.
- Different paper widths, including 58 mm and 80 mm where supported.
- Printable dot width.
- DPI and printer-specific margins.
- Arabic shaping and RTL layout.
- Glyph fallback and missing Arabic characters.
- Line wrapping.
- Truncated text.
- Text extending beyond paper edges.
- Incorrect ticket height.
- Overly large images or QR codes.
- Premature paper cutting.
- Blank or corrupted lines.
- Raster image generation.
- ESC/POS command composition.
- Windows Spooler transmission and acknowledgements.
- Network loss and power interruption.
- Automatic retries and duplicate-print protection.
- Displayed amounts and print-stage-specific content.

Review `BitmapTicketRenderer`, `EscPosTicketRenderer`, `WindowsSpoolerTicketTransport`, `PrintJobProcessor`, the ticket contracts, rendering rules, and every upstream component that supplies data to printing.

Investigate whether the same underlying layout/rendering logic can safely produce both the real paper output and the user-visible preview.

If true pixel-perfect equivalence cannot be guaranteed without specific printer hardware, clearly describe the calibration and physical tests required.

**The fallback preview must not pretend that a ticket has physically printed.**

Recommend the correct user experience when a printer is absent, offline, misconfigured, or fails mid-job.

Do not print to physical devices during this audit without explicit authorization.

---

# PHASE 8 — Exhaustive visual UI/UX and accessibility audit

**Visual correctness is a major priority, not a cosmetic afterthought.**

The owner has already discovered a reproducible visibility defect in the `تسجيل مصروف من الدرج` dialog.

In the provided screenshot, the unselected expense-category buttons have white or near-white backgrounds and extremely pale labels. Their text is difficult or nearly impossible to read.

The selected category `مستلزمات مطبخ وخضار` has a blue background with more readable text.

Treat this screenshot as a concrete visual regression case.

Investigate the real style, theme, converter, resource dictionary, template, binding, and state-selection logic that produces this appearance.

Do not assume the root cause solely from the screenshot.

Audit the ENTIRE application's visual presentation, including Cashier WinUI, Call Center WinUI, reusable controls, dialogs, management screens, WPF client where applicable, and shared styles.

Examine:

- Text contrast and background contrast.
- Default, selected, unselected, hover, pressed, focused, disabled, and error states.
- Incorrectly inherited foreground colors.
- Theme resource conflicts.
- Dark mode and light mode.
- Arabic RTL flow direction.
- Font fallback and Arabic glyph legibility.
- Text clipping and ellipsis.
- Overlapping labels and buttons.
- Hidden or unreachable controls.
- Modal sizing and scrolling.
- Missing or broken bindings.
- Unclear validation messages.
- Button state accuracy.
- Unresponsive click areas.
- Inconsistent spacing and sizing.
- Focus order and keyboard navigation.
- Accessibility and WCAG contrast guidance where applicable.
- Touch targets for cashier workflows.
- Visual hierarchy.
- Wrong or misleading visual status indications.
- Poor empty states.
- Loading indicators and long-running operation states.
- User feedback after success or failure.
- Localization and bidirectional numeric content.
- Display resolution and Windows scaling behavior.

Where possible, open and inspect every actual screen and state, not merely XAML source.

Use real screenshots or UI automation for evidence where permitted, and explicitly report screens that could not be opened.

Cover practical display sizes such as 1366×768 and 1920×1080, and scaling values such as 100%, 125%, and 150%.

Check additional low-resolution and high-contrast environments where feasible.

Record each visual finding with the exact UI control, XAML path, style resource, binding, visual state, screenshot evidence, and suggested correction.

Do not make visual changes during this audit.

---

# PHASE 9 — Database, financial, and business-logic integrity

Audit all money-moving and stock-moving workflows for correctness.

Focus on:

- Checkout and payment.
- Split payments.
- Pending and confirmed electronic payments.
- Cash on delivery.
- Refunds.
- Discounts and manager approval.
- Expenses and cash-drawer movements.
- Treasury transfers and balances.
- Shift opening and closing.
- Business-day reconciliation.
- Driver custody and settlements.
- Inventory deduction.
- Recipe consumption.
- Purchasing and supplier invoices.
- Wages and payroll.
- Backup and restore.
- Tax and pricing calculations.

For every workflow, examine:

- Transaction boundaries.
- Concurrency.
- SQL locking behavior.
- Deadlocks.
- Missing indexes and inefficient queries.
- Unique constraints.
- Idempotency.
- Replay handling.
- Partial failure.
- State transitions.
- Data validation.
- Audit logging.
- Incorrect financial totals.
- Incorrect use of zero for missing or unknown values.
- Potential duplicate records.
- Lost updates.
- Incorrect rollback or compensation behavior.

Never recommend a performance optimization that weakens financial correctness, traceability, or transaction safety.

Use isolated test data only, and do not run destructive tests.

---

# PHASE 10 — High-performance and low-end hardware engineering

The owner wants the POS to operate efficiently on the lowest practical hardware specifications WITHOUT losing essential features.

Investigate:

- Startup time.
- Peak and idle RAM usage.
- CPU load.
- UI responsiveness.
- Long-running background tasks.
- Database latency.
- Network request volume.
- Excessive polling.
- Redundant data loading.
- Blocking calls.
- UI-thread contention.
- Memory and native-resource leaks.
- Object allocations and garbage collection.
- Expensive collections and LINQ operations.
- Rendering cost.
- Image and font memory usage.
- Disk I/O.
- Database connection pools.
- N+1 queries.
- Excessive SQL reads and writes.
- Index usage.
- Caching strategies.
- Print queue throughput.
- Degraded connectivity.
- Recovery after disconnection.
- Build and deployment footprint.

Distinguish measured performance issues from theoretical concerns.

Use measurable benchmarks and repeatable profiling when feasible.

## Minimum hardware requirements

Provide two separate profiles:

**Minimum supported hardware:** The lowest practical specifications on which the complete current system can run reliably with all required features.

**Recommended production hardware:** A realistic configuration for smooth daily restaurant operation.

Specify:

- CPU class.
- RAM.
- Storage and free disk space.
- Windows edition, version, and architecture.
- .NET/runtime dependencies.
- SQL Server requirements.
- Client versus server hardware requirements.
- LAN and printer prerequisites.
- Expected operational limits.

Explicitly investigate Windows 7 and 32-bit compatibility.

Do not claim that .NET 8 WinUI or WPF clients run on Windows 7 merely because the UI framework name seems compatible.

If supporting older systems requires retargeting, replacing UI frameworks, or separating client/server roles, explain the required changes, risks, costs, and expected feature impact.

**Do not recommend dropping existing features merely to achieve lower hardware requirements.**

If a hardware target cannot safely support the complete feature set, report that limitation rather than weakening functionality silently.

For every optimization, provide expected benefit, supporting evidence, implementation complexity, regression risk, and tests required.

## IMPORTANT CLARIFICATION — Windows Compatibility and Performance

Windows 7 compatibility is NOT a requirement or development priority.

I only want to know whether the current application supports Windows 7, as an informational compatibility assessment.

Do NOT recommend redesigning, downgrading, retargeting, or replacing the application's architecture solely to support Windows 7 or 32-bit Windows.

My acceptable target environment includes Windows 10 64-bit, including lightweight installations, provided all required Windows components, .NET runtimes, and security updates are available.

The primary objective is to optimize the existing architecture for excellent performance and low resource consumption on modest Windows 10 64-bit hardware, while preserving ALL existing functionality, security, reliability, and UI quality.

Investigate and document:
- The minimum realistic hardware specifications for the current application.
- Performance bottlenecks and potential optimizations.
- Whether a particular Windows 10 installation is compatible and properly supported.
- Windows 7 compatibility as informational only.

Do not make architecture changes during this audit.

Do not sacrifice features, security, or reliability to achieve lower hardware requirements.

Prefer optimizing the current architecture rather than migrating to another UI framework or technology unless there is compelling evidence of a serious architectural problem unrelated to Windows 7.

---

# PHASE 11 — Code duplication, maintainability, and architecture

Detect:

- Exact duplicate functions.
- Semantically equivalent duplicate functions.
- Repeated business calculations.
- Repeated authorization logic.
- Duplicate API implementations.
- Parallel implementations with divergent behavior.
- Unnecessary wrappers and abstractions.
- Dead code.
- Unused services and dependencies.
- Inappropriate coupling.
- Circular dependencies.
- Multiple sources of truth.
- Missing separation of responsibilities.
- Inconsistent error handling.
- Unnecessary database round trips.
- Contradictory business rules.

Differentiate intentional duplication from harmful duplication.

For every recommendation, explain how to refactor safely without losing behavior.

Identify areas where adding a small shared abstraction would improve correctness, and areas where abstraction would add unnecessary complexity.

---

# PHASE 12 — Features and improvement recommendations

Provide two separate, clearly prioritized categories.

## A. Recommended for implementation now

Include essential fixes and features needed for a robust, secure, efficient, and usable restaurant POS.

This may include printing reliability, order-state controls, backup observability, critical UI corrections, operational alerts, security hardening, and recovery features — but recommend only what the actual evidence supports.

Explain:

- Problem solved.
- Business importance.
- Expected user benefit.
- Source components involved.
- Dependency changes.
- Implementation complexity.
- Regression risks.
- How to test.
- Why it should be prioritized now.

## B. Recommended for future development

Include genuinely useful optional enhancements such as operational analytics, improved reporting, workflow automation, monitoring, or better administrative tools, only where justified.

Do not recommend features merely to increase the size of the project.

Explain dependencies, costs, trade-offs, and whether each feature requires additional hardware or services.

**Do not implement any of these recommendations.**

---

# PHASE 13 — Mandatory evidence and independent verification

Every finding must contain:

- Unique finding ID.
- Category.
- Severity: Critical, High, Medium, Low, Informational.
- Confidence: Confirmed, Probable, Needs Verification.
- Exact source file.
- Git commit and source hash.
- Class or function.
- Precise line range.
- Description of existing behavior.
- Actual evidence.
- Conditions that trigger the issue.
- Reproduction steps where safe.
- Impact on security, finances, reliability, usability, or performance.
- Root-cause analysis.
- Recommended solution.
- Related files.
- Required regression tests.
- Remaining uncertainty.

Never invent vulnerability claims, code references, screenshots, benchmarks, or test results.

Have an independent reviewer or verification pass challenge high-severity findings before promoting them to CONFIRMED.

If sub-agents are used, retain independent findings rather than silently discarding disagreements.

Record findings that were rejected as false positives and why.

---

# PHASE 14 — Coverage and quality gates

Track actual progress separately from task assignment.

The audit must report:

- Tracked files discovered.
- Files mapped.
- Files actually reviewed.
- Files awaiting review.
- Functions discovered.
- Functions actually reviewed.
- API endpoints traced.
- UI screens and visual states inspected.
- Security-critical flows reviewed.
- Dependencies inventoried.
- Dependency advisories checked.
- Printing flows reviewed.
- Findings verified.
- Tests executed.
- Tests skipped and why.
- Areas requiring physical hardware verification.
- Unresolved questions.

Do not count assigning a file to an agent as reviewing that file.

Do not mark an audit complete when one or more categories or source files remain unexamined without explicitly disclosing those gaps.

Do not use impressive but unsupported percentages.

Validate report consistency and source references before publication.

Preserve checkpoints so that large-scale work can continue reliably over multiple sessions.

---

# PHASE 15 — Required final deliverables

Produce a structured audit workspace with at least:

1. `EXECUTIVE_SUMMARY.md` — Arabic overview of the most important findings.
2. `ARCHITECTURE_AND_SOURCE_MAP.md` — verified architecture and file mapping.
3. `SOURCE_INVENTORY.csv` — file-level coverage.
4. `FUNCTION_INDEX.json` — symbols and method coverage.
5. `SECURITY_AND_BACKDOOR_AUDIT.md` — adversarial security investigation.
6. `DEPENDENCY_SECURITY_AND_SBOM.md` — direct and transitive package assessment.
7. `PICKUP_AND_DELIVERY_WORKFLOW_AUDIT.md` — current versus required order lifecycle.
8. `PRINTING_RELIABILITY_AND_PREVIEW.md` — physical printing, Arabic rendering, and preview fallback.
9. `UI_UX_VISUAL_AUDIT.md` — all observed visual defects and evidence.
10. `DATABASE_AND_FINANCIAL_INTEGRITY.md` — transaction correctness.
11. `PERFORMANCE_AND_MINIMUM_HARDWARE.md` — measured bottlenecks and hardware guidance.
12. `CODE_DUPLICATION_AND_ARCHITECTURE.md` — structural improvement findings.
13. `TEST_COVERAGE_AND_MISSING_TESTS.md` — coverage and unverified behavior.
14. `IMMEDIATE_FIXES_AND_FEATURES.md` — actionable work recommended now.
15. `FUTURE_IMPROVEMENTS_AND_FEATURES.md` — optional future development.
16. `MASTER_FINDINGS.json` — machine-readable defect registry.
17. `AUDIT_COVERAGE.json` — measured audit completion.
18. `IMPLEMENTATION_ROADMAP.md` — ordered implementation plan with precise source locations.
19. `FILE_REPORTS/` — a separate comprehensive report for EVERY tracked project file.

Each final report should allow a future Codex implementation session to identify exactly what file, class, function, or UI resource to modify.

Prioritize:

- **P0:** Exploitable critical security issues, serious financial integrity issues, data loss, or catastrophic operational failures.
- **P1:** Important security, printing, correctness, UI usability, and operational reliability defects.
- **P2:** Performance optimization, maintainability, and valuable near-term improvements.
- **P3:** Future features and nonessential enhancements.

Separate confirmed defects from speculative risks and optional design suggestions.

---

# FINAL RULE — AUDIT ONLY, NO IMPLEMENTATION

You have permission to inspect the repository, analyze source, observe the already approved local test environment, execute safe read-only diagnostics, and run non-destructive tests where their side effects have been verified.

You do NOT have permission to modify application source code, install new system software, upgrade libraries, change database schemas, reset data, alter production services, or print physical tickets.

If a test requires such an action, document the proposed action and obtain explicit approval first.

Do not use existing production accounts, financial data, or pending client operations for test execution.

Do not commit or push audit artifacts until the owner authorizes publication.

**Your task ends with evidence-backed audit reports, recommendations, and a prioritized implementation roadmap. Not with code changes.**

Begin with verified baseline discovery and the audit execution plan. Then conduct the complete deep audit, using the methodology you judge most reliable.

Prefer slow, careful, independent review over fast, shallow output.

The owner will authorize the implementation phase separately after reviewing the findings.