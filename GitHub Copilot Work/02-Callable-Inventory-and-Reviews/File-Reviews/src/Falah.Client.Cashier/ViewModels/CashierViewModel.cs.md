# File Review: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs.

## Callables

### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:23:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:23:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.CashierViewModel(Falah.Client.Cashier.Services.AppSession, Falah.Client.Cashier.ViewModels.HeaderViewModel)
- **Lines:** [23-28]
- **Behavior:** Initializes cashier viewmodel with AppSession and HeaderViewModel, subscribing to cart changes to recompute local totals.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:27:35:ParenthesizedLambdaExpression

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
- **Behavior:** Refreshes visible products when active menu category changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:42:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:42:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.OnSearchQueryChanged(string)
- **Lines:** [42-42]
- **Behavior:** Refreshes visible products filtered by search query when search text changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:44:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:44:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.RefreshVisibleProducts()
- **Lines:** [44-52]
- **Behavior:** Populates visible product tiles from active category or search query with applicable offers.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:49:37:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:49:37:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [49-49]
- **Behavior:** Flattens product collections across all categories for global menu search.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:49:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:49:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [49-49]
- **Behavior:** Filters menu products by case-insensitive name substring match.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:51:78:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:51:78:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [51-51]
- **Behavior:** Wraps applicable offers on a menu product into OfferChoice models.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:54:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:54:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.LoadMenuAsync()
- **Lines:** [54-74]
- **Behavior:** Asynchronously loads menu categories and products from the API and selects initial category.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:80:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:80:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.OnVariantPickerProductChanged(Falah.Application.Menu.MenuProductDto?)
- **Lines:** [80-80]
- **Behavior:** Notifies IsVariantPickerOpen property change when product requiring variant selection changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:82:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:82:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SelectProduct(Falah.Application.Menu.MenuProductDto)
- **Lines:** [82-87]
- **Behavior:** Opens variant picker if product has multiple variants, or adds product to cart with default variant.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:89:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:89:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SelectOffer(Falah.Client.Cashier.Models.OfferChoice)
- **Lines:** [89-94]
- **Behavior:** Selects an offer, opening variant picker if multiple variants exist or adding offer item directly to cart.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:98:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:98:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SelectVariant(Falah.Application.Menu.MenuVariantDto)
- **Lines:** [98-105]
- **Behavior:** Adds selected product variant and pending offer to cart and closes variant picker.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:107:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:107:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.CloseVariantPicker()
- **Lines:** [107-108]
- **Behavior:** Dismisses variant selection modal and clears pending offer state.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:110:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:110:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.AddToCart(Falah.Application.Menu.MenuProductDto, Falah.Application.Menu.MenuVariantDto?, string?)
- **Lines:** [110-125]
- **Behavior:** Calculates line unit price based on offer, variant, or base product price and adds CartLine to cart.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:112:86:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:112:86:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [112-112]
- **Behavior:** Finds applicable offer on product matching offerId.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:129:50:GetAccessorDeclaration

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
- **Behavior:** Recalculates cart subtotal across all lines and notifies UI bindings for Subtotal and GrandTotal displays.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:136:41:SimpleLambdaExpression

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
- **Behavior:** Increments line quantity and updates cart totals.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:148:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:148:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.DecreaseQuantity(Falah.Client.Cashier.Models.CartLine)
- **Lines:** [148-153]
- **Behavior:** Decrements line quantity (removing line if reaching zero) and updates cart totals.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:155:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:155:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.RemoveLine(Falah.Client.Cashier.Models.CartLine)
- **Lines:** [155-156]
- **Behavior:** Removes specified line item from cart and recalculates totals.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:158:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:158:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.AppendNote((Falah.Client.Cashier.Models.CartLine? Line, string? Note))
- **Lines:** [158-163]
- **Behavior:** Appends quick note text to cart line notes with comma separator.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:165:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:165:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.ClearCart()
- **Lines:** [165-171]
- **Behavior:** Clears all cart line items, resets subtotal, and invalidates cached checkout request ID.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:178:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:178:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SetOrderType(string)
- **Lines:** [178-179]
- **Behavior:** Sets the active order type (e.g. takeaway, hall, delivery).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---### Callable: src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:181:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:181:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Cashier.ViewModels.CashierViewModel.SelectCategory(Falah.Application.Menu.MenuCategoryDto)
- **Lines:** [181-182]
- **Behavior:** Sets active menu category selection.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

---

## Candidate Findings & Observations

### Observation: LEGACY-CASHIER-DELIVERY-FEE-OMISSION - Hardcoded 0 Delivery Fee and Lack of Delivery Submodule
- **Severity**: Low / Architectural Divergence
- **Location**: `src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs` (line 142)
- **Code**:
  ```csharp
  public long GrandTotalPiastres => CartSubtotalPiastres; // delivery fee is 0 in this slice - see OrderType notes
  ```
- **Analysis**:
  In the legacy WPF Cashier client (`Falah.Client.Cashier`), delivery orders do not calculate delivery service fees and hardcode the delivery fee contribution to 0 (`GrandTotalPiastres => CartSubtotalPiastres`). Furthermore, the legacy client lacks the structured delivery and customer address management present in `Falah.Client.CashierWinUI` (`CashierViewModel.Delivery.cs`). In contrast, `Falah.Client.CashierWinUI` dynamically retrieves active delivery services via `GetActiveDeliveryServicesAsync()`, allows selection of delivery services, and incorporates dynamic delivery pricing into cart calculations.
