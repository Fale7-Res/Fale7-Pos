# File Review: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs.

## Callables

### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:23:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:23:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.CashierViewModel(Falah.Client.Cashier.Services.AppSession, Falah.Client.Cashier.ViewModels.HeaderViewModel)
- **Lines:** [23-28]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.CashierViewModel(Falah.Client.Cashier.Services.AppSession, Falah.Client.Cashier.ViewModels.HeaderViewModel) across lines 23-28.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:27:35:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:27:35:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [27-27]
- **Behavior:** Inline expression lambda 'Cart.CollectionChanged += (_, _) => RecomputeCartTotals();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:35:63:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:35:63:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.Categories.get
- **Lines:** [35-35]
- **Behavior:** Accessor 'get' for property in Falah.Client.Cashier.ViewModels.CashierViewModel.Categories.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:39:64:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:39:64:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.VisibleProducts.get
- **Lines:** [39-39]
- **Behavior:** Accessor 'get' for property in Falah.Client.Cashier.ViewModels.CashierViewModel.VisibleProducts.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:41:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:41:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.OnSelectedCategoryChanged(Falah.Application.Menu.MenuCategoryDto?)
- **Lines:** [41-41]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.OnSelectedCategoryChanged(Falah.Application.Menu.MenuCategoryDto?) across lines 41-41.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:42:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:42:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.OnSearchQueryChanged(string)
- **Lines:** [42-42]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.OnSearchQueryChanged(string) across lines 42-42.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:44:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:44:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.RefreshVisibleProducts()
- **Lines:** [44-52]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.RefreshVisibleProducts() across lines 44-52.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:49:37:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:49:37:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [49-49]
- **Behavior:** Executes logic for lambda expression across lines 49-49.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:49:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:49:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [49-49]
- **Behavior:** Executes logic for lambda expression across lines 49-49.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:51:78:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:51:78:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [51-51]
- **Behavior:** Executes logic for lambda expression across lines 51-51.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:54:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:54:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.LoadMenuAsync()
- **Lines:** [54-74]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.LoadMenuAsync() across lines 54-74.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:80:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:80:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.OnVariantPickerProductChanged(Falah.Application.Menu.MenuProductDto?)
- **Lines:** [80-80]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.OnVariantPickerProductChanged(Falah.Application.Menu.MenuProductDto?) across lines 80-80.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:82:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:82:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SelectProduct(Falah.Application.Menu.MenuProductDto)
- **Lines:** [82-87]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.SelectProduct(Falah.Application.Menu.MenuProductDto) across lines 82-87.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:89:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:89:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SelectOffer(Falah.Client.Cashier.Models.OfferChoice)
- **Lines:** [89-94]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.SelectOffer(Falah.Client.Cashier.Models.OfferChoice) across lines 89-94.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:98:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:98:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SelectVariant(Falah.Application.Menu.MenuVariantDto)
- **Lines:** [98-105]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.SelectVariant(Falah.Application.Menu.MenuVariantDto) across lines 98-105.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:107:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:107:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.CloseVariantPicker()
- **Lines:** [107-108]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.CloseVariantPicker() across lines 107-108.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:110:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:110:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.AddToCart(Falah.Application.Menu.MenuProductDto, Falah.Application.Menu.MenuVariantDto?, string?)
- **Lines:** [110-125]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.AddToCart(Falah.Application.Menu.MenuProductDto, Falah.Application.Menu.MenuVariantDto?, string?) across lines 110-125.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:112:86:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:112:86:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [112-112]
- **Behavior:** Executes logic for lambda expression across lines 112-112.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:129:50:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:129:50:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.Cart.get
- **Lines:** [129-129]
- **Behavior:** Accessor 'get' for property in Falah.Client.Cashier.ViewModels.CashierViewModel.Cart.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:134:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:134:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.RecomputeCartTotals()
- **Lines:** [134-140]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.RecomputeCartTotals() across lines 134-140.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:136:41:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:136:41:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [136-136]
- **Behavior:** Inline expression lambda 'CartSubtotalPiastres = Cart.Sum(l => l.LineTotalPiastres);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:145:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:145:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.IncreaseQuantity(Falah.Client.Cashier.Models.CartLine)
- **Lines:** [145-146]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.IncreaseQuantity(Falah.Client.Cashier.Models.CartLine) across lines 145-146.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:148:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:148:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.DecreaseQuantity(Falah.Client.Cashier.Models.CartLine)
- **Lines:** [148-153]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.DecreaseQuantity(Falah.Client.Cashier.Models.CartLine) across lines 148-153.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:155:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:155:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.RemoveLine(Falah.Client.Cashier.Models.CartLine)
- **Lines:** [155-156]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.RemoveLine(Falah.Client.Cashier.Models.CartLine) across lines 155-156.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:158:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:158:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.AppendNote((Falah.Client.Cashier.Models.CartLine? Line, string? Note))
- **Lines:** [158-163]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.AppendNote((Falah.Client.Cashier.Models.CartLine? Line, string? Note)) across lines 158-163.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:165:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:165:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.ClearCart()
- **Lines:** [165-171]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.ClearCart() across lines 165-171.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:178:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:178:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SetOrderType(string)
- **Lines:** [178-179]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.SetOrderType(string) across lines 178-179.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:181:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:181:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SelectCategory(Falah.Application.Menu.MenuCategoryDto)
- **Lines:** [181-182]
- **Behavior:** Executes logic for Falah.Client.Cashier.ViewModels.CashierViewModel.SelectCategory(Falah.Application.Menu.MenuCategoryDto) across lines 181-182.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
