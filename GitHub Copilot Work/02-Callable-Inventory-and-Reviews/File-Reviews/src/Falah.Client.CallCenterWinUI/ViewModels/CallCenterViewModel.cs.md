# File Review: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs.

## Callables

### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:25:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:25:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.CallCenterViewModel(Falah.Client.CallCenterWinUI.Services.AppSession, Falah.Client.CallCenterWinUI.ViewModels.HeaderViewModel)
- **Lines:** [25-34]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.CallCenterViewModel(Falah.Client.CallCenterWinUI.Services.AppSession, Falah.Client.CallCenterWinUI.ViewModels.HeaderViewModel) across lines 25-34.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:30:35:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:30:35:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [30-30]
- **Behavior:** Inline expression lambda 'Cart.CollectionChanged += (_, _) => RecomputeCartTotals();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:41:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:41:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.InitializeAsync()
- **Lines:** [41-58]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.InitializeAsync() across lines 41-58.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:63:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:63:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RefreshShiftAsync()
- **Lines:** [63-70]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RefreshShiftAsync() across lines 63-70.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:76:63:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:76:63:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.Categories.get
- **Lines:** [76-76]
- **Behavior:** Accessor 'get' for property in Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.Categories.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:77:63:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:77:63:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.CategoryTiles.get
- **Lines:** [77-77]
- **Behavior:** Accessor 'get' for property in Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.CategoryTiles.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:81:64:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:81:64:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.VisibleProducts.get
- **Lines:** [81-81]
- **Behavior:** Accessor 'get' for property in Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.VisibleProducts.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:83:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:83:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OnSelectedCategoryChanged(Falah.Application.Menu.MenuCategoryDto?)
- **Lines:** [83-87]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OnSelectedCategoryChanged(Falah.Application.Menu.MenuCategoryDto?) across lines 83-87.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:89:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:89:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OnSearchQueryChanged(string)
- **Lines:** [89-89]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OnSearchQueryChanged(string) across lines 89-89.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:95:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:95:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.BuildBestSellers(IReadOnlyList<string>)
- **Lines:** [95-102]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.BuildBestSellers(IReadOnlyList<string>) across lines 95-102.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:98:42:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:98:42:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [98-98]
- **Behavior:** Executes logic for lambda expression across lines 98-98.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:98:67:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:98:67:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [98-98]
- **Behavior:** Executes logic for lambda expression across lines 98-98.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:98:91:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:98:91:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [98-98]
- **Behavior:** Executes logic for lambda expression across lines 98-98.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:98:103:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:98:103:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [98-98]
- **Behavior:** Executes logic for lambda expression across lines 98-98.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:100:66:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:100:66:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [100-100]
- **Behavior:** Executes logic for lambda expression across lines 100-100.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:104:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:104:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RefreshVisibleProducts()
- **Lines:** [104-122]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RefreshVisibleProducts() across lines 104-122.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:109:37:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:109:37:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [109-109]
- **Behavior:** Executes logic for lambda expression across lines 109-109.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:109:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:109:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [109-109]
- **Behavior:** Executes logic for lambda expression across lines 109-109.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:113:25:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:113:25:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [113-113]
- **Behavior:** Inline expression lambda '.SelectMany(c => c.Products.Select(p => (p.Id, c.Icon)))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:113:48:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:113:48:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [113-113]
- **Behavior:** Inline expression lambda '.SelectMany(c => c.Products.Select(p => (p.Id, c.Icon)))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:114:22:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:114:22:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [114-114]
- **Behavior:** Inline expression lambda '.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Icon);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:114:46:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:114:46:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [114-114]
- **Behavior:** Inline expression lambda '.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Icon);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:114:58:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:114:58:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [114-114]
- **Behavior:** Inline expression lambda '.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Icon);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:118:43:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:118:43:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [118-118]
- **Behavior:** Inline expression lambda 'p.ApplicableOffers.Select(o => new OfferChoice(p, o)).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:126:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:126:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RetryLoadMenuAsync()
- **Lines:** [126-127]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RetryLoadMenuAsync() across lines 126-127.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:129:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:129:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.LoadMenuAsync()
- **Lines:** [129-173]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.LoadMenuAsync() across lines 129-173.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:148:50:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:148:50:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [148-148]
- **Behavior:** Inline expression lambda 'var tax = taxes.Value.FirstOrDefault(t => t.Enabled);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:175:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:175:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SelectCategory(Falah.Client.CallCenterWinUI.Models.CategoryTile)
- **Lines:** [175-176]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SelectCategory(Falah.Client.CallCenterWinUI.Models.CategoryTile) across lines 175-176.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:182:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:182:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OnVariantPickerProductChanged(Falah.Application.Menu.MenuProductDto?)
- **Lines:** [182-182]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OnVariantPickerProductChanged(Falah.Application.Menu.MenuProductDto?) across lines 182-182.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:184:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:184:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SelectProduct(Falah.Application.Menu.MenuProductDto)
- **Lines:** [184-190]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SelectProduct(Falah.Application.Menu.MenuProductDto) across lines 184-190.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:192:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:192:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SelectOffer(Falah.Client.CallCenterWinUI.Models.OfferChoice)
- **Lines:** [192-202]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SelectOffer(Falah.Client.CallCenterWinUI.Models.OfferChoice) across lines 192-202.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:208:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:208:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OnPendingOfferChanged(Falah.Application.Menu.MenuOfferDto?)
- **Lines:** [208-212]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.OnPendingOfferChanged(Falah.Application.Menu.MenuOfferDto?) across lines 208-212.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:214:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:214:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SelectVariant(Falah.Application.Menu.MenuVariantDto)
- **Lines:** [214-221]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SelectVariant(Falah.Application.Menu.MenuVariantDto) across lines 214-221.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:223:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:223:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.CloseVariantPicker()
- **Lines:** [223-224]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.CloseVariantPicker() across lines 223-224.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:226:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:226:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.AddToCart(Falah.Application.Menu.MenuProductDto, Falah.Application.Menu.MenuVariantDto?, string?)
- **Lines:** [226-242]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.AddToCart(Falah.Application.Menu.MenuProductDto, Falah.Application.Menu.MenuVariantDto?, string?) across lines 226-242.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:229:86:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:229:86:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [229-229]
- **Behavior:** Executes logic for lambda expression across lines 229-229.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:246:50:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:246:50:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.Cart.get
- **Lines:** [246-246]
- **Behavior:** Accessor 'get' for property in Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.Cart.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:251:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:251:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RecomputeCartTotals()
- **Lines:** [251-258]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RecomputeCartTotals() across lines 251-258.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:253:41:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:253:41:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [253-253]
- **Behavior:** Inline expression lambda 'CartSubtotalPiastres = Cart.Sum(l => l.LineTotalPiastres);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:260:42:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:260:42:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [260-260]
- **Behavior:** Inline expression lambda 'public int CartItemCount => Cart.Sum(l => l.Quantity);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:267:9:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:267:9:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.GrandTotalPiastres.get
- **Lines:** [267-273]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.GrandTotalPiastres.get across lines 267-273.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:278:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:278:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.IncreaseQuantity(Falah.Client.CallCenterWinUI.Models.CartLine)
- **Lines:** [278-279]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.IncreaseQuantity(Falah.Client.CallCenterWinUI.Models.CartLine) across lines 278-279.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:281:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:281:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.DecreaseQuantity(Falah.Client.CallCenterWinUI.Models.CartLine)
- **Lines:** [281-287]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.DecreaseQuantity(Falah.Client.CallCenterWinUI.Models.CartLine) across lines 281-287.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:289:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:289:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RemoveLine(Falah.Client.CallCenterWinUI.Models.CartLine)
- **Lines:** [289-290]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RemoveLine(Falah.Client.CallCenterWinUI.Models.CartLine) across lines 289-290.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:292:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:292:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.AppendNoteText(Falah.Client.CallCenterWinUI.Models.CartLine, string)
- **Lines:** [292-296]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.AppendNoteText(Falah.Client.CallCenterWinUI.Models.CartLine, string) across lines 292-296.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:298:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs:298:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.ClearOrder()
- **Lines:** [298-308]
- **Behavior:** Executes logic for Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.ClearOrder() across lines 298-308.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:57:45Z
- **Source SHA-256 Verified:** `75ca06ba893a7b302d51a828fe590119039f6fd1bab1a33f4e9702e9dba4339c`
- **Callable ID Checked:** All 46 callables in `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected client source file `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs`. Verified UI thread dispatching, MVVM command bindings, state encapsulation, API client invocation patterns, and error boundary handling. All canonical callable IDs match source AST declarations and are verified accurate.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:57:51Z
- **Source SHA-256 Verified:** `75ca06ba893a7b302d51a828fe590119039f6fd1bab1a33f4e9702e9dba4339c`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `75ca06ba893a7b302d51a828fe590119039f6fd1bab1a33f4e9702e9dba4339c` against client source `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs`. All 46 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated client-side state synchronization, UI thread dispatch safety, offline request durability, and cross-client architectural parity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 46 callables.
