# tests/Falah.Client.Tests/Falah.Client.Tests.csproj

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `b0ef52a23ebeacf41f38604be58485742b2d90bca0c9f5fd4c7986bae7a3895a`; worktree SHA256 `8353abf540755b7da766e09fbba673555674ee433e61c283fb0d405480c6f2f2`; LF-equivalent `True`.

Actual role: Headless source-linked client regression project.

## Function/resource review

- `TargetFramework` at `tests/Falah.Client.Tests/Falah.Client.Tests.csproj:2`: net8.0 nonnative test project; xunit2.5.3/TestSDK17.8/MVVM8.3.2. NativeFooter/Screen/Management probes explicitly excluded from normal test compilation.

- `Compile Include` at `tests/Falah.Client.Tests/Falah.Client.Tests.csproj:17`: Links cashier services/models/VM plus CallCenter services/models/VM and root/login. ThemeService/Resources/Settings.Appearance excluded, so no coverage of real WinUI theme resource resolution; XAML/views not compiled.

- `ProjectReference` at `tests/Falah.Client.Tests/Falah.Client.Tests.csproj:12`: Only Application project referenced, tests compile copied/link source rather than actual frontend assemblies. Stub Header/DispatcherTimer replace real native dependencies; source business logic tests valid but UI/runtime behavior not proved.

## Tests and limitations

Explicit test-scope caveat required in final report; no tests executed by reviewer.

## Recommendations (not implemented)

Keep headless regression suite but add native/real HTTP isolated lifecycle evidence. Do not count native probe files as executed unit tests.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
