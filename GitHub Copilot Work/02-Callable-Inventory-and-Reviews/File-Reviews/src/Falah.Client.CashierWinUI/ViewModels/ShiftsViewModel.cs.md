# File Review: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:38:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:38:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftsViewModel(Falah.Client.CashierWinUI.Services.AppSession, Falah.Client.CashierWinUI.ViewModels.HeaderViewModel)
- **Lines:** [38-42]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftsViewModel(Falah.Client.CashierWinUI.Services.AppSession, Falah.Client.CashierWinUI.ViewModels.HeaderViewModel) across lines 38-42.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:65:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:65:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.OnActiveShiftChanged(Falah.Application.Shifts.CurrentShiftDto?)
- **Lines:** [65-71]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.OnActiveShiftChanged(Falah.Application.Shifts.CurrentShiftDto?) across lines 65-71.
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
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ReprintShiftOpenAsync(Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftRow?) across lines 80-82.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:84:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:84:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ReprintShiftCloseAsync(Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftRow?)
- **Lines:** [84-86]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ReprintShiftCloseAsync(Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftRow?) across lines 84-86.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:95:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:95:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ReprintAsync(Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftRow?, string, string)
- **Lines:** [95-112]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ReprintAsync(Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftRow?, string, string) across lines 95-112.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:116:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:116:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.FormatCairo(DateTimeOffset)
- **Lines:** [116-117]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.FormatCairo(DateTimeOffset) across lines 116-117.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:119:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:119:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.LoadAsync()
- **Lines:** [119-168]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.LoadAsync() across lines 119-168.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:176:9:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:176:9:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftRow.ShiftRow(Falah.Application.Reports.ShiftSummaryDto)
- **Lines:** [176-176]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ShiftRow.ShiftRow(Falah.Application.Reports.ShiftSummaryDto) across lines 176-176.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
