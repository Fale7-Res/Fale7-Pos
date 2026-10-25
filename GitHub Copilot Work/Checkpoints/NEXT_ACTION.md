# Next Action: Dispatch Specialist Parallel Wave 4 (Strict Acceptance Gated)

- **Audit Governance:** Strict Callable Acceptance Gate Enforced.
- **Accepted Substantive Callables:** `783`
- **Accepted Independently Verified:** `812`
- **Current Unfinished Production Callables:** `2,241`
- **Immediate Task:** Dispatch Wave 4 batches to the 5 active subagents with mandatory exact callable ID mapping:
  1. `financial-auditor`: `src/Falah.Infrastructure/Treasury/DepositHandler.cs`, `SafeDropHandler.cs`, `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs`, `EfFinancialJournal.cs`.
  2. `security-auditor`: `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs`, `UsersViewModel.Actions.cs` (SEC-002 RetainedRequestStore PIN storage).
  3. `mirrored-code-auditor`: `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Refund.cs` vs WinUI refund handling (FIN-003, FIN-004).
  4. `printing-workflow-auditor`: `src/Falah.PrintAgent/BitmapTicketRenderer.cs` (Arabic rasterization via GS v 0), `src/Falah.Domain/Printing/TicketLayout.cs`.
  5. `ui-maintainability-auditor`: `src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs`, `src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs`.
