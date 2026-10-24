# src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs

**path**: src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: a77a348c179416b551b4bbf173398f29c8b2430244609f3fcc23d52d0ac1bd13

**git_blob_sha256**: a77a348c179416b551b4bbf173398f29c8b2430244609f3fcc23d52d0ac1bd13

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 47]]

**functionality_and_reasoning**: Full47 lines read. Cashier sees provided open shift drawer expenses but does not verify shift.OpenedByUserId equalsactor despite OWN doc; single shared active shift reduces exposure but still claim inaccurate. Noncashier role arbitrary gets day-wide expenses without permission in service, root gate required. DateOnly.TryParse culture-dependent. No actor Active check here session/host handles.

**significant_methods**: Constructor; GetVisibleExpensesAsync; ToDto

**interactions_and_risks**: Full47 lines read. Cashier sees provided open shift drawer expenses but does not verify shift.OpenedByUserId equalsactor despite OWN doc; single shared active shift reduces exposure but still claim inaccurate. Noncashier role arbitrary gets day-wide expenses without permission in service, root gate required. DateOnly.TryParse culture-dependent. No actor Active check here session/host handles.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared unique active shift and root endpoint ownership not assumed.

**recommendations**: Explicit ownership/permission model independent role string, canonical ISO date parser and consistent no-current-shift response.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
