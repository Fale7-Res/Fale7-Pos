# File Review: src/Falah.PrintAgent/PrintServerClient.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.PrintAgent/PrintServerClient.cs.

## Callables

### Callable: src/Falah.PrintAgent/PrintServerClient.cs:13:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:13:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.IPrintServerClient.AuthenticateAsync(string, CancellationToken)
- **Lines:** [13-13]
- **Behavior:** Executes logic for Falah.PrintAgent.IPrintServerClient.AuthenticateAsync(string, CancellationToken) across lines 13-13.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:14:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:14:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.IPrintServerClient.GetPendingAsync(string, CancellationToken)
- **Lines:** [14-14]
- **Behavior:** Executes logic for Falah.PrintAgent.IPrintServerClient.GetPendingAsync(string, CancellationToken) across lines 14-14.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:15:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:15:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.IPrintServerClient.GetTicketAsync(string, CancellationToken)
- **Lines:** [15-15]
- **Behavior:** Executes logic for Falah.PrintAgent.IPrintServerClient.GetTicketAsync(string, CancellationToken) across lines 15-15.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:16:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:16:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.IPrintServerClient.ReportResultAsync(Falah.Application.Printing.ReportPrintJobResultRequest, CancellationToken)
- **Lines:** [16-16]
- **Behavior:** Executes logic for Falah.PrintAgent.IPrintServerClient.ReportResultAsync(Falah.Application.Printing.ReportPrintJobResultRequest, CancellationToken) across lines 16-16.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:28:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:28:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.PrintAgent.PrintServerClient.PrintServerClient(string, TimeSpan)
- **Lines:** [28-31]
- **Behavior:** Executes logic for Falah.PrintAgent.PrintServerClient.PrintServerClient(string, TimeSpan) across lines 28-31.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:33:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:33:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.PrintServerClient.AuthenticateAsync(string, CancellationToken)
- **Lines:** [33-40]
- **Behavior:** Executes logic for Falah.PrintAgent.PrintServerClient.AuthenticateAsync(string, CancellationToken) across lines 33-40.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:42:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:42:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.PrintServerClient.GetPendingAsync(string, CancellationToken)
- **Lines:** [42-47]
- **Behavior:** Executes logic for Falah.PrintAgent.PrintServerClient.GetPendingAsync(string, CancellationToken) across lines 42-47.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:49:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:49:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.PrintServerClient.GetTicketAsync(string, CancellationToken)
- **Lines:** [49-54]
- **Behavior:** Executes logic for Falah.PrintAgent.PrintServerClient.GetTicketAsync(string, CancellationToken) across lines 49-54.
- **Validations:** Validation checks detected: False. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:56:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:56:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.PrintServerClient.ReportResultAsync(Falah.Application.Printing.ReportPrintJobResultRequest, CancellationToken)
- **Lines:** [56-61]
- **Behavior:** Executes logic for Falah.PrintAgent.PrintServerClient.ReportResultAsync(Falah.Application.Printing.ReportPrintJobResultRequest, CancellationToken) across lines 56-61.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:63:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:63:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.PrintServerClient.Dispose()
- **Lines:** [63-63]
- **Behavior:** Executes logic for Falah.PrintAgent.PrintServerClient.Dispose() across lines 63-63.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
