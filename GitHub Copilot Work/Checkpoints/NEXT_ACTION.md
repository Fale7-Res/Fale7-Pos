# Next Action: Dispatch Specialist Parallel Wave 2

- **Previous Wave:** Wave 1 Complete (180 callables promoted across 14 files; 100% verified source-grounded reviews).
- **Current Unfinished Production Callables:** `6,361`
- **Immediate Task:** Dispatch Wave 2 batches to the 5 active subagents:
  1. `financial-auditor`: `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`, `ConfirmElectronicPaymentHandler.cs`, `RefundOrderHandler.cs` (High-priority financial core: FIN-001, FIN-002, FIN-003, FIN-016).
  2. `security-auditor`: `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs`, `ResetUserPinHandler.cs`, `src/Falah.Server/Program.cs` auth endpoints (SEC-001, SEC-002).
  3. `mirrored-code-auditor`: `src/Falah.Domain/Orders/Order.cs`, `src/Falah.Infrastructure/Orders/MenuPricingService.cs`.
  4. `printing-workflow-auditor`: `src/Falah.PrintAgent/PrintJobProcessor.cs`, `src/Falah.Domain/Printing/EscPosTicketRenderer.cs`.
  5. `ui-maintainability-auditor`: `src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs`, `src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs`.
