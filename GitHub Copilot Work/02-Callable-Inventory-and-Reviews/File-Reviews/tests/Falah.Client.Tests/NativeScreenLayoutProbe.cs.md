# File Review: tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs.

## Callables

### Callable: tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs:11:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs:11:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Verification.NativeScreenLayoutProbe.Start(Window)
- **Lines:** [11-67]
- **Behavior:** Initializes the native layout probe window host Grid and wires the async `Loaded` visual inspection lifecycle runner.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs:16:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs:16:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [16-65]
- **Behavior:** Event handler lambda for `host.Loaded` dynamically instantiating views across resolutions, measuring visual counts, recording progress logs, and serializing results to JSON.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs:27:55:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs:27:55:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [27-28]
- **Behavior:** Predicate lambda filtering assembly types to non-abstract `UserControl` classes within `.Views` namespaces, omitting placeholders.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs:32:52:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs:32:52:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [32-32]
- **Behavior:** Inline expression lambda 'foreach (var type in views.OrderBy(t => t.Name))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs:69:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeScreenLayoutProbe.cs:69:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Verification.NativeScreenLayoutProbe.CountVisuals(DependencyObject)
- **Lines:** [69-75]
- **Behavior:** Recursively traverses the visual tree using `VisualTreeHelper` to count all rendered child visual elements.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
