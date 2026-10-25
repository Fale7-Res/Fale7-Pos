# File Review: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:29:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:29:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CashierViewModel(Falah.Client.CashierWinUI.Services.AppSession, Falah.Client.CashierWinUI.ViewModels.HeaderViewModel)
- **Lines:** [29-34]
- **Behavior:** Initializes CashierViewModel with injected AppSession and HeaderViewModel dependencies. Hooks Cart.CollectionChanged event to automatically trigger RecomputeCartTotals() and NotifyCheckoutAvailability() whenever items are added or removed from the basket.
- **Validations:** None on constructor parameters. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:33:35:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:33:35:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [33-33]
- **Behavior:** Event handler lambda (_, _) => { RecomputeCartTotals(); NotifyCheckoutAvailability(); } attached to Cart.CollectionChanged to recalculate basket figures and refresh checkout button availability.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:41:63:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:41:63:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.Categories.get
- **Lines:** [41-41]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.CashierViewModel.Categories.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:44:63:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:44:63:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CategoryTiles.get
- **Lines:** [44-44]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CategoryTiles.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:48:64:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:48:64:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.VisibleProducts.get
- **Lines:** [48-48]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.CashierViewModel.VisibleProducts.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:50:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:50:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnSelectedCategoryChanged(Falah.Application.Menu.MenuCategoryDto?)
- **Lines:** [50-54]
- **Behavior:** Partial change notification handler for SelectedCategory. Calls RefreshVisibleProducts() to display products of the active category, and updates IsSelected flag on all CategoryTiles via ReferenceEquals comparison.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:56:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:56:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnSearchQueryChanged(string)
- **Lines:** [56-56]
- **Behavior:** Partial change notification handler for SearchQuery; invokes RefreshVisibleProducts() to filter catalog products live across categories.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:60:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:60:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnIsLoadingMenuChanged(bool)
- **Lines:** [60-60]
- **Behavior:** Partial change notification handler on IsLoadingMenu; calls NotifyCheckoutAvailability() so checkout buttons update their disabled state during menu load.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:61:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:61:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnMenuErrorChanged(string?)
- **Lines:** [61-61]
- **Behavior:** Partial change notification handler on MenuError; calls NotifyCheckoutAvailability() to disable checkout when menu failed to load.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:69:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:69:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.BuildBestSellers(IReadOnlyList<string>)
- **Lines:** [69-76]
- **Behavior:** Builds synthetic best sellers category DTO (__best_sellers, "الأكثر مبيعًا", "🔥") based on server-ranked productIds. Groups loaded category products by ID, filters to items present in byId dictionary, preserving server ranking order, and returns null if no products match or list is empty.
- **Validations:** Checks productIds.Count > 0 and matching product count > 0. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:72:42:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:72:42:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [72-72]
- **Behavior:** Flat-mapping projection c => c.Products extracting product lists across loaded categories in BuildBestSellers.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:72:67:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:72:67:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [72-72]
- **Behavior:** Grouping key selector p => p.Id grouping products by product ID in BuildBestSellers.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:72:91:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:72:91:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [72-72]
- **Behavior:** Dictionary key selector g => g.Key extracting product ID in BuildBestSellers.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:72:103:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:72:103:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [72-72]
- **Behavior:** Dictionary value selector g => g.First() picking the first product instance per ID.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:74:66:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:74:66:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [74-74]
- **Behavior:** Projection selector id => byId[id] resolving product DTOs in server ranking order.
- **Validations:** Dictionary lookup guarded by byId.ContainsKey check. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:80:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:80:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.RefreshVisibleProducts()
- **Lines:** [80-100]
- **Behavior:** Clears and repopulates VisibleProducts. If SearchQuery is empty, shows SelectedCategory?.Products; otherwise searches across all categories for products whose name contains SearchQuery. Builds iconByProduct map to ensure search results display their origin category icon. Wraps each item in a ProductTile model with applicable offers, image URI, fallback icon, and best-seller badge. Notifies HasNoResults and CatalogueTitle.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:85:37:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:85:37:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [85-85]
- **Behavior:** Flat-mapping projection c => c.Products extracting products across all categories when executing search in RefreshVisibleProducts.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:85:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:85:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [85-85]
- **Behavior:** Filter predicate matching product name against SearchQuery using CurrentCultureIgnoreCase.
- **Validations:** Case-insensitive string containment check. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:89:25:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:89:25:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [89-89]
- **Behavior:** Inline expression lambda '.SelectMany(c => c.Products.Select(p => (p.Id, c.Icon)))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:89:48:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:89:48:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [89-89]
- **Behavior:** Inline expression lambda '.SelectMany(c => c.Products.Select(p => (p.Id, c.Icon)))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:90:22:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:90:22:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [90-90]
- **Behavior:** Inline expression lambda '.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Icon);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:90:46:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:90:46:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [90-90]
- **Behavior:** Inline expression lambda '.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Icon);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:90:58:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:90:58:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [90-90]
- **Behavior:** Inline expression lambda '.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Icon);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:94:43:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:94:43:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [94-94]
- **Behavior:** Inline expression lambda 'p.ApplicableOffers.Select(o => new OfferChoice(p, o)).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:106:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:106:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.RetryLoadMenuAsync()
- **Lines:** [106-107]
- **Behavior:** RelayCommand retrying menu load by delegating to LoadMenuAsync().
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:109:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:109:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.LoadMenuAsync()
- **Lines:** [109-152]
- **Behavior:** Asynchronously loads POS menu snapshot and tax rules from server. Sets IsLoadingMenu = true, clears MenuError. Calls GetMenuAsync() and GetTaxRulesAsync(). Configures domain TaxRule from the first active tax rule, populates Categories, constructs synthetic best sellers tile if applicable, populates CategoryTiles, and selects first category. Wrapped in try/finally setting IsLoadingMenu = false and notifying HasNoResults.
- **Validations:** Validates API result success for menu and tax endpoints. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:124:50:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:124:50:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [124-124]
- **Behavior:** Inline expression lambda 'var tax = taxes.Value.FirstOrDefault(t => t.Enabled);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:158:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:158:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnVariantPickerProductChanged(Falah.Application.Menu.MenuProductDto?)
- **Lines:** [158-158]
- **Behavior:** Partial change notification handler for VariantPickerProduct; notifies PropertyChanged for IsVariantPickerOpen to control variant picker flyout visibility.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:160:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:160:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SelectProduct(Falah.Application.Menu.MenuProductDto)
- **Lines:** [160-166]
- **Behavior:** RelayCommand handling catalog product tile tap. Validates CanEditOperation. If product has multiple variants (Variants.Count > 1), opens variant picker flyout by setting VariantPickerProduct = product. If exactly one variant exists, automatically selects it. Otherwise adds base product to cart via AddToCart.
- **Validations:** Guarded by if (!CanEditOperation) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:168:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:168:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SelectOffer(Falah.Client.CashierWinUI.Models.OfferChoice)
- **Lines:** [168-179]
- **Behavior:** RelayCommand handling promotional offer chip tap. Validates CanEditOperation. If the item has multiple variants, opens variant picker with PendingOffer = choice.Offer. Otherwise adds product to cart with the applied offer ID via AddToCart.
- **Validations:** Guarded by if (!CanEditOperation) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:191:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:191:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OnPendingOfferChanged(Falah.Application.Menu.MenuOfferDto?)
- **Lines:** [191-195]
- **Behavior:** Partial change notification handler for PendingOffer; raises PropertyChanged for HasPendingOffer and PendingOfferBanner to display the fixed offer price banner on the variant picker.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:197:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:197:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SelectVariant(Falah.Application.Menu.MenuVariantDto)
- **Lines:** [197-205]
- **Behavior:** RelayCommand confirming variant choice in picker flyout. Validates CanEditOperation and non-null VariantPickerProduct. Invokes AddToCart with selected variant and PendingOffer?.Id, then resets VariantPickerProduct = null and PendingOffer = null.
- **Validations:** Guarded by if (!CanEditOperation || VariantPickerProduct is null) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:207:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:207:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CloseVariantPicker()
- **Lines:** [207-208]
- **Behavior:** RelayCommand dismissing variant picker flyout: resets VariantPickerProduct = null and PendingOffer = null.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:210:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:210:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.AddToCart(Falah.Application.Menu.MenuProductDto, Falah.Application.Menu.MenuVariantDto?, string?)
- **Lines:** [210-226]
- **Behavior:** Constructs a new CartLine and appends it to Cart collection. Validates CanEditOperation. Resolves unit price precedence: offer FixedPricePiastres if offerId matches, else variant PricePiastres, else product BasePricePiastres. Sets ProductId, VariantId, names, SendToGrill flag, and offer metadata.
- **Validations:** Guarded by if (!CanEditOperation) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:213:86:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:213:86:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [213-213]
- **Behavior:** Predicate o => o.Id == offerId resolving active promotional offer from product ApplicableOffers.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:230:50:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:230:50:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.Cart.get
- **Lines:** [230-230]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.CashierViewModel.Cart.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:235:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:235:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.RecomputeCartTotals()
- **Lines:** [235-243]
- **Behavior:** Recalculates basket subtotal by summing LineTotalPiastres across all lines in Cart. Raises PropertyChanged notifications for CartSubtotalDisplay, GrandTotalDisplay, GrandTotalPiastres, CartItemCount, and triggers NotifyTotals().
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:237:41:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:237:41:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [237-237]
- **Behavior:** Inline expression lambda 'CartSubtotalPiastres = Cart.Sum(l => l.LineTotalPiastres);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:245:42:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:245:42:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [245-245]
- **Behavior:** Inline expression lambda 'public int CartItemCount => Cart.Sum(l => l.Quantity);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:252:9:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:252:9:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.GrandTotalPiastres.get
- **Lines:** [252-258]
- **Behavior:** Computes local estimate of order grand total in piastres: adds delivery service fee if OrderType == "delivery", subtracts discount amount, applies tax calculation via domain TaxEngine.Calculate(taxable, _taxRule).Final.Piastres, and subtracts comp amount. Clamps negative result to 0.
- **Validations:** Clamps result to >= 0. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:263:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:263:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.IncreaseQuantity(Falah.Client.CashierWinUI.Models.CartLine)
- **Lines:** [263-264]
- **Behavior:** RelayCommand incrementing quantity on a cart line when CanEditOperation allows, and invoking RecomputeCartTotals().
- **Validations:** Guarded by if (!CanEditOperation) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:266:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:266:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.DecreaseQuantity(Falah.Client.CashierWinUI.Models.CartLine)
- **Lines:** [266-272]
- **Behavior:** RelayCommand decrementing quantity on a cart line if quantity > 1, or removing line from Cart if quantity == 1, then calling RecomputeCartTotals().
- **Validations:** Guarded by if (!CanEditOperation) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:274:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:274:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.RemoveLine(Falah.Client.CashierWinUI.Models.CartLine)
- **Lines:** [274-275]
- **Behavior:** RelayCommand removing a line from Cart when CanEditOperation allows, and calling RecomputeCartTotals().
- **Validations:** Guarded by if (!CanEditOperation) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:280:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:280:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.AppendNoteText(Falah.Client.CashierWinUI.Models.CartLine, string)
- **Lines:** [280-284]
- **Behavior:** Appends modifier note text (e.g. "بدون بصل") to line.Notes with Arabic comma separation when CanEditOperation allows.
- **Validations:** Guarded by if (!CanEditOperation) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:286:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:286:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ClearCart()
- **Lines:** [286-295]
- **Behavior:** RelayCommand resetting entire cart: clears Cart lines, disables discounts, comp, and split payment flags, resets delivery configuration, recalculates totals via RecomputeCartTotals(), and clears CurrentCheckoutRequestId.
- **Validations:** Guarded by if (!CanEditOperation) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:301:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:301:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SetOrderType(string)
- **Lines:** [301-302]
- **Behavior:** RelayCommand updating OrderType (e.g. "takeaway", "dinein", "delivery") when CanEditOperation allows.
- **Validations:** Guarded by if (CanEditOperation). Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:304:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs:304:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SelectCategory(Falah.Client.CashierWinUI.Models.CategoryTile)
- **Lines:** [304-305]
- **Behavior:** RelayCommand selecting an active category tile: sets SelectedCategory = tile.Category.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
