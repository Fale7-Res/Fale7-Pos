# File Review: tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs.

## Callables

### Callable: tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:24:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:24:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Verification.NativeFooterLayoutProbe.Start(Window)
- **Lines:** [24-31]
- **Behavior:** Entry point initializing the native layout probe test runner. Instantiates a white Grid root host, sets it as the window content, hooks host.Loaded to initiate RunAsync once, and activates the test window.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:29:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:29:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [29-29]
- **Behavior:** Async event handler lambda ensuring RunAsync is executed exactly once upon the host Grid being loaded in the visual tree.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:33:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:33:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Verification.NativeFooterLayoutProbe.RunAsync(Window, Grid)
- **Lines:** [33-126]
- **Behavior:** Executes programmatic visual regression and layout verification across display resolutions (1920x1080, 1366x768) and DPI scales (1.0x, 1.25x, 1.5x) across multiple cart states ('empty', 'small', 'large', 'discount', 'tax', 'inclusive-tax', 'split', 'combined'). Mounts real CartPanelView instances, measures layout boundaries of RTL footer labels and totals, verifies lack of column/row overlap, ensures scroll viewer isolation from pinned footer, optionally captures PNG snapshots using RenderTargetBitmap/BitmapEncoder, writes telemetry to results.json, and closes the window.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:70:17:LocalFunctionStatement

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:70:17:LocalFunctionStatement
- **Kind:** LocalFunctionStatement
- **Symbol:** Bounds(string)
- **Lines:** [70-74]
- **Behavior:** Local helper function locating an element by XAML name on the CartPanelView and calculating its bounding rectangle (Rect) relative to the host Grid via TransformToVisual.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:130:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs:130:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Verification.NativeFooterLayoutProbe.Fixtures.SendAsync(System.Net.Http.HttpRequestMessage, CancellationToken)
- **Lines:** [130-138]
- **Behavior:** Mock HttpMessageHandler responding to GET requests for menu and tax rules without contacting real network services, throwing InvalidOperationException on non-GET calls or unexpected endpoints.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException, InvalidOperationException.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---