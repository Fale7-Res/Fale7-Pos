# File Review: src/Falah.Client.CashierWinUI/Services/ApiClient.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/Services/ApiClient.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:26:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:26:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ApiClient(System.Net.Http.HttpClient)
- **Lines:** [26-26]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.ApiClient(System.Net.Http.HttpClient) across lines 26-26.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:28:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:28:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ApiClient(string)
- **Lines:** [28-31]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.ApiClient(string) across lines 28-31.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:36:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:36:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ProductImageUri(string, string?)
- **Lines:** [36-41]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.ProductImageUri(string, string?) across lines 36-41.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:43:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:43:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.PingHealthAsync(CancellationToken)
- **Lines:** [43-54]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.PingHealthAsync(CancellationToken) across lines 43-54.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:58:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:58:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ReadResultAsync<T>(System.Net.Http.HttpResponseMessage, CancellationToken)
- **Lines:** [58-84]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.ReadResultAsync<T>(System.Net.Http.HttpResponseMessage, CancellationToken) across lines 58-84.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:92:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:92:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ReadAsync<T>(Func<Task<System.Net.Http.HttpResponseMessage>>, CancellationToken)
- **Lines:** [92-103]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.ReadAsync<T>(Func<Task<System.Net.Http.HttpResponseMessage>>, CancellationToken) across lines 92-103.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:112:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:112:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SendAsync<T>(Func<Task<System.Net.Http.HttpResponseMessage>>, CancellationToken)
- **Lines:** [112-123]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.SendAsync<T>(Func<Task<System.Net.Http.HttpResponseMessage>>, CancellationToken) across lines 112-123.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:125:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:125:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SendEmptyAsync(Func<Task<System.Net.Http.HttpResponseMessage>>, CancellationToken)
- **Lines:** [125-137]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.SendEmptyAsync(Func<Task<System.Net.Http.HttpResponseMessage>>, CancellationToken) across lines 125-137.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:139:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:139:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetCurrentShiftAsync(CancellationToken)
- **Lines:** [139-140]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetCurrentShiftAsync(CancellationToken) across lines 139-140.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:140:36:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:140:36:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [140-140]
- **Behavior:** Executes logic for lambda expression across lines 140-140.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:141:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:141:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTaxRulesAsync(CancellationToken)
- **Lines:** [141-142]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetTaxRulesAsync(CancellationToken) across lines 141-142.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:142:72:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:142:72:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [142-142]
- **Behavior:** Executes logic for lambda expression across lines 142-142.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:143:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:143:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetSettingsAsync(CancellationToken)
- **Lines:** [143-144]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetSettingsAsync(CancellationToken) across lines 143-144.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:144:59:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:144:59:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [144-144]
- **Behavior:** Executes logic for lambda expression across lines 144-144.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:145:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:145:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.LookupCustomerAsync(string, CancellationToken)
- **Lines:** [145-146]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.LookupCustomerAsync(string, CancellationToken) across lines 145-146.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:146:60:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:146:60:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [146-146]
- **Behavior:** Executes logic for lambda expression across lines 146-146.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:147:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:147:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SaveCustomerAsync(Falah.Application.Customers.UpsertCustomerRequest, CancellationToken)
- **Lines:** [147-148]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.SaveCustomerAsync(Falah.Application.Customers.UpsertCustomerRequest, CancellationToken) across lines 147-148.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:148:60:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:148:60:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [148-148]
- **Behavior:** Executes logic for lambda expression across lines 148-148.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:157:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:157:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.LoginByPinAsync(string, CancellationToken)
- **Lines:** [157-171]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.LoginByPinAsync(string, CancellationToken) across lines 157-171.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:160:13:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:160:13:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [160-160]
- **Behavior:** Executes logic for lambda expression across lines 160-160.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:175:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:175:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.LogoutAsync(CancellationToken)
- **Lines:** [175-182]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.LogoutAsync(CancellationToken) across lines 175-182.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:184:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:184:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetMenuAsync(CancellationToken)
- **Lines:** [184-187]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetMenuAsync(CancellationToken) across lines 184-187.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:186:46:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:186:46:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [186-186]
- **Behavior:** Executes logic for lambda expression across lines 186-186.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:189:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:189:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.OpenShiftAsync(Falah.Application.Shifts.OpenShiftRequest, CancellationToken)
- **Lines:** [189-192]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.OpenShiftAsync(Falah.Application.Shifts.OpenShiftRequest, CancellationToken) across lines 189-192.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:191:49:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:191:49:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [191-191]
- **Behavior:** Executes logic for lambda expression across lines 191-191.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:194:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:194:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.PrepareShiftCloseAsync(Falah.Application.Shifts.PrepareShiftCloseRequest, CancellationToken)
- **Lines:** [194-197]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.PrepareShiftCloseAsync(Falah.Application.Shifts.PrepareShiftCloseRequest, CancellationToken) across lines 194-197.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:196:57:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:196:57:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [196-196]
- **Behavior:** Executes logic for lambda expression across lines 196-196.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:199:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:199:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CloseShiftAsync(Falah.Application.Shifts.CloseShiftRequest, CancellationToken)
- **Lines:** [199-202]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CloseShiftAsync(Falah.Application.Shifts.CloseShiftRequest, CancellationToken) across lines 199-202.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:201:50:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:201:50:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [201-201]
- **Behavior:** Executes logic for lambda expression across lines 201-201.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:204:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:204:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken)
- **Lines:** [204-207]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) across lines 204-207.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:206:53:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:206:53:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [206-206]
- **Behavior:** Executes logic for lambda expression across lines 206-206.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:209:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:209:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RefundAsync(Falah.Application.Orders.RefundOrderRequest, CancellationToken)
- **Lines:** [209-212]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.RefundAsync(Falah.Application.Orders.RefundOrderRequest, CancellationToken) across lines 209-212.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:211:51:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:211:51:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [211-211]
- **Behavior:** Executes logic for lambda expression across lines 211-211.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:214:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:214:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ConfirmElectronicPaymentAsync(Falah.Application.Orders.ConfirmElectronicPaymentRequest, CancellationToken)
- **Lines:** [214-217]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.ConfirmElectronicPaymentAsync(Falah.Application.Orders.ConfirmElectronicPaymentRequest, CancellationToken) across lines 214-217.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:216:64:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:216:64:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [216-216]
- **Behavior:** Executes logic for lambda expression across lines 216-216.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:219:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:219:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CancelPendingElectronicPaymentAsync(Falah.Application.Orders.CancelPendingElectronicPaymentRequest, CancellationToken)
- **Lines:** [219-222]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CancelPendingElectronicPaymentAsync(Falah.Application.Orders.CancelPendingElectronicPaymentRequest, CancellationToken) across lines 219-222.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:221:70:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:221:70:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [221-221]
- **Behavior:** Executes logic for lambda expression across lines 221-221.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:224:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:224:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetActiveDeliveryServicesAsync(CancellationToken)
- **Lines:** [224-227]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetActiveDeliveryServicesAsync(CancellationToken) across lines 224-227.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:226:67:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:226:67:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [226-226]
- **Behavior:** Executes logic for lambda expression across lines 226-226.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:231:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:231:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetOrdersAsync(string, CancellationToken)
- **Lines:** [231-233]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetOrdersAsync(string, CancellationToken) across lines 231-233.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:232:51:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:232:51:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [232-233]
- **Behavior:** Executes logic for lambda expression across lines 232-233.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:237:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:237:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPriceRulesAsync(CancellationToken)
- **Lines:** [237-238]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetPriceRulesAsync(CancellationToken) across lines 237-238.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:238:74:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:238:74:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [238-238]
- **Behavior:** Executes logic for lambda expression across lines 238-238.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:240:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:240:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SetProductAvailabilityAsync(Falah.Application.Menu.SetProductAvailabilityRequest, CancellationToken)
- **Lines:** [240-242]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.SetProductAvailabilityAsync(Falah.Application.Menu.SetProductAvailabilityRequest, CancellationToken) across lines 240-242.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:241:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:241:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [241-242]
- **Behavior:** Executes logic for lambda expression across lines 241-242.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:246:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:246:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.UpdateSettingsAsync(Falah.Application.Settings.UpdateSettingsRequest, CancellationToken)
- **Lines:** [246-247]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.UpdateSettingsAsync(Falah.Application.Settings.UpdateSettingsRequest, CancellationToken) across lines 246-247.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:247:59:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:247:59:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [247-247]
- **Behavior:** Executes logic for lambda expression across lines 247-247.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:249:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:249:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetBackupsAsync(CancellationToken)
- **Lines:** [249-250]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetBackupsAsync(CancellationToken) across lines 249-250.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:250:76:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:250:76:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [250-250]
- **Behavior:** Executes logic for lambda expression across lines 250-250.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:252:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:252:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateBackupAsync(Falah.Application.Backup.CreateBackupRequest, CancellationToken)
- **Lines:** [252-253]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CreateBackupAsync(Falah.Application.Backup.CreateBackupRequest, CancellationToken) across lines 252-253.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:253:61:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:253:61:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [253-253]
- **Behavior:** Executes logic for lambda expression across lines 253-253.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:255:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:255:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.VerifyBackupAsync(Falah.Application.Backup.VerifyBackupRequest, CancellationToken)
- **Lines:** [255-257]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.VerifyBackupAsync(Falah.Application.Backup.VerifyBackupRequest, CancellationToken) across lines 255-257.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:256:61:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:256:61:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [256-257]
- **Behavior:** Executes logic for lambda expression across lines 256-257.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:261:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:261:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetCurrentBusinessDayAsync(CancellationToken)
- **Lines:** [261-262]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetCurrentBusinessDayAsync(CancellationToken) across lines 261-262.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:262:65:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:262:65:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [262-262]
- **Behavior:** Executes logic for lambda expression across lines 262-262.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:264:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:264:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetBusinessDayAsync(string, CancellationToken)
- **Lines:** [264-265]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetBusinessDayAsync(string, CancellationToken) across lines 264-265.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:265:65:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:265:65:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [265-265]
- **Behavior:** Executes logic for lambda expression across lines 265-265.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:267:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:267:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetCloseBlockersAsync(string, CancellationToken)
- **Lines:** [267-269]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetCloseBlockersAsync(string, CancellationToken) across lines 267-269.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:268:81:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:268:81:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [268-269]
- **Behavior:** Executes logic for lambda expression across lines 268-269.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:271:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:271:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetChecklistAsync(string, CancellationToken)
- **Lines:** [271-273]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetChecklistAsync(string, CancellationToken) across lines 271-273.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:272:88:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:272:88:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [272-273]
- **Behavior:** Executes logic for lambda expression across lines 272-273.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:275:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:275:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CompleteChecklistItemAsync(Falah.Application.BusinessDay.CompleteChecklistItemRequest, CancellationToken)
- **Lines:** [275-277]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CompleteChecklistItemAsync(Falah.Application.BusinessDay.CompleteChecklistItemRequest, CancellationToken) across lines 275-277.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:276:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:276:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [276-277]
- **Behavior:** Executes logic for lambda expression across lines 276-277.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:279:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:279:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetExceptionsAsync(string, CancellationToken)
- **Lines:** [279-281]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetExceptionsAsync(string, CancellationToken) across lines 279-281.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:280:85:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:280:85:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [280-281]
- **Behavior:** Executes logic for lambda expression across lines 280-281.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:283:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:283:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetBusinessDaysAsync(CancellationToken)
- **Lines:** [283-284]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetBusinessDaysAsync(CancellationToken) across lines 283-284.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:284:80:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:284:80:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [284-284]
- **Behavior:** Executes logic for lambda expression across lines 284-284.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:286:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:286:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.OpenBusinessDayAsync(Falah.Application.BusinessDay.OpenBusinessDayRequest, CancellationToken)
- **Lines:** [286-287]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.OpenBusinessDayAsync(Falah.Application.BusinessDay.OpenBusinessDayRequest, CancellationToken) across lines 286-287.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:287:72:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:287:72:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [287-287]
- **Behavior:** Executes logic for lambda expression across lines 287-287.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:289:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:289:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CloseBusinessDayAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken)
- **Lines:** [289-291]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CloseBusinessDayAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) across lines 289-291.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:290:78:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:290:78:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [290-291]
- **Behavior:** Executes logic for lambda expression across lines 290-291.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:293:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:293:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ReopenBusinessDayAsync(Falah.Application.BusinessDay.ReopenBusinessDayRequest, CancellationToken)
- **Lines:** [293-295]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.ReopenBusinessDayAsync(Falah.Application.BusinessDay.ReopenBusinessDayRequest, CancellationToken) across lines 293-295.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:294:65:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:294:65:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [294-295]
- **Behavior:** Executes logic for lambda expression across lines 294-295.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:297:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:297:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SetExceptionStatusAsync(Falah.Application.BusinessDay.SetExceptionStatusRequest, CancellationToken)
- **Lines:** [297-299]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.SetExceptionStatusAsync(Falah.Application.BusinessDay.SetExceptionStatusRequest, CancellationToken) across lines 297-299.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:298:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:298:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [298-299]
- **Behavior:** Executes logic for lambda expression across lines 298-299.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:301:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:301:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetBusinessDayReconciliationAsync(string, string, CancellationToken)
- **Lines:** [301-303]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetBusinessDayReconciliationAsync(string, string, CancellationToken) across lines 301-303.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:302:75:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:302:75:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [302-303]
- **Behavior:** Executes logic for lambda expression across lines 302-303.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:305:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:305:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDailySummaryAsync(string, string, CancellationToken)
- **Lines:** [305-307]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetDailySummaryAsync(string, string, CancellationToken) across lines 305-307.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:306:62:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:306:62:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [306-307]
- **Behavior:** Executes logic for lambda expression across lines 306-307.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:311:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:311:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDailyStatementAsync(string, DateOnly?, CancellationToken)
- **Lines:** [311-315]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetDailyStatementAsync(string, DateOnly?, CancellationToken) across lines 311-315.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:313:64:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:313:64:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [313-315]
- **Behavior:** Executes logic for lambda expression across lines 313-315.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:317:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:317:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetLatestCloseSnapshotAsync(string, CancellationToken)
- **Lines:** [317-319]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetLatestCloseSnapshotAsync(string, CancellationToken) across lines 317-319.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:318:78:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:318:78:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [318-319]
- **Behavior:** Executes logic for lambda expression across lines 318-319.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:321:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:321:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetCloseSnapshotsAsync(string, CancellationToken)
- **Lines:** [321-323]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetCloseSnapshotsAsync(string, CancellationToken) across lines 321-323.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:322:93:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:322:93:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [322-323]
- **Behavior:** Executes logic for lambda expression across lines 322-323.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:325:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:325:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPrintingOverviewAsync(CancellationToken)
- **Lines:** [325-326]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetPrintingOverviewAsync(CancellationToken) across lines 325-326.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:326:40:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:326:40:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [326-326]
- **Behavior:** Executes logic for lambda expression across lines 326-326.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:328:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:328:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPendingPrintJobsAsync(string, CancellationToken)
- **Lines:** [328-330]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetPendingPrintJobsAsync(string, CancellationToken) across lines 328-330.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:329:47:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:329:47:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [329-330]
- **Behavior:** Executes logic for lambda expression across lines 329-330.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:334:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:334:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateUserAsync(Falah.Application.Users.CreateUserRequest, CancellationToken)
- **Lines:** [334-335]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CreateUserAsync(Falah.Application.Users.CreateUserRequest, CancellationToken) across lines 334-335.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:335:37:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:335:37:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [335-335]
- **Behavior:** Executes logic for lambda expression across lines 335-335.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:337:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:337:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ChangePinAsync(Falah.Application.Users.ChangePinRequest, CancellationToken)
- **Lines:** [337-339]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.ChangePinAsync(Falah.Application.Users.ChangePinRequest, CancellationToken) across lines 337-339.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:338:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:338:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [338-339]
- **Behavior:** Executes logic for lambda expression across lines 338-339.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:341:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:341:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetAllUsersAsync(string, CancellationToken)
- **Lines:** [341-343]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetAllUsersAsync(string, CancellationToken) across lines 341-343.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:342:50:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:342:50:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [342-343]
- **Behavior:** Executes logic for lambda expression across lines 342-343.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:345:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:345:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPermissionCatalogAsync(CancellationToken)
- **Lines:** [345-346]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetPermissionCatalogAsync(CancellationToken) across lines 345-346.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:346:83:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:346:83:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [346-346]
- **Behavior:** Executes logic for lambda expression across lines 346-346.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:348:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:348:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SetUserActiveAsync(Falah.Application.Users.SetUserActiveRequest, CancellationToken)
- **Lines:** [348-350]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.SetUserActiveAsync(Falah.Application.Users.SetUserActiveRequest, CancellationToken) across lines 348-350.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:349:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:349:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [349-350]
- **Behavior:** Executes logic for lambda expression across lines 349-350.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:352:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:352:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.UpdateUserPermissionsAsync(Falah.Application.Users.UpdateUserPermissionsRequest, CancellationToken)
- **Lines:** [352-354]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.UpdateUserPermissionsAsync(Falah.Application.Users.UpdateUserPermissionsRequest, CancellationToken) across lines 352-354.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:353:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:353:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [353-354]
- **Behavior:** Executes logic for lambda expression across lines 353-354.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:356:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:356:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetAuditEventsAsync(string, string?, string?, int, CancellationToken)
- **Lines:** [356-359]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetAuditEventsAsync(string, string?, string?, int, CancellationToken) across lines 356-359.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:358:73:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:358:73:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [358-359]
- **Behavior:** Executes logic for lambda expression across lines 358-359.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:363:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:363:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetEmployeesAsync(CancellationToken)
- **Lines:** [363-364]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetEmployeesAsync(CancellationToken) across lines 363-364.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:364:75:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:364:75:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [364-364]
- **Behavior:** Executes logic for lambda expression across lines 364-364.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:368:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:368:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetEmployeeBalanceAsync(string, CancellationToken)
- **Lines:** [368-369]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetEmployeeBalanceAsync(string, CancellationToken) across lines 368-369.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:369:39:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:369:39:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [369-369]
- **Behavior:** Executes logic for lambda expression across lines 369-369.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:371:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:371:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetEmployeeLedgerAsync(string, CancellationToken)
- **Lines:** [371-373]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetEmployeeLedgerAsync(string, CancellationToken) across lines 371-373.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:372:86:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:372:86:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [372-373]
- **Behavior:** Executes logic for lambda expression across lines 372-373.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:375:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:375:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.EmployeeCashOutAsync(Falah.Application.Employees.RecordEmployeeCashOutRequest, CancellationToken)
- **Lines:** [375-377]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.EmployeeCashOutAsync(Falah.Application.Employees.RecordEmployeeCashOutRequest, CancellationToken) across lines 375-377.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:376:70:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:376:70:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [376-377]
- **Behavior:** Executes logic for lambda expression across lines 376-377.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:381:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:381:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetEmployeeFinancialReportAsync(string, DateOnly, DateOnly, CancellationToken)
- **Lines:** [381-385]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetEmployeeFinancialReportAsync(string, DateOnly, DateOnly, CancellationToken) across lines 381-385.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:383:94:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:383:94:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [383-385]
- **Behavior:** Executes logic for lambda expression across lines 383-385.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:390:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:390:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.PayEmployeeAsync(Falah.Application.Employees.PayEmployeeRequest, CancellationToken)
- **Lines:** [390-392]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.PayEmployeeAsync(Falah.Application.Employees.PayEmployeeRequest, CancellationToken) across lines 390-392.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:391:66:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:391:66:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [391-392]
- **Behavior:** Executes logic for lambda expression across lines 391-392.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:394:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:394:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RecordDailyWageAsync(Falah.Application.Employees.RecordDailyWageRequest, CancellationToken)
- **Lines:** [394-396]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.RecordDailyWageAsync(Falah.Application.Employees.RecordDailyWageRequest, CancellationToken) across lines 394-396.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:395:71:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:395:71:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [395-396]
- **Behavior:** Executes logic for lambda expression across lines 395-396.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:398:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:398:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RecordOvertimeAsync(Falah.Application.Employees.RecordOvertimeRequest, CancellationToken)
- **Lines:** [398-400]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.RecordOvertimeAsync(Falah.Application.Employees.RecordOvertimeRequest, CancellationToken) across lines 398-400.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:399:71:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:399:71:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [399-400]
- **Behavior:** Executes logic for lambda expression across lines 399-400.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:402:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:402:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RecordExtraShiftAsync(Falah.Application.Employees.RecordExtraShiftRequest, CancellationToken)
- **Lines:** [402-404]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.RecordExtraShiftAsync(Falah.Application.Employees.RecordExtraShiftRequest, CancellationToken) across lines 402-404.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:403:71:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:403:71:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [403-404]
- **Behavior:** Executes logic for lambda expression across lines 403-404.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:406:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:406:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetAttendanceAsync(DateOnly?, string?, CancellationToken)
- **Lines:** [406-408]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetAttendanceAsync(DateOnly?, string?, CancellationToken) across lines 406-408.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:407:83:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:407:83:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [407-408]
- **Behavior:** Executes logic for lambda expression across lines 407-408.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:410:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:410:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CorrectAttendanceAsync(Falah.Application.Employees.CorrectAttendanceRequest, CancellationToken)
- **Lines:** [410-412]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CorrectAttendanceAsync(Falah.Application.Employees.CorrectAttendanceRequest, CancellationToken) across lines 410-412.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:411:68:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:411:68:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [411-412]
- **Behavior:** Executes logic for lambda expression across lines 411-412.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:414:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:414:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPayrollRunsAsync(CancellationToken)
- **Lines:** [414-415]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetPayrollRunsAsync(CancellationToken) across lines 414-415.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:415:77:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:415:77:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [415-415]
- **Behavior:** Executes logic for lambda expression across lines 415-415.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:417:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:417:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPayrollLinesAsync(string, CancellationToken)
- **Lines:** [417-419]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetPayrollLinesAsync(string, CancellationToken) across lines 417-419.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:418:78:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:418:78:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [418-419]
- **Behavior:** Executes logic for lambda expression across lines 418-419.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:421:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:421:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreatePayrollRunAsync(Falah.Application.Employees.CreatePayrollRequest, CancellationToken)
- **Lines:** [421-422]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CreatePayrollRunAsync(Falah.Application.Employees.CreatePayrollRequest, CancellationToken) across lines 421-422.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:422:62:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:422:62:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [422-422]
- **Behavior:** Executes logic for lambda expression across lines 422-422.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:424:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:424:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.TransitionPayrollAsync(Falah.Application.Employees.TransitionPayrollRequest, CancellationToken)
- **Lines:** [424-426]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.TransitionPayrollAsync(Falah.Application.Employees.TransitionPayrollRequest, CancellationToken) across lines 424-426.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:425:62:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:425:62:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [425-426]
- **Behavior:** Executes logic for lambda expression across lines 425-426.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:428:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:428:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPayrollRunAsync(string, CancellationToken)
- **Lines:** [428-430]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetPayrollRunAsync(string, CancellationToken) across lines 428-430.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:429:62:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:429:62:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [429-430]
- **Behavior:** Executes logic for lambda expression across lines 429-430.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:432:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:432:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.PayPayrollAsync(Falah.Application.Employees.PayPayrollRequest, CancellationToken)
- **Lines:** [432-434]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.PayPayrollAsync(Falah.Application.Employees.PayPayrollRequest, CancellationToken) across lines 432-434.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:433:65:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:433:65:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [433-434]
- **Behavior:** Executes logic for lambda expression across lines 433-434.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:438:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:438:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRecipesAsync(CancellationToken)
- **Lines:** [438-439]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetRecipesAsync(CancellationToken) across lines 438-439.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:439:73:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:439:73:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [439-439]
- **Behavior:** Executes logic for lambda expression across lines 439-439.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:441:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:441:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeCostAsync(string, string, CancellationToken)
- **Lines:** [441-443]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeCostAsync(string, string, CancellationToken) across lines 441-443.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:442:62:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:442:62:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [442-443]
- **Behavior:** Executes logic for lambda expression across lines 442-443.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:445:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:445:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeProfitabilityAsync(string, long, string, CancellationToken)
- **Lines:** [445-447]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeProfitabilityAsync(string, long, string, CancellationToken) across lines 445-447.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:446:73:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:446:73:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [446-447]
- **Behavior:** Executes logic for lambda expression across lines 446-447.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:452:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:452:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeCostSummariesAsync(string, CancellationToken)
- **Lines:** [452-454]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeCostSummariesAsync(string, CancellationToken) across lines 452-454.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:453:84:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:453:84:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [453-454]
- **Behavior:** Executes logic for lambda expression across lines 453-454.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:456:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:456:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeCostCoverageAsync(string, CancellationToken)
- **Lines:** [456-458]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeCostCoverageAsync(string, CancellationToken) across lines 456-458.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:457:70:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:457:70:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [457-458]
- **Behavior:** Executes logic for lambda expression across lines 457-458.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:460:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:460:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeCostDetailAsync(string, string, CancellationToken)
- **Lines:** [460-462]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeCostDetailAsync(string, string, CancellationToken) across lines 460-462.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:461:68:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:461:68:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [461-462]
- **Behavior:** Executes logic for lambda expression across lines 461-462.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:464:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:464:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SaveRecipeAsync(Falah.Application.Inventory.SaveRecipeRequest, CancellationToken)
- **Lines:** [464-465]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.SaveRecipeAsync(Falah.Application.Inventory.SaveRecipeRequest, CancellationToken) across lines 464-465.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:465:58:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:465:58:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [465-465]
- **Behavior:** Executes logic for lambda expression across lines 465-465.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:467:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:467:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetLossSummaryAsync(string, DateOnly, CancellationToken)
- **Lines:** [467-469]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetLossSummaryAsync(string, DateOnly, CancellationToken) across lines 467-469.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:468:63:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:468:63:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [468-469]
- **Behavior:** Executes logic for lambda expression across lines 468-469.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:473:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:473:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetSuppliersAsync(CancellationToken)
- **Lines:** [473-474]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetSuppliersAsync(CancellationToken) across lines 473-474.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:474:76:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:474:76:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [474-474]
- **Behavior:** Executes logic for lambda expression across lines 474-474.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:476:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:476:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.AddSupplierAsync(Falah.Application.Purchasing.AddSupplierRequest, CancellationToken)
- **Lines:** [476-477]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.AddSupplierAsync(Falah.Application.Purchasing.AddSupplierRequest, CancellationToken) across lines 476-477.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:477:67:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:477:67:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [477-477]
- **Behavior:** Executes logic for lambda expression across lines 477-477.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:479:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:479:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetSupplierLedgerAsync(string, CancellationToken)
- **Lines:** [479-481]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetSupplierLedgerAsync(string, CancellationToken) across lines 479-481.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:480:87:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:480:87:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [480-481]
- **Behavior:** Executes logic for lambda expression across lines 480-481.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:483:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:483:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPurchasesAsync(CancellationToken)
- **Lines:** [483-484]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetPurchasesAsync(CancellationToken) across lines 483-484.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:484:76:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:484:76:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [484-484]
- **Behavior:** Executes logic for lambda expression across lines 484-484.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:486:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:486:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ReceivePurchaseAsync(Falah.Application.Purchasing.ReceivePurchaseRequest, CancellationToken)
- **Lines:** [486-487]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.ReceivePurchaseAsync(Falah.Application.Purchasing.ReceivePurchaseRequest, CancellationToken) across lines 486-487.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:487:61:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:487:61:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [487-487]
- **Behavior:** Executes logic for lambda expression across lines 487-487.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:489:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:489:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.PayPurchaseAsync(Falah.Application.Purchasing.PayPurchaseRequest, CancellationToken)
- **Lines:** [489-491]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.PayPurchaseAsync(Falah.Application.Purchasing.PayPurchaseRequest, CancellationToken) across lines 489-491.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:490:61:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:490:61:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [490-491]
- **Behavior:** Executes logic for lambda expression across lines 490-491.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:493:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:493:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPayablesAgingAsync(CancellationToken)
- **Lines:** [493-494]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetPayablesAgingAsync(CancellationToken) across lines 493-494.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:494:66:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:494:66:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [494-494]
- **Behavior:** Executes logic for lambda expression across lines 494-494.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:496:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:496:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SetPurchaseDueDateAsync(Falah.Application.Purchasing.SetPurchaseDueDateRequest, CancellationToken)
- **Lines:** [496-498]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.SetPurchaseDueDateAsync(Falah.Application.Purchasing.SetPurchaseDueDateRequest, CancellationToken) across lines 496-498.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:497:61:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:497:61:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [497-498]
- **Behavior:** Executes logic for lambda expression across lines 497-498.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:502:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:502:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTreasuryPositionAsync(string, CancellationToken)
- **Lines:** [502-504]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetTreasuryPositionAsync(string, CancellationToken) across lines 502-504.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:503:40:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:503:40:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [503-504]
- **Behavior:** Executes logic for lambda expression across lines 503-504.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:506:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:506:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTreasuryBalancesAsync(string, CancellationToken)
- **Lines:** [506-508]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetTreasuryBalancesAsync(string, CancellationToken) across lines 506-508.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:507:61:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:507:61:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [507-508]
- **Behavior:** Executes logic for lambda expression across lines 507-508.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:510:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:510:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTreasuryMovementsAsync(string, string?, int, CancellationToken)
- **Lines:** [510-512]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetTreasuryMovementsAsync(string, string?, int, CancellationToken) across lines 510-512.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:511:55:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:511:55:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [511-512]
- **Behavior:** Executes logic for lambda expression across lines 511-512.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:514:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:514:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.TreasuryDepositAsync(Falah.Application.Treasury.DepositRequest, CancellationToken)
- **Lines:** [514-515]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.TreasuryDepositAsync(Falah.Application.Treasury.DepositRequest, CancellationToken) across lines 514-515.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:515:34:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:515:34:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [515-515]
- **Behavior:** Executes logic for lambda expression across lines 515-515.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:517:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:517:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.TreasuryWithdrawAsync(Falah.Application.Treasury.WithdrawalRequest, CancellationToken)
- **Lines:** [517-518]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.TreasuryWithdrawAsync(Falah.Application.Treasury.WithdrawalRequest, CancellationToken) across lines 517-518.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:518:37:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:518:37:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [518-518]
- **Behavior:** Executes logic for lambda expression across lines 518-518.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:520:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:520:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.TreasuryTransferAsync(Falah.Application.Treasury.TransferRequest, CancellationToken)
- **Lines:** [520-521]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.TreasuryTransferAsync(Falah.Application.Treasury.TransferRequest, CancellationToken) across lines 520-521.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:521:35:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:521:35:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [521-521]
- **Behavior:** Executes logic for lambda expression across lines 521-521.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:525:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:525:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetInventoryItemsAsync(CancellationToken)
- **Lines:** [525-526]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetInventoryItemsAsync(CancellationToken) across lines 525-526.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:526:80:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:526:80:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [526-526]
- **Behavior:** Executes logic for lambda expression across lines 526-526.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:528:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:528:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateInventoryItemAsync(Falah.Application.Inventory.CreateInventoryItemRequest, CancellationToken)
- **Lines:** [528-529]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CreateInventoryItemAsync(Falah.Application.Inventory.CreateInventoryItemRequest, CancellationToken) across lines 528-529.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:529:74:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:529:74:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [529-529]
- **Behavior:** Executes logic for lambda expression across lines 529-529.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:531:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:531:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.UpdateInventoryItemAsync(Falah.Application.Inventory.UpdateInventoryItemRequest, CancellationToken)
- **Lines:** [531-532]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.UpdateInventoryItemAsync(Falah.Application.Inventory.UpdateInventoryItemRequest, CancellationToken) across lines 531-532.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:532:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:532:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [532-532]
- **Behavior:** Executes logic for lambda expression across lines 532-532.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:534:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:534:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetInventoryOverviewAsync(string, CancellationToken)
- **Lines:** [534-536]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetInventoryOverviewAsync(string, CancellationToken) across lines 534-536.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:535:69:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:535:69:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [535-536]
- **Behavior:** Executes logic for lambda expression across lines 535-536.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:538:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:538:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetInventoryItemDetailAsync(string, string, CancellationToken)
- **Lines:** [538-540]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetInventoryItemDetailAsync(string, string, CancellationToken) across lines 538-540.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:539:71:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:539:71:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [539-540]
- **Behavior:** Executes logic for lambda expression across lines 539-540.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:542:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:542:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RecordCountAdjustmentAsync(Falah.Application.Inventory.RecordCountAdjustmentRequest, CancellationToken)
- **Lines:** [542-543]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.RecordCountAdjustmentAsync(Falah.Application.Inventory.RecordCountAdjustmentRequest, CancellationToken) across lines 542-543.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:543:76:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:543:76:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [543-543]
- **Behavior:** Executes logic for lambda expression across lines 543-543.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:545:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:545:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RecordWasteAsync(Falah.Application.Inventory.RecordWasteRequest, CancellationToken)
- **Lines:** [545-546]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.RecordWasteAsync(Falah.Application.Inventory.RecordWasteRequest, CancellationToken) across lines 545-546.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:546:66:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:546:66:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [546-546]
- **Behavior:** Executes logic for lambda expression across lines 546-546.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:548:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:548:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetStockMovementsAsync(string?, int, CancellationToken)
- **Lines:** [548-550]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetStockMovementsAsync(string?, int, CancellationToken) across lines 548-550.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:549:80:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:549:80:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [549-550]
- **Behavior:** Executes logic for lambda expression across lines 549-550.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:557:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:557:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.Day(DateOnly?)
- **Lines:** [557-557]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.Day(DateOnly?) across lines 557-557.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:566:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:566:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetSalesReportAsync(string, DateOnly?, DateOnly?, string?, CancellationToken)
- **Lines:** [566-569]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetSalesReportAsync(string, DateOnly?, DateOnly?, string?, CancellationToken) across lines 566-569.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:567:61:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:567:61:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [567-569]
- **Behavior:** Executes logic for lambda expression across lines 567-569.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:571:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:571:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetExpenseReportAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [571-573]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetExpenseReportAsync(string, DateOnly?, DateOnly?, CancellationToken) across lines 571-573.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:572:63:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:572:63:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [572-573]
- **Behavior:** Executes logic for lambda expression across lines 572-573.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:575:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:575:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTaxReportAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [575-577]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetTaxReportAsync(string, DateOnly?, DateOnly?, CancellationToken) across lines 575-577.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:576:59:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:576:59:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [576-577]
- **Behavior:** Executes logic for lambda expression across lines 576-577.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:579:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:579:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTopItemsAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [579-581]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetTopItemsAsync(string, DateOnly?, DateOnly?, CancellationToken) across lines 579-581.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:580:79:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:580:79:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [580-581]
- **Behavior:** Executes logic for lambda expression across lines 580-581.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:583:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:583:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetProductHourlySalesAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [583-585]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetProductHourlySalesAsync(string, DateOnly?, DateOnly?, CancellationToken) across lines 583-585.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:584:83:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:584:83:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [584-585]
- **Behavior:** Executes logic for lambda expression across lines 584-585.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:587:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:587:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetCategorySalesAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [587-589]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetCategorySalesAsync(string, DateOnly?, DateOnly?, CancellationToken) across lines 587-589.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:588:78:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:588:78:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [588-589]
- **Behavior:** Executes logic for lambda expression across lines 588-589.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:591:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:591:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDeliveryOutcomesAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [591-593]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetDeliveryOutcomesAsync(string, DateOnly?, DateOnly?, CancellationToken) across lines 591-593.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:592:72:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:592:72:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [592-593]
- **Behavior:** Executes logic for lambda expression across lines 592-593.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:595:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:595:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetSignificantActionsAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [595-597]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetSignificantActionsAsync(string, DateOnly?, DateOnly?, CancellationToken) across lines 595-597.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:596:82:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:596:82:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [596-597]
- **Behavior:** Executes logic for lambda expression across lines 596-597.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:599:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:599:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetShiftsHistoryAsync(string, CancellationToken)
- **Lines:** [599-601]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetShiftsHistoryAsync(string, CancellationToken) across lines 599-601.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:600:77:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:600:77:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [600-601]
- **Behavior:** Executes logic for lambda expression across lines 600-601.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:603:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:603:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetShiftSummaryAsync(string, string, CancellationToken)
- **Lines:** [603-605]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetShiftSummaryAsync(string, string, CancellationToken) across lines 603-605.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:604:62:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:604:62:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [604-605]
- **Behavior:** Executes logic for lambda expression across lines 604-605.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:607:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:607:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRefundActivityAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [607-609]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetRefundActivityAsync(string, DateOnly?, DateOnly?, CancellationToken) across lines 607-609.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:608:79:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:608:79:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [608-609]
- **Behavior:** Executes logic for lambda expression across lines 608-609.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:611:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:611:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetFoodCostReportAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [611-613]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetFoodCostReportAsync(string, DateOnly?, DateOnly?, CancellationToken) across lines 611-613.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:612:64:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:612:64:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [612-613]
- **Behavior:** Executes logic for lambda expression across lines 612-613.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:617:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:617:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetActiveDriversAsync(CancellationToken)
- **Lines:** [617-618]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetActiveDriversAsync(CancellationToken) across lines 617-618.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:618:45:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:618:45:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [618-618]
- **Behavior:** Executes logic for lambda expression across lines 618-618.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:620:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:620:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetAllDriversAsync(CancellationToken)
- **Lines:** [620-621]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetAllDriversAsync(CancellationToken) across lines 620-621.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:621:45:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:621:45:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [621-621]
- **Behavior:** Executes logic for lambda expression across lines 621-621.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:623:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:623:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateDriverAsync(Falah.Application.Delivery.CreateDriverRequest, CancellationToken)
- **Lines:** [623-624]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CreateDriverAsync(Falah.Application.Delivery.CreateDriverRequest, CancellationToken) across lines 623-624.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:624:30:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:624:30:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [624-624]
- **Behavior:** Executes logic for lambda expression across lines 624-624.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:626:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:626:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SetDriverActiveAsync(Falah.Application.Delivery.SetDriverActiveRequest, CancellationToken)
- **Lines:** [626-627]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.SetDriverActiveAsync(Falah.Application.Delivery.SetDriverActiveRequest, CancellationToken) across lines 626-627.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:627:30:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:627:30:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [627-627]
- **Behavior:** Executes logic for lambda expression across lines 627-627.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:631:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:631:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDispatchBoardAsync(CancellationToken)
- **Lines:** [631-632]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetDispatchBoardAsync(CancellationToken) across lines 631-632.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:632:47:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:632:47:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [632-632]
- **Behavior:** Executes logic for lambda expression across lines 632-632.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:634:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:634:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.MarkDeliveryReadyAsync(Falah.Application.Delivery.MarkDeliveryReadyRequest, CancellationToken)
- **Lines:** [634-635]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.MarkDeliveryReadyAsync(Falah.Application.Delivery.MarkDeliveryReadyRequest, CancellationToken) across lines 634-635.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:635:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:635:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [635-635]
- **Behavior:** Executes logic for lambda expression across lines 635-635.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:637:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:637:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.AssignDeliveryAsync(Falah.Application.Delivery.AssignDeliveryRequest, CancellationToken)
- **Lines:** [637-638]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.AssignDeliveryAsync(Falah.Application.Delivery.AssignDeliveryRequest, CancellationToken) across lines 637-638.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:638:41:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:638:41:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [638-638]
- **Behavior:** Executes logic for lambda expression across lines 638-638.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:640:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:640:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ReassignDeliveryAsync(Falah.Application.Delivery.ReassignDeliveryRequest, CancellationToken)
- **Lines:** [640-641]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.ReassignDeliveryAsync(Falah.Application.Delivery.ReassignDeliveryRequest, CancellationToken) across lines 640-641.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:641:43:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:641:43:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [641-641]
- **Behavior:** Executes logic for lambda expression across lines 641-641.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:643:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:643:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RecordDeliveryResultAsync(Falah.Application.Delivery.RecordDeliveryResultRequest, CancellationToken)
- **Lines:** [643-644]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.RecordDeliveryResultAsync(Falah.Application.Delivery.RecordDeliveryResultRequest, CancellationToken) across lines 643-644.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:644:47:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:644:47:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [644-644]
- **Behavior:** Executes logic for lambda expression across lines 644-644.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:646:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:646:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SettleDriverCustodyAsync(Falah.Application.Delivery.SettleDriverCustodyRequest, CancellationToken)
- **Lines:** [646-647]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.SettleDriverCustodyAsync(Falah.Application.Delivery.SettleDriverCustodyRequest, CancellationToken) across lines 646-647.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:647:46:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:647:46:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [647-647]
- **Behavior:** Executes logic for lambda expression across lines 647-647.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:649:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:649:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDriverAccountAsync(string, CancellationToken)
- **Lines:** [649-650]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetDriverAccountAsync(string, CancellationToken) across lines 649-650.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:650:37:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:650:37:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [650-650]
- **Behavior:** Executes logic for lambda expression across lines 650-650.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:656:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:656:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDriverStatisticsAsync(string, DateTimeOffset, DateTimeOffset, CancellationToken)
- **Lines:** [656-660]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetDriverStatisticsAsync(string, DateTimeOffset, DateTimeOffset, CancellationToken) across lines 656-660.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:658:40:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:658:40:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [658-660]
- **Behavior:** Executes logic for lambda expression across lines 658-660.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:662:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:662:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDeliveryTimelineAsync(string, CancellationToken)
- **Lines:** [662-663]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetDeliveryTimelineAsync(string, CancellationToken) across lines 662-663.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:663:52:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:663:52:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [663-663]
- **Behavior:** Executes logic for lambda expression across lines 663-663.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:665:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:665:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.LookupOrderByNumberAsync(string, CancellationToken)
- **Lines:** [665-668]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.LookupOrderByNumberAsync(string, CancellationToken) across lines 665-668.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:667:49:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:667:49:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [667-667]
- **Behavior:** Executes logic for lambda expression across lines 667-667.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:670:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:670:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetOrderAsync(string, CancellationToken)
- **Lines:** [670-673]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetOrderAsync(string, CancellationToken) across lines 670-673.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:672:49:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:672:49:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [672-672]
- **Behavior:** Executes logic for lambda expression across lines 672-672.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:675:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:675:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.IssueManagerApprovalAsync(Falah.Application.Approvals.IssueManagerApprovalRequest, CancellationToken)
- **Lines:** [675-678]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.IssueManagerApprovalAsync(Falah.Application.Approvals.IssueManagerApprovalRequest, CancellationToken) across lines 675-678.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:677:60:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:677:60:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [677-677]
- **Behavior:** Executes logic for lambda expression across lines 677-677.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:680:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:680:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.AddExpenseAsync(Falah.Application.Expenses.AddExpenseRequest, CancellationToken)
- **Lines:** [680-683]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.AddExpenseAsync(Falah.Application.Expenses.AddExpenseRequest, CancellationToken) across lines 680-683.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:682:50:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:682:50:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [682-682]
- **Behavior:** Executes logic for lambda expression across lines 682-682.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:687:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:687:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SafeDropAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken)
- **Lines:** [687-690]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.SafeDropAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken) across lines 687-690.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:689:48:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:689:48:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [689-689]
- **Behavior:** Executes logic for lambda expression across lines 689-689.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:692:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:692:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CashierCashMovementAsync(Falah.Application.CashDrawer.CashierCashMovementRequest, CancellationToken)
- **Lines:** [692-695]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CashierCashMovementAsync(Falah.Application.CashDrawer.CashierCashMovementRequest, CancellationToken) across lines 692-695.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:694:59:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:694:59:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [694-694]
- **Behavior:** Executes logic for lambda expression across lines 694-694.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:697:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:697:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.OpenCashDrawerAsync(Falah.Application.CashDrawer.OpenCashDrawerRequest, CancellationToken)
- **Lines:** [697-700]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.OpenCashDrawerAsync(Falah.Application.CashDrawer.OpenCashDrawerRequest, CancellationToken) across lines 697-700.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:699:51:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:699:51:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [699-699]
- **Behavior:** Executes logic for lambda expression across lines 699-699.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:706:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:706:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTreasuryBalanceAsync(string, CancellationToken)
- **Lines:** [706-709]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.GetTreasuryBalanceAsync(string, CancellationToken) across lines 706-709.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:708:52:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:708:52:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [708-708]
- **Behavior:** Executes logic for lambda expression across lines 708-708.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:711:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:711:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreatePrintJobsAsync(string, Falah.Application.Printing.CreatePrintJobsRequest, CancellationToken)
- **Lines:** [711-714]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.CreatePrintJobsAsync(string, Falah.Application.Printing.CreatePrintJobsRequest, CancellationToken) across lines 711-714.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:713:55:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:713:55:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [713-713]
- **Behavior:** Executes logic for lambda expression across lines 713-713.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:718:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:718:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ReprintDocumentAsync(Falah.Application.Printing.ReprintDocumentRequest, CancellationToken)
- **Lines:** [718-721]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.Services.ApiClient.ReprintDocumentAsync(Falah.Application.Printing.ReprintDocumentRequest, CancellationToken) across lines 718-721.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:720:55:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.cs:720:55:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [720-720]
- **Behavior:** Executes logic for lambda expression across lines 720-720.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
