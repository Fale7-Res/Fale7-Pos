# UI checkpoint candidates — not final findings

Source: accessible original ui_visual recovery reply. Final findings JSON and UI summary were not saved. Recheck actual source and precise evidence before promotion. No screenshot or runtime reproduction claimed.

- UI-001: memory-only pending delegates for cashier financial operations/Treasury recovery.
- UI-002: CashierView expense buttons lines313–340 and CheckoutView payment buttons lines31–42 hardcode WhiteBrush with pale dark-theme TextPrimary; visual verification pending.
- UI-003: pickup kitchen-before-payment stage absent.
- UI-004: ThemeResources.Get dictionary traversal and converter invalidation.
- UI-005: electronic recovery with LastCheckoutResult null may disable confirmation/cancel and editing.
- UI-006: pickup schedule omitted by ResetDelivery.
- UI-007: Call Center asynchronous customer lookup may overwrite newer input.
- UI-009: PrintingViewModel exposes four of six server routes.
- UI-010: keyboard guards may omit operational modals.
- UI-011: ClearResolvedPrint may leave definitively rejected manual reprint on disk.
- UI-012: Call Center receipt reprint defaults IncludeKitchenRoutes true; peer static confirmation, parent challenge pending.
- UI-013: legacy WPF change parsing may mix currency units.
- BackupViewModel Completed versus Succeeded status mismatch requires handler check.

Recover these candidates with symbols, paths, line numbers, confidence, severity, counterarguments and proposed tests. Do not invent missing UI-008 or assume numbering is exhaustive.
