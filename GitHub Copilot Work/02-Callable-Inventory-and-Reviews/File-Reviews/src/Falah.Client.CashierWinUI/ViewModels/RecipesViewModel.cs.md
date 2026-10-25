# File Review: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:27:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:27:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.RecipesViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [27-27]
- **Behavior:** Constructor initializing RecipesViewModel with the shared AppSession instance for API communication and permission checks.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:36:53:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:36:53:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.RecipeRow.Cost.get
- **Lines:** [36-36]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.RecipeRow.Cost.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:36:58:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:36:58:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.RecipeRow.Cost.init
- **Lines:** [36-36]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.RecipeRow.Cost.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:40:35:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:40:35:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.RecipeRow.CostsHidden.get
- **Lines:** [40-40]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.RecipeRow.CostsHidden.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:40:40:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:40:40:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.RecipeRow.CostsHidden.init
- **Lines:** [40-40]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.RecipeRow.CostsHidden.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:84:54:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:84:54:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.Recipes.get
- **Lines:** [84-84]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.Recipes.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:95:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:95:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.OnSearchTextChanged(string)
- **Lines:** [95-95]
- **Behavior:** Partial change handler invoked when SearchText changes, triggering ApplyFilter() to update the visible recipes list.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:96:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:96:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.OnFilterIndexChanged(int)
- **Lines:** [96-96]
- **Behavior:** Partial change handler invoked when FilterIndex changes (0: All, 1: Incomplete, 2: Uncosted), triggering ApplyFilter().
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:117:17:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:117:17:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [117-117]
- **Behavior:** Inline expression lambda '}.Where(x => x is not null));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:122:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:122:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.LoadAsync()
- **Lines:** [122-172]
- **Behavior:** Asynchronously loads menu recipes and costing data. If the user lacks reports_view permission (!CanViewCost), delegates to LoadWithoutCostsAsync(). Otherwise fetches cost summaries via _session.Api.GetRecipeCostSummariesAsync and coverage via _session.Api.GetRecipeCostCoverageAsync, populates _allRecipes ordered by category and product name, executes ApplyFilter(), and loads raw ingredients via LoadIngredientsAsync().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:157:61:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:157:61:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [157-157]
- **Behavior:** LINQ sorting key selector delegate ordering recipe summaries alphabetically by category name or product name.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:157:89:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:157:89:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [157-157]
- **Behavior:** LINQ sorting key selector delegate ordering recipe summaries alphabetically by category name or product name.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:176:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:176:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.LoadWithoutCostsAsync()
- **Lines:** [176-207]
- **Behavior:** Alternative loader for users with menu_manage but lacking reports_view permissions. Queries recipes and menu categories/products, builds RecipeRow entries with CostsHidden = true and zeroed financial fields, applies current filter, and loads inventory ingredients.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:209:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:209:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.ApplyFilter()
- **Lines:** [209-227]
- **Behavior:** Filters the cached _allRecipes list based on SearchText (matching product name, recipe name, or category) and FilterIndex criteria (all, incomplete cost, or uncosted). Populates the observable Recipes collection and notifies HasNoMatches.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:212:41:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:212:41:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [212-222]
- **Behavior:** Predicate lambda evaluating whether a RecipeRow matches current text search terms and filter completeness index.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:234:50:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:234:50:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.CostLineRow.Line.get
- **Lines:** [234-234]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.CostLineRow.Line.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:234:55:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:234:55:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.CostLineRow.Line.init
- **Lines:** [234-234]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.CostLineRow.Line.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:289:60:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:289:60:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.DetailLines.get
- **Lines:** [289-289]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.DetailLines.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:299:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:299:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.OnSelectedRecipeChanged(Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.RecipeRow?)
- **Lines:** [299-305]
- **Behavior:** Partial change handler invoked when SelectedRecipe changes. Notifies HasSelection, cancels any in-progress edit mode (unless _reselecting is true), and initiates asynchronous loading of recipe cost breakdown via LoadDetailAsync().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:307:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:307:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.OnDetailChanged(Falah.Application.Inventory.RecipeCostDetailDto?)
- **Lines:** [307-329]
- **Behavior:** Partial change handler invoked when Detail DTO changes. Populates DetailLines collection with CostLineRow wrappers and raises PropertyChanged for all calculated financial, packaging, margin, and completeness UI properties.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:383:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:383:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.LoadDetailAsync()
- **Lines:** [383-407]
- **Behavior:** Asynchronously retrieves detailed recipe costing breakdown from _session.Api.GetRecipeCostDetailAsync using current recipe ID and user ID. Sets Detail or displays DetailError on failure.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:415:50:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:415:50:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.InventoryItemId.get
- **Lines:** [415-415]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.InventoryItemId.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:415:55:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:415:55:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.InventoryItemId.init
- **Lines:** [415-415]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.InventoryItemId.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:416:43:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:416:43:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.ItemName.get
- **Lines:** [416-416]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.ItemName.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:416:48:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:416:48:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.ItemName.init
- **Lines:** [416-416]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.ItemName.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:417:39:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:417:39:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.Unit.get
- **Lines:** [417-417]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.Unit.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:417:44:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:417:44:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.Unit.init
- **Lines:** [417-417]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.Unit.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:425:9:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:425:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine.OnKindIndexChanged(int)
- **Lines:** [425-425]
- **Behavior:** Partial change handler notifying PropertyChanged for IsYield whenever the ingredient line kind index changes (0: Direct, 1: Yield/Per-piece, 2: Packaging).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:430:61:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:430:61:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditorLines.get
- **Lines:** [430-430]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditorLines.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:431:65:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:431:65:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.Ingredients.get
- **Lines:** [431-431]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.Ingredients.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:441:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:441:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.OnIngredientSearchTextChanged(string)
- **Lines:** [441-441]
- **Behavior:** Partial change handler invoked when IngredientSearchText changes, calling ApplyIngredientFilter() to filter available ingredients.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:443:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:443:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.ApplyIngredientFilter()
- **Lines:** [443-453]
- **Behavior:** Filters the cached _allIngredients list against IngredientSearchText, excluding items already present in EditorLines, and updates the observable Ingredients collection with up to 60 matches.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:446:41:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:446:41:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [446-446]
- **Behavior:** LINQ selector projection extracting InventoryItemId from EditorLines to create a hash set of already-added ingredients.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:449:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:449:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [449-450]
- **Behavior:** LINQ predicate filtering active inventory ingredients not in chosen set and matching search text case-insensitively.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:457:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:457:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.LoadIngredientsAsync()
- **Lines:** [457-468]
- **Behavior:** Asynchronously queries active raw inventory items from _session.Api.GetInventoryItemsAsync() to populate _allIngredients cache, then applies ingredient filtering.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:472:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:472:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.StartEdit()
- **Lines:** [472-493]
- **Behavior:** RelayCommand entering recipe edit mode. Clones current Detail lines into EditorLines with editable quantity and kind selections, sets IsEditing = true, refreshes ingredient picker, and notifies CanStartEdit.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:495:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:495:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.CancelEdit()
- **Lines:** [495-502]
- **Behavior:** RelayCommand canceling edit mode, resetting IsEditing to false, clearing SaveError and EditorLines, and notifying CanStartEdit.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:504:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:504:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.AddIngredient()
- **Lines:** [504-521]
- **Behavior:** RelayCommand adding the selected IngredientToAdd to EditorLines if not already added. Sets default kind to Packaging if item is packaging, clears picker selection and search text, and reapplies ingredient filter.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:508:29:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:508:29:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [508-508]
- **Behavior:** Inline expression lambda 'if (EditorLines.Any(l => l.InventoryItemId == item.Id)) return;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:523:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:523:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.RemoveLine(Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.EditableLine?)
- **Lines:** [523-529]
- **Behavior:** RelayCommand removing a line from EditorLines and updating the ingredient picker filter so the item becomes available again.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:531:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:531:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.SaveAsync()
- **Lines:** [531-600]
- **Behavior:** RelayCommand validating and saving edited recipe. Validates non-empty lines, parses numeric quantities via MilliUnits.TryParse, validates and parses yield ratios for per-piece lines, constructs SaveRecipeRequest with idempotency key, and calls _session.Api.SaveRecipeAsync. On success, exits edit mode, reloads recipe detail via LoadDetailAsync(), and refreshes summary list via RefreshSummaryAsync().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:604:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:604:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.RefreshSummaryAsync()
- **Lines:** [604-632]
- **Behavior:** Asynchronously refreshes recipe cost summaries and coverage statistics from backend after saving. Restores previous recipe selection using _reselecting flag to prevent unnecessary detail reload flicker or edit disruption.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:612:57:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:612:57:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [612-612]
- **Behavior:** Primary sorting key selector delegate `s => s.CategoryName` ordering refreshed recipe cost summaries alphabetically by category name.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:612:85:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:612:85:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [612-612]
- **Behavior:** Secondary sorting key selector delegate `s => s.ProductName` ordering refreshed recipe cost summaries alphabetically by product name.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:618:48:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs:618:48:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [618-618]
- **Behavior:** Inline expression lambda 'var again = Recipes.FirstOrDefault(r => r.Cost.RecipeId == selectedId);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:57:45Z
- **Source SHA-256 Verified:** `24d7807c2ca0ccf99aea7cf633ccfe99655de6acf2d0c19133b45060e90fdc63`
- **Callable ID Checked:** All 45 callables in `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected client source file `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs`. Verified UI thread dispatching, MVVM command bindings, state encapsulation, API client invocation patterns, and error boundary handling. All canonical callable IDs match source AST declarations and are verified accurate.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:57:51Z
- **Source SHA-256 Verified:** `24d7807c2ca0ccf99aea7cf633ccfe99655de6acf2d0c19133b45060e90fdc63`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `24d7807c2ca0ccf99aea7cf633ccfe99655de6acf2d0c19133b45060e90fdc63` against client source `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs`. All 45 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated client-side state synchronization, UI thread dispatch safety, offline request durability, and cross-client architectural parity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 45 callables.


## Candidate Findings & Observations
- **Potential NullReferenceException on User Context Access (lines 130, 606):**
  In `LoadAsync` (line 130) and `RefreshSummaryAsync` (line 606), `var actor = _session.CurrentUser!.Id;` dereferences `CurrentUser` using the null-forgiving operator (`!`). If `LoadAsync()` is triggered while the user session is expired, uninitialized, or being refreshed, this call will throw an unhandled `NullReferenceException` rather than surfacing a clean error state or redirecting to the login flow.
