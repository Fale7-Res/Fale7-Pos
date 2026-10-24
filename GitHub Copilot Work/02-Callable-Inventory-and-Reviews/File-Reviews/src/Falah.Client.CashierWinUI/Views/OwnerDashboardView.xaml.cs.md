# src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `bc6e2ad5af1b43888b2c25ea19e1011a8ba90f709b1007f9e8fe34503e3b1047`

Worktree SHA-256: `bc6e2ad5af1b43888b2c25ea19e1011a8ba90f709b1007f9e8fe34503e3b1047`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: owner KPI dashboard constructor only.

## Source paths and function/resource analysis

- `OwnerDashboardView` at `src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml.cs:5`: Entire file is imports, namespace, partial UserControl and one InitializeComponent constructor for owner KPI dashboard. No business methods, event subscriptions, direct API calls, cancellation, data mutations, SQL, process launching or printing are implemented in this code-behind. Parent DataContext flows into its distinct XAML; source-level absence here does not establish behavior or authorization of that XAML/VM.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

NOT_APPLICABLE standalone business logic tests; actual associated XAML resource/binding compilation and native integration are required separately. Constructor read in full; no rendered UI proof.

## Recommendations (not implemented)

No code-behind defect found in its limited responsibility. Review separately tracked XAML and bound view model before claiming the screen works.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
