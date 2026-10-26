# File Review: tests/Falah.Client.Tests/NativeManagementLayoutProbe.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/NativeManagementLayoutProbe.cs.

## Callables

### Callable: tests/Falah.Client.Tests/NativeManagementLayoutProbe.cs:20:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeManagementLayoutProbe.cs:20:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Verification.NativeManagementLayoutProbe.FixtureHandler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [20-27]
- **Behavior:** Test fixture HTTP message handler serving read-only fixture JSON responses for users and permissions while rejecting non-GET write requests with an `InvalidOperationException`.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/NativeManagementLayoutProbe.cs:29:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeManagementLayoutProbe.cs:29:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Verification.NativeManagementLayoutProbe.RunAsync(Window, Grid)
- **Lines:** [29-85]
- **Behavior:** Renders management dialogs (`AttendanceView`, `UsersView`) across multiple resolutions and scaling factors to verify layout integrity, throwing exceptions if dialog components collapse, overlap, or clip outside bounds.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException, InvalidOperationException, InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/NativeManagementLayoutProbe.cs:86:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeManagementLayoutProbe.cs:86:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Verification.NativeManagementLayoutProbe.Bounds(FrameworkElement, UIElement)
- **Lines:** [86-87]
- **Behavior:** Helper method calculating the bounding rectangle of a UI element relative to another element using visual transform coordinates.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
