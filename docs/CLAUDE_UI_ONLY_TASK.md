# Dedicated task: complete native UI and interaction verification

Owner request, 2026-11-08: give UI its own task and check every button. The owner will also send additional requirements to check against the project.

## Mission and ownership

Own the complete user interface of both active WinUI clients: Cashier/Management and Call Center. Improve the experience and prove that every reachable action behaves correctly. Execute the work; do not stop after an inventory or screenshots.

This is a separate UI workstream. Choose your approach and design decisions, reuse existing findings, and coordinate with any ongoing implementation or test runs before editing shared files or building. Do not launch a repetitive analyst/implementer/reviewer cycle for every control.

Read CLAUDE.md and the current UI and verification reports. Preserve completed fixes and evidence. Historical screen counts are not a coverage boundary: discover the current navigation, XAML, templates, commands, handlers, dialogs, and conditional controls. Treat the WPF client as a legacy reference unless evidence shows it is actively used.

## Inspect every action, not only every screen

Build an inventory of all reachable interactive controls in both clients. Include:

- Buttons and icon buttons, including repeated row actions and controls inside templates.
- Navigation entries, tabs, dropdowns, context menus, flyouts, toggles, and theme controls.
- Search, filters, sorting, date ranges, refresh, pagination, and selection.
- Forms, validation, numeric inputs, variant selection, quantity controls, preparation notes, and cart actions.
- Dialog opening, confirm, cancel, close, back, Escape, Enter, and keyboard shortcuts.
- Recovery banners, retry, discard, and actions visible only under particular permissions or states.

For each action, identify its visible label or AutomationId, owning view, bound command/event handler, visibility/enabling conditions, expected effect, observed effect, and supporting evidence. Inventorying a binding does not prove that the action works.

Use stable AutomationIds where useful. For repeated controls, test the action type and meaningful row/state variations; do not create hundreds of redundant tests for identical instances. Explain the covered cases and exceptions.

## Execute realistic native workflows

Open the real programs against disposable FalahTest_ data and isolated client storage. Check the effect after each meaningful action: UI state, persisted result or server response where relevant, and behavior after refresh/reopening. Clicking without checking the outcome is insufficient.

Cover all reachable screens and nested dialogs, including:

- Login, lock/logout, permissions, and the shared shell/header.
- Cashier catalogue, cart, variants, notes, pickup/customer details, checkout, payment methods, refunds, and cashier operations.
- Orders, delivery assignment/results, drivers, custody, customers, and Call Center search/order/customer flows.
- Shift open/close, cash count, treasury dialogs, pending-operation recovery, and business-day controls.
- Menu editing, prices, recipes, inventory counts/movements/waste, suppliers, and purchasing receive/pay.
- Employees, attendance, payroll, users, settings, reports, audit, printing, and backup screens.

Exercise populated and empty states, long Arabic names, large amounts, dense tables, no search results, invalid inputs, loading, failure, permission refusal, and repeated submission. Use seeded test roles rather than relying only on an all-permissions owner session.

For financial controls, check the existing safety behavior, including retaining unresolved requests and preventing unintended repeated submission. Any deeper financial or server defect belongs in a clearly linked dependency; do not silently change business rules to make a UI test pass.

## Visual quality and usability

Make both clients coherent and suitable for daily Arabic restaurant work. You may improve hierarchy, spacing, typography, labels, navigation, colors, feedback, and layout using professional judgment.

Check RTL and mixed Arabic/Latin text, currency and date formatting, readable tables, complete labels, touch targets, scrolling affordances, and dialogs whose primary actions remain reachable. Users must understand why an action is unavailable and what to do after an error.

Verify Light and Dark, saved appearance preferences, and live theme changes in both clients. Test resizing and the practical minimum window size. Check screens and important dialogs at representative 1366×768 and 1920×1080 configurations where the environment permits.

Record physical pixels, effective layout units, and actual Windows scale separately. A smaller window at 125% can exercise a layout width equivalent to 150%; it does not prove actual 150% font/icon rendering. Do not change global display settings without authorization. Record unavailable configurations explicitly.

Perform a keyboard-only pass: logical Tab/Shift+Tab order, visible focus, Enter/Escape, dropdown operation, modal focus containment/return, and shortcuts that cannot trigger hidden underlying actions. Test touch where supported; otherwise mark it unverified.

Measure contrast for key text and interactive states using actual colors; distinguish measurement from visual judgment. Do not claim a numeric ratio from screenshots by eye.

## Fixes and verification

Fix reproducible UI defects and verify the resulting behavior in the native app. Add meaningful regression tests for behavioral changes. Purely visual adjustments do not need tests that merely repeat the implementation; native inspection and useful before/after evidence are required.

Do not weaken tests, remove useful actions, hide failures, or relabel an unverified control as passed. If a feature is absent, identify it as absent rather than creating a cosmetic button that does nothing.

Reuse the accepted automated baseline. Run targeted/adjacent checks for your changes, build both affected native clients, and choose broader regression checks according to the final scope. Coordinate shared bin/obj outputs and avoid unnecessary full-suite repetition.

## Additional requirements from the owner

The owner will send examples, screenshots, or feature descriptions. For each received item, determine its current state before implementing:

| State | Required explanation |
|---|---|
| Present and verified | Where to find it and what evidence proves it works |
| Present but partial | What works and the exact missing behavior |
| Present but unverified | What exists and the verification needed |
| Absent | What would need to be added |
| Unclear | The smallest missing detail needed to assess it |

Do not invent the upcoming requirements or treat an example as authorization to change financial policy. Continue independent UI work while waiting for them. Record new scope separately from defects in existing controls.

## Safety boundaries

Preserve all owner changes. No commit, push, reset, clean, stash, deployment, or service installation is authorized. Do not access production Falah data, real station pending operations, or real credentials. Use disposable test data and process-scoped client storage, and coordinate cleanup of test resources you own.

Do not redesign financial rules, tax/payroll policy, permission grants, or migrations as part of cosmetic work. If a backend dependency blocks meaningful UI verification, document the exact dependency, leave that action blocked, and continue elsewhere. Never claim an untested fix is complete.

## Deliverables and completion criteria

- Maintain docs/UI_CONTROL_VERIFICATION_MATRIX.csv: client, screen/dialog, control/action, role/state, expected result, observed result, verification status, and evidence/issue reference. Use VERIFIED, FIXED_AND_VERIFIED, BLOCKED, or NOT_TESTED; reserve NOT_APPLICABLE for a documented reason.
- Update docs/UI_VERIFICATION_AND_CHANGES.md with changes, actual coverage, dimensions/scaling, screenshots, and explicit gaps.
- Create docs/UI_OWNER_REQUESTS.md when the first additional owner requirement arrives; record each item and the presence assessment above.
- Update the relevant issue files and project overview when verified behavior changes.
- Give a concise Arabic closeout with control/action counts per client, remaining blockers, and where to see the completed UI.

Do not call the UI task complete while reachable actions remain untested or required UI work is unfinished. If external constraints prevent completion, report the completed scope and exact remaining work honestly. Automated test success alone does not close this task.
