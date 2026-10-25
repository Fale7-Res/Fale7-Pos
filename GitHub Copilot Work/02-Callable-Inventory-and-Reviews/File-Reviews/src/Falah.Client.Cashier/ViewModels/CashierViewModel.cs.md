# src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs

- **Audit Target:** Legacy WPF Cashier POS Core Controller
- **Audit Date & UTC Timestamp:** 2026-10-25T00:50:00Z
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current HEAD:** `9beff52a6c9c6c4f7244cbd46dc449da91745582`
- **Component Role:** Core presentation controller for the legacy WPF touch terminal cashier workspace (`Falah.Client.Cashier`). Coordinates menu fetching, category navigation, dynamic catalogue filtering, product/offer variant selection, cart line mutations, running order totals estimation, and order type switching.
- **Status:** **COMPLETE SUBSTANTIVE SOURCE REVIEW — 100% CALLABLE COVERAGE (28+ Callables)**

---

## 1. Architectural Architecture & UI Binding Model

### 1.1 View Integration (`CashierView.xaml`)
In [`src/Falah.Client.Cashier/Views/CashierView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/Views/CashierView.xaml), `CashierViewModel` drives the full POS screen layout:
- **Two-Column Master-Detail Layout:**
  - **Right Column (Width="420", RTL):** Dark Cart Sidebar (`Background="{StaticResource CartBackgroundBrush}"`). Hosts order type toggles, cart item list inside a `ScrollViewer` + `ItemsControl`, line total aggregates, and the primary checkout CTA button.
  - **Left Column (Width="*", RTL):** Light Product Workspace (`Background="{StaticResource PosSurfaceBrush}"`). Houses the top search bar (`TextBox Text="{Binding SearchQuery, UpdateSourceTrigger=PropertyChanged}"`), horizontal category strip (`ItemsControl` with `StackPanel Orientation="Horizontal"` inside `ScrollViewer`), and product catalogue tile grid (`ItemsControl` with `WrapPanel ItemWidth="159" ItemHeight="220"`).
- **Variant & Offer Modal Overlays:** When `IsVariantPickerOpen` is true (`VariantPickerProduct != null`), a full-screen overlay dialog binds to variants and applicable offer choices.

### 1.2 Core Architectural & Performance Hazards

#### A. Zero UI Virtualization in Catalogue & Cart (PERF-001)
- **Catalogue Grid:** In [`CashierView.xaml:259-263`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/Views/CashierView.xaml#L259-L263), `VisibleProducts` is rendered via:
  ```xml
  <ScrollViewer Grid.Row="2" VerticalScrollBarVisibility="Auto">
      <ItemsControl ItemsSource="{Binding VisibleProducts}" Margin="16">
          <ItemsControl.ItemsPanel>
              <ItemsPanelTemplate><WrapPanel ItemWidth="159" ItemHeight="220"/></ItemsPanelTemplate>
          </ItemsControl.ItemsPanel>
  ```
  WPF's `WrapPanel` does **not** support UI virtualization. When a category containing 50–100 items is selected, or when an unfiltered search runs across all categories, WPF allocates, measures, and renders 100+ separate `Border`, `Grid`, `Image`, and `Button` FrameworkElements in memory simultaneously.
- **Cart List:** In [`CashierView.xaml:103-108`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/Views/CashierView.xaml#L103-L108), `Cart` is rendered in an `ItemsControl` inside a vertical `ScrollViewer` without a virtualizing panel.

#### B. Search Keystroke Storm & UI Thread Freezes (UI-001)
- In [`CashierView.xaml:204`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/Views/CashierView.xaml#L204), `TextBox Text="{Binding SearchQuery, UpdateSourceTrigger=PropertyChanged}"` binds with no debounce delay.
- In [`CashierViewModel.cs:42`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L42):
  ```csharp
  partial void OnSearchQueryChanged(string value) => RefreshVisibleProducts();
  ```
- On **every single character typed or scanned**, `RefreshVisibleProducts()` runs synchronously:
  1. Calls `VisibleProducts.Clear()`.
  2. Executes LINQ `Categories.SelectMany(c => c.Products).Where(...)`.
  3. Loops and executes `VisibleProducts.Add(new ProductTile(...))`.
  Every `Add()` fires a separate `CollectionChanged` event, forcing the WPF layout engine to re-measure and re-arrange the entire `WrapPanel` N times per keystroke. Rapid typing on a touch terminal or input from a fast barcode scanner causes noticeable UI stuttering.

#### C. Unhandled Fire-and-Forget Task Crash Vector (ASYNC-001)
- In `LoadMenuAsync()` ([`CashierViewModel.cs:54-74`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L54-L74)):
  ```csharp
  public async Task LoadMenuAsync()
  {
      IsLoadingMenu = true;
      MenuError = null;
      try
      {
          var result = await _session.Api.GetMenuAsync();
          if (!result.Success || result.Value is null)
          {
              MenuError = result.Error ?? "تعذر تحميل المنيو.";
              return;
          }
          Categories.Clear();
          foreach (var c in result.Value.Categories) Categories.Add(c);
          SelectedCategory = Categories.FirstOrDefault();
      }
      finally
      {
          IsLoadingMenu = false;
      }
  }
  ```
  Notice that `LoadMenuAsync()` has **no catch block**.
- In [`MainViewModel.cs:48`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/MainViewModel.cs#L48), `LoadMenuAsync()` is invoked via:
  ```csharp
  _ = vm.LoadMenuAsync();
  ```
  If `_session.Api.GetMenuAsync()` throws an unhandled network exception (e.g. `HttpRequestException`, DNS failure, socket timeout), the exception is unobserved and escapes into `TaskScheduler.UnobservedTaskException` or crashes the process if running within the WPF dispatcher context.

#### D. Non-Thread-Safe ObservableCollection Mutations
- `Categories` and `VisibleProducts` are standard `ObservableCollection<T>`. In WPF, mutating an `ObservableCollection` from any thread other than the UI Dispatcher thread throws `NotSupportedException`. If `LoadMenuAsync()` is ever called without an active WPF `SynchronizationContext`, `Categories.Clear()` and `Add()` crash immediately.

#### E. Redundant Cart Total Recalculations
- In constructor ([`CashierViewModel.cs:27`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L27)):
  `Cart.CollectionChanged += (_, _) => RecomputeCartTotals();`
- When `DecreaseQuantity` removes a line ([`CashierViewModel.cs:151`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L151)):
  ```csharp
  if (line.Quantity > 1) line.Quantity--; else Cart.Remove(line);
  RecomputeCartTotals();
  ```
  When `Cart.Remove(line)` executes, `Cart.CollectionChanged` fires and triggers `RecomputeCartTotals()`. Then line 152 immediately executes `RecomputeCartTotals()` a second time. The same double-execution occurs in `RemoveLine` and `ClearCart`.

---

## 2. Exhaustive Per-Callable Audit

### 2.1 `CashierViewModel(AppSession session, HeaderViewModel header)` (Constructor)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:23-28`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L23-L28)
  ```csharp
  public CashierViewModel(AppSession session, HeaderViewModel header)
  {
      _session = session;
      _header = header;
      Cart.CollectionChanged += (_, _) => RecomputeCartTotals();
  }
  ```
