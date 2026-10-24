# tests/Falah.Client.Tests/NativeManagementLayoutProbe.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `faac9344c780d29618b3e3d408cb1c3543aa68e1d747f595a748c0828345f562`; worktree SHA256 `faac9344c780d29618b3e3d408cb1c3543aa68e1d747f595a748c0828345f562`; LF-equivalent `True`.

Actual role: Opt-in populated attendance/user-dialog native geometry fixture.

## Function/resource review

- `FixtureHandler` at `tests/Falah.Client.Tests/NativeManagementLayoutProbe.cs:18`: Throws any nonGET; users/all returns synthetic owner and all otherGET permission list. No real authentication or mutation.

- `RunAsync(` at `tests/Falah.Client.Tests/NativeManagementLayoutProbe.cs:29`: Runs4 states at6 nominal sizes. Uses real VM constructors/dialog commands and long Arabic names/errors, attaches real views. Measures named card/scroll/footer and asserts positive geometry,44px footer/scroll, card viewport bounds and footer separation. Each exception captured in returned record rather than thrown to top runner. Covers only attendance correction and user create.

- `Bounds(` at `tests/Falah.Client.Tests/NativeManagementLayoutProbe.cs:73`: TransformToVisual reports layout rectangle in parent DIP coordinates. No keyboard focus, scroll reach, text clipping, native DPI or dark theme pixel validation.

## Tests and limitations

SOURCE_REVIEW_ONLY, NOT_EXECUTED. Synthetic owner all-permission hides role-specific behavior; no source says real user setup validated.

## Recommendations (not implemented)

Keep opt-in probe isolated and treat returned error fields as failures; broaden independently justified modal cases after authorization.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
