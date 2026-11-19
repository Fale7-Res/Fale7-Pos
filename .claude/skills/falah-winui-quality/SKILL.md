---
name: falah-winui-quality
description: Use when building or changing a screen in the Falah WinUI 3 clients (Falah.Client.CashierWinUI, Falah.Client.CallCenterWinUI) - view models, XAML views, money labels, commands that send money to the server, owner/report screens. Covers the project's MVVM patterns, durability rules, Arabic RTL and accessibility requirements, and how to prove a screen works.
---

# Falah WinUI quality

## Shape of a screen

- View model: CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`), split into partial
  files per concern (`CashierViewModel.Checkout.cs`, `.Refund.cs`, `.Durability.cs`, ...). No
  business rule lives in the client; the server decides and the client shows its Arabic message.
- Server calls go through `Services/ApiClient*.cs` and return `ApiResult<T>`:
  `Success`, `Error` (server's own words), `OutcomeUnknown` (5xx / transport — the command may have
  happened), `StatusCode`.
- **Money commands must survive a crash**: keep the request in `RetainedRequest` /
  `CashierViewModel.Remember(...)` before sending, retry it with the same request id, clear it only
  when the outcome is known. Never generate a new request id for a retry.
- States: `LoadingStateView`, `EmptyStateView`, `ErrorStateView` (with retry),
  `PermissionDeniedStateView` (Controls/). Use them instead of blank areas.
- Permission-gated UI reads `AppSession.HasPermission(...)`; the server still enforces it.

## Labels and numbers

- Money is piastres in the DTO; display as ج with the shared formatter/converters
  (`Converters/Converters.cs`); never round in the view model.
- Use the names from `docs/METRIC_DEFINITIONS.md`: "إجمالي المبيعات" (gross) vs "صافي المبيعات" (net
  of refunds, VAT included), "الإيراد بدون الضريبة", "تكلفة الطعام التقديرية", "النتيجة التشغيلية
  التقديرية". An estimate says it is an estimate. A pending electronic amount is never shown as cash.
- Say which basis a report uses (يوم تقويمي / يوم عمل / وردية) next to its date picker.

## Arabic RTL and accessibility

- Root `FlowDirection="RightToLeft"`; numbers and order numbers stay readable (LTR runs where needed).
- Every interactive control and every figure a test or screen reader must find gets
  `AutomationProperties.Name` (Arabic) and, where automation needs it, `AutomationProperties.AutomationId`.
- Keyboard: tab order follows reading order; Enter submits, Esc closes dialogs; focus moves to the
  first field of a dialog and back to the invoking control when it closes.
- Destructive or irreversible actions (void, refund, close) confirm with the amount and the target.

## Verify

1. `dotnet build src/Falah.Client.CashierWinUI/Falah.Client.CashierWinUI.csproj -p:Platform=x64` — 0 errors.
2. View-model tests in `tests/Falah.Client.Tests` (no UI; fake `ApiClient` handlers) for every new
   command path, including `OutcomeUnknown` and recovery after restart.
3. Run it: start the server (Development, its own test database — never `Falah`), launch the app,
   and drive the screen; with WinApp CLI (`winapp ui ...`, see the `winui:winui-ui-testing` skill)
   inspect the accessibility tree and capture screenshots. Record what was run and what was only
   built.

## Done means

Connected to the real endpoint, right labels and basis, all states handled, durable where money moves,
accessible names present, client tests green, and the screen actually exercised (or the report says
plainly that it was only built).