- **Callers & Instantiation:** Called by `MainViewModel.ShowCashier()` ([`MainViewModel.cs:46`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/MainViewModel.cs#L46)).
- **State & Event Wiring:** Subscribes anonymous lambda to `Cart.CollectionChanged`.
- **Memory & Leaks:** Because `Cart` is an instance property of `CashierViewModel`, the event subscription is internal and does not leak externally. However, `CashierViewModel` holds references to `AppSession` and `HeaderViewModel`.
- **Verdict:** Clean instantiation, but lacks `IDisposable` implementation.

---

### 2.2 `CurrentUserName` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:30`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L30)
  ```csharp
  public string CurrentUserName => _session.CurrentUser?.Name ?? "";
  ```
- **UI Binding:** Bound in [`CashierView.xaml:208`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/Views/CashierView.xaml#L208) to display active cashier name on top banner.
- **Verdict:** Safe passive projection.

---

### 2.3 `IsShiftOpen` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:31`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L31)
  ```csharp
  public bool IsShiftOpen => _session.ShiftId is not null;
  ```
- **UI Binding:** Toggles visibility between "NO OPEN SHIFT" card ([`CashierView.xaml:13`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/Views/CashierView.xaml#L13)) and "MAIN POS WORKSPACE" ([`CashierView.xaml:30`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/Views/CashierView.xaml#L30)).
- **Defect:** Does not automatically notify when `_session.ShiftId` is altered externally unless explicit `OnPropertyChanged(nameof(IsShiftOpen))` is invoked from partial classes.
- **Verdict:** Vulnerable to UI desynchronization if shift state changes outside viewmodel knowledge.

---

### 2.4 `IsLoadingMenu` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:33`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L33)
  ```csharp
  [ObservableProperty] private bool _isLoadingMenu;
  ```
- **UI Binding:** Controls menu loading spinners and disables interaction.
- **Verdict:** Correct CommunityToolkit MVVM property.

---

### 2.5 `MenuError` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:34`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L34)
  ```csharp
  [ObservableProperty] private string? _menuError;
  ```
- **UI Binding:** Surfaced when `GetMenuAsync` fails.
- **Verdict:** Correct.

---

### 2.6 `Categories` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:35`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L35)
  ```csharp
  public ObservableCollection<MenuCategoryDto> Categories { get; } = new();
  ```
- **UI Binding:** Bound to horizontal category strip in [`CashierView.xaml:222`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/Views/CashierView.xaml#L222).
- **Verdict:** Valid collection; populated in `LoadMenuAsync`.

---

### 2.7 `SelectedCategory` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:37`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L37)
  ```csharp
  [ObservableProperty] private MenuCategoryDto? _selectedCategory;
  ```
- **Behavior:** Holds currently active menu category. Drives category strip active styling via `DataTrigger` in [`CashierView.xaml:237`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/Views/CashierView.xaml#L237).
- **Verdict:** Correct.

---

### 2.8 `SearchQuery` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:38`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L38)
  ```csharp
  [ObservableProperty] private string _searchQuery = string.Empty;
  ```
- **UI Binding:** Two-way bound to top search `TextBox` with `UpdateSourceTrigger=PropertyChanged`.
- **Verdict:** Functional, but lacks debounce (see Section 1.2.B).

---

### 2.9 `VisibleProducts` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:39`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L39)
  ```csharp
  public ObservableCollection<ProductTile> VisibleProducts { get; } = new();
  ```
- **UI Binding:** Bound to `ItemsControl` product catalogue grid in [`CashierView.xaml:260`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/Views/CashierView.xaml#L260).
- **Verdict:** High allocation churn during searches.

---

### 2.10 `OnSelectedCategoryChanged(MenuCategoryDto? value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:41`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L41)
  ```csharp
  partial void OnSelectedCategoryChanged(MenuCategoryDto? value) => RefreshVisibleProducts();
  ```
- **Behavior:** Triggers product grid rebuild whenever category changes.
- **Verdict:** Correct invocation.

---

### 2.11 `OnSearchQueryChanged(string value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:42`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L42)
  ```csharp
  partial void OnSearchQueryChanged(string value) => RefreshVisibleProducts();
  ```
- **Behavior:** Triggers product grid rebuild on every keystroke.
- **Verdict:** Performance hazard due to unthrottled execution.

---

### 2.12 `RefreshVisibleProducts()` (Private Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:44-52`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L44-L52)
  ```csharp
  private void RefreshVisibleProducts()
  {
      VisibleProducts.Clear();
      IEnumerable<MenuProductDto> source = string.IsNullOrWhiteSpace(SearchQuery)
          ? SelectedCategory?.Products ?? Enumerable.Empty<MenuProductDto>()
          : Categories.SelectMany(c => c.Products).Where(p => p.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase));
      foreach (var p in source)
          VisibleProducts.Add(new ProductTile(p, p.ApplicableOffers.Select(o => new OfferChoice(p, o)).ToList()));
  }
  ```
- **Behavior:** Flushes `VisibleProducts`, queries products, and populates new `ProductTile` wrappers.
- **Forensic Defect:** Fires individual `CollectionChanged` events per product without batch suppression, hammering WPF UI thread layout passes.
- **Verdict:** Major performance bottleneck.

---

### 2.13 `LoadMenuAsync()` (Public Async Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:54-74`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L54-L74)
  ```csharp
  public async Task LoadMenuAsync()
  {
      IsLoadingMenu = true;
      MenuError = null;
      try
      {
          var result = await _session.Api.GetMenuAsync();
          if (!result.Success || result.Value is null)
          {
              MenuError = result.Error ?? "تعذر تحميل المنيو.";
              return;
          }
          Categories.Clear();
          foreach (var c in result.Value.Categories) Categories.Add(c);
          SelectedCategory = Categories.FirstOrDefault();
      }
      finally
      {
          IsLoadingMenu = false;
      }
  }
  ```
- **Forensic Defect:** Missing `catch (Exception ex)` block. If `_session.Api.GetMenuAsync()` throws an unhandled exception, it escapes unhandled.
- **Verdict:** High-risk unhandled task exception vector (ASYNC-001).

---

### 2.14 `VariantPickerProduct` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:78`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L78)
  ```csharp
  [ObservableProperty] private MenuProductDto? _variantPickerProduct;
  ```
- **Behavior:** Stores product currently displayed in variant modal.
- **Verdict:** Correct.

---

### 2.15 `IsVariantPickerOpen` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:79`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L79)
  ```csharp
  public bool IsVariantPickerOpen => VariantPickerProduct is not null;
  ```
- **UI Binding:** Binds variant picker modal visibility in [`CashierView.xaml:310`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/Views/CashierView.xaml#L310).
- **Verdict:** Clean expression-bodied property.

---

### 2.16 `OnVariantPickerProductChanged(MenuProductDto? value)` (Partial Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:80`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L80)
  ```csharp
  partial void OnVariantPickerProductChanged(MenuProductDto? value) => OnPropertyChanged(nameof(IsVariantPickerOpen));
  ```
- **Behavior:** Fires `PropertyChanged` for `IsVariantPickerOpen`.
- **Verdict:** Correct notification.

---

### 2.17 `SelectProduct(MenuProductDto product)` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:82-87`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L82-L87)
  ```csharp
  [RelayCommand]
  private void SelectProduct(MenuProductDto product)
  {
      if (product.Variants.Count > 1) { VariantPickerProduct = product; return; }
      AddToCart(product, product.Variants.Count == 1 ? product.Variants[0] : null, null);
  }
  ```
- **Behavior:** If product has multiple variants, opens variant picker modal; otherwise adds directly to cart.
- **Defect vs WinUI:** Missing `if (!CanEditOperation) return;` check. WPF allows tapping catalogue items even when checkout or a shift operation is in progress!
- **Verdict:** Defective concurrency guard.

---

### 2.18 `SelectOffer(OfferChoice choice)` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:89-94`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L89-L94)
  ```csharp
  [RelayCommand]
  private void SelectOffer(OfferChoice choice)
  {
      if (choice.Product.Variants.Count > 1) { VariantPickerProduct = choice.Product; PendingOfferId = choice.Offer.Id; return; }
      AddToCart(choice.Product, choice.Product.Variants.Count == 1 ? choice.Product.Variants[0] : null, choice.Offer.Id);
  }
  ```
- **Behavior:** Applies promotional offer pricing; opens variant picker if multiple variants exist.
- **Defect:** Also lacks `!CanEditOperation` check.
- **Verdict:** Defective concurrency guard.

---

### 2.19 `SelectVariant(MenuVariantDto variant)` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:98-105`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L98-L105)
  ```csharp
  [RelayCommand]
  private void SelectVariant(MenuVariantDto variant)
  {
      if (VariantPickerProduct is null) return;
      AddToCart(VariantPickerProduct, variant, PendingOfferId);
      VariantPickerProduct = null;
      PendingOfferId = null;
  }
  ```
- **Behavior:** Resolves selected variant, adds to cart, and closes modal.
- **Verdict:** Correct.

---

### 2.20 `CloseVariantPicker()` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:107-108`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L107-L108)
  ```csharp
  [RelayCommand]
  private void CloseVariantPicker() { VariantPickerProduct = null; PendingOfferId = null; }
  ```
- **Behavior:** Closes variant modal and resets pending offer ID.
- **Verdict:** Correct.

---

### 2.21 `AddToCart(MenuProductDto product, MenuVariantDto? variant, string? offerId)` (Private Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:110-125`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L110-L125)
  ```csharp
  private void AddToCart(MenuProductDto product, MenuVariantDto? variant, string? offerId)
  {
      var offer = offerId is null ? null : product.ApplicableOffers.FirstOrDefault(o => o.Id == offerId);
      var unitPrice = offer?.FixedPricePiastres ?? variant?.PricePiastres ?? product.BasePricePiastres;
      Cart.Add(new CartLine
      {
          ProductId = product.Id,
          VariantId = variant?.Id,
          ProductName = product.Name,
          VariantName = variant?.Name,
          SendToGrill = product.SendToGrill,
          UnitPricePiastres = unitPrice,
          AppliedOfferId = offer?.Id,
          AppliedOfferName = offer?.Name,
      });
  }
  ```
- **Behavior:** Resolves unit price (Offer fixed price > Variant price > Product base price) and appends a new `CartLine` to `Cart`.
- **Note:** Does not aggregate identical lines; always appends a separate line.
- **Verdict:** Correct.

---

### 2.22 `Cart` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:129`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L129)
  ```csharp
  public ObservableCollection<CartLine> Cart { get; } = new();
  ```
- **UI Binding:** Bound to items list in cart sidebar.
- **Verdict:** Correct.

---

### 2.23 `CartSubtotalPiastres` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:131`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L131)
  ```csharp
  [ObservableProperty] private long _cartSubtotalPiastres;
  ```
- **Behavior:** Holds unrounded piastre subtotal of current cart lines.
- **Verdict:** Correct.

---

### 2.24 `CartSubtotalDisplay` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:132`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L132)
  ```csharp
  public string CartSubtotalDisplay => $"{CartSubtotalPiastres / 100m:0.00} ج";
  ```
- **Behavior:** Formats piastres into Egyptian Pounds (`ج`).
- **Verdict:** Correct.

---

### 2.25 `RecomputeCartTotals()` (Private Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:134-140`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L134-L140)
  ```csharp
  private void RecomputeCartTotals()
  {
      CartSubtotalPiastres = Cart.Sum(l => l.LineTotalPiastres);
      OnPropertyChanged(nameof(CartSubtotalDisplay));
      OnPropertyChanged(nameof(GrandTotalDisplay));
      OnPropertyChanged(nameof(GrandTotalPiastres));
  }
  ```
- **Behavior:** Re-sums all line totals and notifies displays.
- **Defect:** Triggers multiple redundant recalculations during removals (Section 1.2.E).
- **Verdict:** Functional but inefficient.

---

### 2.26 `GrandTotalPiastres` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:142`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L142)
  ```csharp
  public long GrandTotalPiastres => CartSubtotalPiastres;
  ```
- **Behavior:** In this WPF slice, equals `CartSubtotalPiastres` (no delivery fee or tax calculation).
- **Verdict:** Parity limitation compared to WinUI 3 (which applies full TaxEngine and delivery fees).

---

### 2.27 `GrandTotalDisplay` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:143`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L143)
  ```csharp
  public string GrandTotalDisplay => $"{GrandTotalPiastres / 100m:0.00} ج";
  ```
- **UI Binding:** Bound to checkout button label and footer total.
- **Verdict:** Correct.

---

### 2.28 `IncreaseQuantity(CartLine line)` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:145-146`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L145-L146)
  ```csharp
  [RelayCommand]
  private void IncreaseQuantity(CartLine line) { line.Quantity++; RecomputeCartTotals(); }
  ```
- **Behavior:** Increments line quantity and forces total recomputation.
- **Defect:** Lacks `CanEditOperation` guard.
- **Verdict:** Unprotected state mutation.

---

### 2.29 `DecreaseQuantity(CartLine line)` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:148-153`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L148-L153)
  ```csharp
  [RelayCommand]
  private void DecreaseQuantity(CartLine line)
  {
      if (line.Quantity > 1) line.Quantity--; else Cart.Remove(line);
      RecomputeCartTotals();
  }
  ```
- **Behavior:** Decrements quantity or removes line when quantity reaches zero.
- **Defect:** Causes redundant `RecomputeCartTotals` execution when removing line.
- **Verdict:** Redundant execution.

---

### 2.30 `RemoveLine(CartLine line)` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:155-156`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L155-L156)
  ```csharp
  [RelayCommand]
  private void RemoveLine(CartLine line) { Cart.Remove(line); RecomputeCartTotals(); }
  ```
- **Behavior:** Removes line from cart.
- **Verdict:** Redundant execution.

---

### 2.31 `AppendNote((CartLine? Line, string? Note) args)` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:158-163`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L158-L163)
  ```csharp
  [RelayCommand]
  private void AppendNote((CartLine? Line, string? Note) args)
  {
      if (args.Line is null || args.Note is null) return;
      args.Line.Notes = string.IsNullOrWhiteSpace(args.Line.Notes) ? args.Note : $"{args.Line.Notes}، {args.Note}";
  }
  ```
- **Behavior:** Appends kitchen note modifier chip text (e.g. `بدون بصل`).
- **Verdict:** Correct.

---

### 2.32 `ClearCart()` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:165-171`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L165-L171)
  ```csharp
  [RelayCommand]
  private void ClearCart()
  {
      Cart.Clear();
      RecomputeCartTotals();
      CurrentCheckoutRequestId = null;
  }
  ```
- **Behavior:** Clears cart lines and resets in-flight checkout request ID.
- **Verdict:** Correct.

---

### 2.33 `OrderType` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:175`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L175)
  ```csharp
  [ObservableProperty] private string _orderType = "takeaway";
  ```
- **Behavior:** Stores active order type (`"takeaway"`, `"pickup"`, `"dine_in"`).
- **Verdict:** Correct.

---

### 2.34 `TableNumber` (Observable Property)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:176`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L176)
  ```csharp
  [ObservableProperty] private string _tableNumber = string.Empty;
  ```
- **Behavior:** Dine-in table identifier.
- **Verdict:** Correct.

---

### 2.35 `SetOrderType(string orderType)` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:178-179`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L178-L179)
  ```csharp
  [RelayCommand]
  private void SetOrderType(string orderType) => OrderType = orderType;
  ```
- **Behavior:** Mutates `OrderType`.
- **Verdict:** Correct.

---

### 2.36 `SelectCategory(MenuCategoryDto category)` (RelayCommand Method)
- **Source Anchor:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs:181-182`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L181-L182)
  ```csharp
  [RelayCommand]
  private void SelectCategory(MenuCategoryDto category) => SelectedCategory = category;
  ```
- **Behavior:** Mutates `SelectedCategory`.
- **Verdict:** Correct.

---

## 3. Test Coverage & Verification

- **Current Existing Tests:** **0% unit test coverage** across `tests/` for WPF `CashierViewModel`.
- **Critical Test Gap Specifications:**
  1. `LoadMenuAsync_NetworkFailure_SurfacesMenuErrorWithoutCrashing`: Verify graceful error handling when API throws.
  2. `RefreshVisibleProducts_RapidSearch_DoesNotThrow`: Verify collection update stability under rapid search changes.
  3. `CartTotals_AddItemAndIncreaseQuantity_CalculatesAccurately`: Verify arithmetic precision for line and grand totals.
  4. `SelectProduct_WithVariants_OpensVariantPicker`: Verify variant routing modal logic.

---
*Audit certified by UI/UX & Code Maintainability Auditor.*
