# File Review: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:23:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:23:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.MenuEditorViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [23-23]
- **Behavior:** Initializes MenuEditorViewModel with the injected AppSession dependency. Instantiates a RetainedRequest<object> (_write) keyed as "menu.authoring" to ensure all menu and pricing authoring commands execute idempotently with crash-recovery support.
- **Validations:** None on constructor parameters. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:29:37:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:29:37:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Id.get
- **Lines:** [29-29]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Id.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:29:42:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:29:42:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Id.init
- **Lines:** [29-29]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Id.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:30:39:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:30:39:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Name.get
- **Lines:** [30-30]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Name.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:30:44:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:30:44:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Name.init
- **Lines:** [30-30]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ProductChoice.Name.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:35:37:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:35:37:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Value.get
- **Lines:** [35-35]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Value.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:35:42:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:35:42:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Value.init
- **Lines:** [35-35]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Value.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:36:39:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:36:39:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Name.get
- **Lines:** [36-36]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Name.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:36:44:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:36:44:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Name.init
- **Lines:** [36-36]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.DayChoice.Name.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:41:29:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:41:29:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.VariantRow.Id.get
- **Lines:** [41-41]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.VariantRow.Id.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:41:34:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:41:34:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.VariantRow.Id.init
- **Lines:** [41-41]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.VariantRow.Id.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:46:52:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:46:52:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Rows.get
- **Lines:** [46-46]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Rows.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:47:59:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:47:59:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.CategoryChoices.get
- **Lines:** [47-47]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.CategoryChoices.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:48:57:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:48:57:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RecipeChoices.get
- **Lines:** [48-48]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RecipeChoices.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:49:56:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:49:56:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Variants.get
- **Lines:** [49-49]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Variants.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:50:64:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:50:64:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.PriceProducts.get
- **Lines:** [50-50]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.PriceProducts.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:51:45:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:51:45:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Stations.get
- **Lines:** [51-51]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Stations.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:52:45:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:52:45:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.TaxModes.get
- **Lines:** [52-52]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.TaxModes.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:53:44:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:53:44:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Days.get
- **Lines:** [53-53]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Days.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:54:17:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:54:17:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [54-54]
- **Behavior:** LINQ projection lambda (name, index) => new DayChoice { Value = index, Name = name } mapping day names (Sunday through Saturday) to DayChoice observable objects to initialize the Days collection.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:104:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:104:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnIsBusyChanged(bool)
- **Lines:** [104-104]
- **Behavior:** Partial change lifecycle method on IsBusy; triggers NotifyState() to refresh command availability and lock navigation while a save is in progress.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:105:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:105:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnIsLoadingChanged(bool)
- **Lines:** [105-105]
- **Behavior:** Partial change lifecycle method on IsLoading; triggers NotifyState() to refresh UI navigation and button states.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:106:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:106:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.NotifyState()
- **Lines:** [106-110]
- **Behavior:** Raises PropertyChanged notifications for CanBrowse, CanEdit, CanToggleStatus, HasNoRows, SaveLabel, StatusActionLabel, EditorTitle, CanEditImage, and ImageHint. Calls _session.SetManagementOperationPending(this, IsBusy || _write.HasPending) to prevent shell navigation while an authoring operation is pending.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:111:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:111:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OpenAsync()
- **Lines:** [111-116]
- **Behavior:** Asynchronously opens the menu editor dialog. Validates CanOpen (checks menu_manage, settings_manage, or offers_manage) and verifies that no unresolved operations exist in the session. Sets IsOpen = true, HasEditor = false, clears error/note messages, and invokes LoadAsync().
- **Validations:** Checks CanOpen and !_session.HasUnresolvedOperation. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:117:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:117:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Close()
- **Lines:** [117-121]
- **Behavior:** RelayCommand closing the menu editor dialog when CanBrowse allows. Sets IsOpen = false, HasEditor = false, and fires the Closed event.
- **Validations:** Guarded by if (!CanBrowse) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:122:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:122:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.LoadAsync()
- **Lines:** [122-126]
- **Behavior:** RelayCommand loading catalog entities. Verifies CanBrowse is true and calls ReloadAsync().
- **Validations:** Guarded by if (!CanBrowse) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:127:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:127:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ReloadAsync()
- **Lines:** [127-152]
- **Behavior:** Asynchronously fetches all menu management entities: categories via GetAllCategoriesAsync(), products via GetAllProductsAsync(), recipes via GetRecipesAsync(), tax rules via GetTaxRulesAsync(), and price rules via GetPriceRulesAsync(). Populates CategoryChoices (with inactive flags), RecipeChoices (including placeholder for orphaned recipe links), and PriceProducts. Sets IsReady = true and calls RefreshRows(). Wrapped in try/finally setting IsLoading = false.
- **Validations:** Validates API success for all 5 endpoints. Surfaces errors in LoadError. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:143:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:143:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [143-143]
- **Behavior:** LINQ ordering selector c => c.SortOrder sorting categories before populating CategoryChoices in ReloadAsync.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:146:53:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:146:53:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [146-146]
- **Behavior:** Flat-mapping projection extracting all recipe IDs referenced by products or their variants (p => p.Variants.Select(v => v.RecipeId).Append(p.RecipeId)) in ReloadAsync.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:146:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:146:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [146-146]
- **Behavior:** Projection selector v => v.RecipeId mapping product variants to their attached recipe ID.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:146:119:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:146:119:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [146-146]
- **Behavior:** Filter predicate id => id is not null removing null recipe references when assembling RecipeChoices.
- **Validations:** Null check. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:147:40:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:147:40:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [147-147]
- **Behavior:** Predicate c => c.Id == id verifying if a recipe reference is already present in RecipeChoices.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:153:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:153:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SetKind(string)
- **Lines:** [153-159]
- **Behavior:** RelayCommand switching between catalog domains ("products", "categories", "tax", "prices"). Validates CanBrowse and domain name whitelist. Resets HasEditor = false, _editing = null, clears errors and ProductImageUri, updates domain-specific property notifications, and refreshes catalog rows via RefreshRows() and NotifyState().
- **Validations:** Validates CanBrowse and kind is one of products, categories, tax, prices. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:164:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:164:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnSearchTextChanged(string)
- **Lines:** [164-164]
- **Behavior:** Partial change notification handler for SearchText; invokes RefreshRows() to filter catalog rows live as the user types.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:168:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:168:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RefreshRows()
- **Lines:** [168-186]
- **Behavior:** Filters and populates the Rows observable collection based on active Kind and SearchText. Projects categories sorted by SortOrder, tax rules, price rules, and products sorted by SortOrder. Performs case-insensitive matching on Name and Detail. Notifies HasNoRows and HasNoMatches.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:173:49:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:173:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [173-173]
- **Behavior:** LINQ ordering selector c => c.SortOrder sorting categories in RefreshRows.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:173:74:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:173:74:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [173-173]
- **Behavior:** Projection lambda c => new CatalogRow(c.Name, c.Active ? "نشط" : "متوقف", c) creating catalog table rows for categories.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:174:34:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:174:34:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [174-174]
- **Behavior:** Projection lambda mapping tax rules to CatalogRow objects with formatted rate percentage and enabled status.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:175:40:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:175:40:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [175-175]
- **Behavior:** Projection lambda mapping price rules to CatalogRow objects with formatted fixed price in pounds and active status.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:176:36:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:176:36:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [176-176]
- **Behavior:** LINQ ordering selector p => p.SortOrder sorting products in RefreshRows.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:176:61:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:176:61:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [176-176]
- **Behavior:** Projection lambda mapping products to CatalogRow objects displaying formatted base price and availability.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:180:31:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:180:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [180-181]
- **Behavior:** Filter predicate matching row Name or Detail against search term using CurrentCultureIgnoreCase.
- **Validations:** Case-insensitive string containment checks. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:187:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:187:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.New()
- **Lines:** [187-191]
- **Behavior:** RelayCommand opening a blank creation form. Validates CanEdit, resets form fields via ResetForm(), clears _editing, sets HasEditor = true, and updates state notifications via NotifyState() and NotifyImageState().
- **Validations:** Guarded by if (!CanEdit) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:192:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:192:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ResetForm()
- **Lines:** [192-199]
- **Behavior:** Resets all editor form properties to empty/default states (Name, Icon, PriceText, TaxRateText, StartTimeText, EndTimeText, SortOrderText = "0", Category = null, Station = Stations[0], Variants cleared, selections on PriceProducts and Days reset).
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:200:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:200:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SelectRow(Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.CatalogRow)
- **Lines:** [200-223]
- **Behavior:** RelayCommand selecting a catalog row to populate the editing form. Validates CanBrowse and IsReady. Resets form, assigns _editing = row.Value, sets HasEditor = true, and binds form fields according to DTO type (CategoryDetailsDto, ProductDetailsDto, TaxRuleDto, PriceRuleDto). Loads product image for products and calls NotifyState().
- **Validations:** Guarded by if (!CanBrowse || !IsReady) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:209:59:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:209:59:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [209-209]
- **Behavior:** Predicate c => c.Id == p.CategoryId finding the matching category choice when selecting a product row.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:209:125:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:209:125:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [209-209]
- **Behavior:** Predicate r => r.Id == p.RecipeId finding the matching recipe choice when selecting a product row.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:210:51:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:210:51:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [210-210]
- **Behavior:** Predicate s => s.Id == p.PreparationStation finding the matching preparation station choice when selecting a product row.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:211:179:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:211:179:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [211-211]
- **Behavior:** Predicate r => r.Id == v.RecipeId finding the matching recipe choice for each product variant during row selection.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:214:132:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:214:132:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [214-214]
- **Behavior:** Predicate m => m.Id == t.Mode finding the matching tax mode choice (Exclusive / Inclusive) when selecting a tax rule row.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:241:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:241:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnProductImageUriChanged(Uri?)
- **Lines:** [241-241]
- **Behavior:** Partial change notification handler for ProductImageUri; calls NotifyImageState() to refresh image editing controls.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:242:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:242:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.OnIsImageBusyChanged(bool)
- **Lines:** [242-242]
- **Behavior:** Partial change notification handler for IsImageBusy; calls NotifyImageState() to disable image actions during upload/delete.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:243:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:243:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.NotifyImageState()
- **Lines:** [243-246]
- **Behavior:** Raises PropertyChanged notifications for HasProductImage, CanEditImage, and ImageHint to keep photo card UI in sync.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:248:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:248:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.LoadProductImage(Falah.Application.Menu.ProductDetailsDto?)
- **Lines:** [248-251]
- **Behavior:** Computes and assigns ProductImageUri by querying _session.Api.ProductImageUri(product.Id, product.ImageVersion), or sets null if product is null.
- **Validations:** Null check on product parameter. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:255:78:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:255:78:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.PickImageAsync.get
- **Lines:** [255-255]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.PickImageAsync.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:255:83:SetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:255:83:SetAccessorDeclaration
- **Kind:** SetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.PickImageAsync.set
- **Lines:** [255-255]
- **Behavior:** Accessor 'set' for property in Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.PickImageAsync.set.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:257:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:257:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ChooseImageAsync()
- **Lines:** [257-289]
- **Behavior:** RelayCommand uploading product photo. Validates CanEditImage, PickImageAsync delegate, _editing as ProductDetailsDto, and non-null current user. Invokes PickImageAsync() to read file bytes, checks size <= 500KB (ProductImageRules.MaxBytes), sets IsImageBusy = true, and calls _session.Api.SetProductImageAsync with Base64 bytes. On success, updates ImageVersion on _editing, refreshes image URI, sets ActionNote, and reloads catalog via ReloadAsync(). Wrapped in try/finally setting IsImageBusy = false.
- **Validations:** Validates CanEditImage, byte length <= ProductImageRules.MaxBytes, API success. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:291:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:291:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RemoveImageAsync()
- **Lines:** [291-307]
- **Behavior:** RelayCommand removing a product photo. Validates CanEditImage, HasProductImage, product selection, and current user. Sets IsImageBusy = true, calls _session.Api.RemoveProductImageAsync(product.Id, actorId), clears ImageVersion, sets ProductImageUri = null, sets ActionNote, and reloads catalog via ReloadAsync(). Wrapped in try/finally.
- **Validations:** Validates CanEditImage, HasProductImage, API success. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:309:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:309:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.AddVariant()
- **Lines:** [309-309]
- **Behavior:** RelayCommand appending a new VariantRow to Variants with default recipe choice when CanEdit && IsProduct is true.
- **Validations:** Guarded by if (CanEdit && IsProduct). Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:310:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:310:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.RemoveVariant(Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.VariantRow)
- **Lines:** [310-310]
- **Behavior:** RelayCommand removing the specified VariantRow from Variants when CanEdit && IsProduct is true.
- **Validations:** Guarded by if (CanEdit && IsProduct). Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:311:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:311:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.TryTime(string, out TimeOnly?)
- **Lines:** [311-316]
- **Behavior:** Validates and parses 24-hour time strings (HH:mm). Returns true with time = null if text is empty/whitespace. Otherwise validates exact format HH:mm using invariant culture.
- **Validations:** Format check via TimeOnly.TryParseExact. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:317:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:317:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.CreateRequest()
- **Lines:** [317-352]
- **Behavior:** Validates current form inputs and constructs the appropriate domain request DTO. Checks CanEdit, HasEditor, current user, non-empty Name, integer SortOrder, positive Price. For categories, creates CreateCategoryRequest or UpdateCategoryRequest. For products, validates category selection, station, variant names, positive variant prices; constructs CreateProductRequest or UpdateProductRequest. For tax, validates positive rate in basis points and tax mode; creates CreateTaxRuleRequest or UpdateTaxRuleRequest. For price rules, validates start/end times and at least one selected product; creates SavePriceRuleRequest. Surfaces validation errors via ActionError and returns null on failure.
- **Validations:** Validates Name, SortOrder, Price > 0, Category non-null, Station non-null, variant names & prices > 0, tax rate, time format, at least 1 product selected for offer. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:348:39:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:348:39:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [348-348]
- **Behavior:** Inline expression lambda 'var ids = PriceProducts.Where(p => p.Selected).Select(p => p.Id).ToArray();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:348:63:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:348:63:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [348-348]
- **Behavior:** Inline expression lambda 'var ids = PriceProducts.Where(p => p.Selected).Select(p => p.Id).ToArray();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:350:31:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:350:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [350-350]
- **Behavior:** Inline expression lambda 'var days = Days.Where(d => d.Selected).Select(d => d.Value).ToArray();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:350:55:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:350:55:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [350-350]
- **Behavior:** Inline expression lambda 'var days = Days.Where(d => d.Selected).Select(d => d.Value).ToArray();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:353:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:353:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SaveAsync()
- **Lines:** [353-359]
- **Behavior:** RelayCommand saving the current entity. Guarded by if (IsBusy) return;. Clears ActionError. Retrieves pending retained request from _write.Value or calls CreateRequest(). If request is non-null, submits it via SubmitAsync(request).
- **Validations:** Guarded by if (IsBusy) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:360:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:360:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.ToggleStatusAsync()
- **Lines:** [360-366]
- **Behavior:** RelayCommand toggling active/available status of selected category or product. Validates CanToggleStatus, generates idempotency key menu-status-{guid}, creates SetCategoryActiveRequest or SetProductAvailabilityRequest with inverted boolean flag, and invokes SubmitAsync(request).
- **Validations:** Guarded by if (!CanToggleStatus) return;. Assumes CurrentUser is non-null. Throws: NullReferenceException if _session.CurrentUser is null.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:367:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:367:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SubmitAsync(object)
- **Lines:** [367-378]
- **Behavior:** Submits authoring request idempotently via _write.SendAsync(request, SendWriteAsync). On failure, sets ActionError from FailureMessage. On success, closes form (HasEditor = false, _editing = null), sets ActionNote, and refreshes catalog state via ReloadAsync(). Manages IsBusy in try/finally.
- **Validations:** Evaluates result.Success. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:379:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:379:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.Confirm<T>(Falah.Client.CashierWinUI.Services.ApiClient.ApiResult<T>)
- **Lines:** [379-380]
- **Behavior:** Generic helper adapting an ApiResult<T> into a boolean ApiResult<bool>, preserving Success, Error, OutcomeUnknown, and StatusCode.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:381:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs:381:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.SendWriteAsync(object)
- **Lines:** [381-393]
- **Behavior:** Pattern-matching command dispatcher executing the appropriate ApiClient method based on request type (CreateCategoryRequest, UpdateCategoryRequest, SetCategoryActiveRequest, CreateProductRequest, UpdateProductRequest, SetProductAvailabilityRequest, CreateTaxRuleRequest, UpdateTaxRuleRequest, SavePriceRuleRequest). Throws InvalidOperationException for unsupported commands.
- **Validations:** Pattern match over 9 request types. Throws: InvalidOperationException if request type is unsupported.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
