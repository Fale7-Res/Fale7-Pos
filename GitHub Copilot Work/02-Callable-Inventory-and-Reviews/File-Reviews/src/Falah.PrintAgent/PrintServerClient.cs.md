# File Review: src/Falah.PrintAgent/PrintServerClient.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.PrintAgent/PrintServerClient.cs.

## Callables

### Callable: src/Falah.PrintAgent/PrintServerClient.cs:13:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:13:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.IPrintServerClient.AuthenticateAsync(string, CancellationToken)
- **Lines:** [13-13]
- **Behavior:** Interface method contract for authenticating the print agent station against the backend using a staff/station PIN to acquire a session token.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:14:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:14:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.IPrintServerClient.GetPendingAsync(string, CancellationToken)
- **Lines:** [14-14]
- **Behavior:** Interface method contract for polling pending print jobs assigned to a specific routing destination (e.g. kitchen or cashier).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:15:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:15:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.IPrintServerClient.GetTicketAsync(string, CancellationToken)
- **Lines:** [15-15]
- **Behavior:** Interface method contract for retrieving composed ticket layout and printable ESC/POS or text payload for a specific job ID.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:16:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:16:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.IPrintServerClient.ReportResultAsync(Falah.Application.Printing.ReportPrintJobResultRequest, CancellationToken)
- **Lines:** [16-16]
- **Behavior:** Interface method contract for reporting print job execution results (success or failure with error details) back to the server.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:28:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:28:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.PrintAgent.PrintServerClient.PrintServerClient(string, TimeSpan)
- **Lines:** [28-31]
- **Behavior:** Initializes PrintServerClient, instantiating an internal HttpClient configured with the specified baseAddress URI and timeout. Note: Direct instantiation of HttpClient rather than using IHttpClientFactory can cause socket starvation if instances are frequently created and discarded, though this client implements IDisposable.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:33:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:33:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.PrintServerClient.AuthenticateAsync(string, CancellationToken)
- **Lines:** [33-40]
- **Behavior:** Authenticates print agent by posting LoginByPinRequest(pin) to /api/auth/login-by-pin. Verifies HTTP success via response.EnsureSuccessStatusCode(), deserializes LoginResultDto (throwing InvalidOperationException if empty), and assigns the Bearer token to _http.DefaultRequestHeaders.Authorization. Note: Lacks automatic re-authentication or token refresh on 401 session expiration.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:42:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:42:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.PrintServerClient.GetPendingAsync(string, CancellationToken)
- **Lines:** [42-47]
- **Behavior:** Polls /api/print-jobs/pending?route={route} via HTTP GET using established Bearer authentication. Returns deserialized List<PrintJobDto> or an empty list if null.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:49:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:49:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.PrintServerClient.GetTicketAsync(string, CancellationToken)
- **Lines:** [49-54]
- **Behavior:** Retrieves ticket layout and content from /api/print-jobs/{jobId}/ticket via HTTP GET. Deserializes ComposedTicketDto, throwing InvalidOperationException if ticket content is empty.
- **Validations:** Validation checks detected: False. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:56:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:56:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.PrintServerClient.ReportResultAsync(Falah.Application.Printing.ReportPrintJobResultRequest, CancellationToken)
- **Lines:** [56-61]
- **Behavior:** Submits print execution outcome to /api/print-jobs/{request.PrintJobId}/report-result via HTTP POST with JSON body. Verifies response success status via EnsureSuccessStatusCode().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.PrintAgent/PrintServerClient.cs:63:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.PrintAgent/PrintServerClient.cs:63:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.PrintAgent.PrintServerClient.Dispose()
- **Lines:** [63-63]
- **Behavior:** Disposes the internal HttpClient instance, releasing network sockets and underlying HTTP connection resources.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
