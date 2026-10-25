# File Review: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:38:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:38:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftsViewModel(Falah.Client.CashierWinUI.Services.AppSession, Falah.Client.CashierWinUI.ViewModels.HeaderViewModel)
- **Lines:** [38-42]
- **Behavior:** Initializes ShiftsViewModel with injected AppSession and HeaderViewModel dependencies for user session access and shell header synchronization.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:65:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:65:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.OnActiveShiftChanged(Falah.Application.Shifts.CurrentShiftDto?)
- **Lines:** [65-71]
- **Behavior:** Partial property-changed handler for ActiveShift. Fires OnPropertyChanged for HasActiveShift, ActiveShiftStatusLabel, and CanCloseActiveShift to update shift status badges and button states.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:73:53:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:73:53:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.History.get
- **Lines:** [73-73]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.History.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:80:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:80:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ReprintShiftOpenAsync(Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftRow?)
- **Lines:** [80-82]
- **Behavior:** Dispatches reprint request for shift opening receipt by calling ReprintAsync(row, "ShiftOpening", "إعادة طباعة إيصال فتح الوردية").
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:84:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:84:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ReprintShiftCloseAsync(Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftRow?)
- **Lines:** [84-86]
- **Behavior:** Dispatches reprint request for shift closing receipt by calling ReprintAsync(row, "ShiftClosing", "إعادة طباعة تقرير إغلاق الوردية (Z-Report)").
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:95:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:95:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ReprintAsync(Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftRow?, string, string)
- **Lines:** [95-112]
- **Behavior:** Core reprint method. Validates row is not null and not already reprinting. Issues ReprintDocumentRequest with unique request ID (rp-{Guid:N}), documentKind, shift ID, actor, and reason via Api.ReprintDocumentAsync. Sets ReprintMessage with status notification.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:116:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:116:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.FormatCairo(DateTimeOffset)
- **Lines:** [116-117]
- **Behavior:** Converts UTC timestamp to Cairo timezone (BusinessClock.CairoTimeZone) and formats string as yyyy-MM-dd HH:mm.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:119:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:119:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.LoadAsync()
- **Lines:** [119-168]
- **Behavior:** Asynchronously loads active shift and shift history. Fetches current shift, updates _session.ShiftId and _session.ShiftName, and if CanSeeShiftTotals is permitted, queries active shift summary and historical shifts, populating History collection with ShiftRow instances.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:176:9:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:176:9:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftRow.ShiftRow(Falah.Application.Reports.ShiftSummaryDto)
- **Lines:** [176-176]
- **Behavior:** Initializes a ShiftRow display adapter wrapping a ShiftSummaryDto, exposing calculated balance difference, Cairo-formatted timestamps, and shift cash breakdown metrics.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
