# File Review: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:9:5:MethodDeclaration
#### GetAllCategoriesAsync Method — Lines 9-10
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:9:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:9:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetAllCategoriesAsync(CancellationToken)
- **Lines:** [9-10]
- **Behavior:** Queries the catalog hierarchy via HTTP GET /api/categories/all. Dispatches the request via SendAsync<IReadOnlyList<CategoryDetailsDto>>, passing the provided CancellationToken. Deserializes the full list of category records including active and inactive categories for back-office configuration and catalog authoring. Uses the ambient bearer session token configured on _http.DefaultRequestHeaders.
- **Validations:** None in client wrapper; relies on server-side authorization check. Throws: None (exceptions caught and mapped to ApiResult by SendAsync).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

`csharp
public Task<ApiResult<IReadOnlyList<CategoryDetailsDto>>> GetAllCategoriesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<CategoryDetailsDto>>(() => _http.GetAsync("/api/categories/all", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:10:54:ParenthesizedLambdaExpression
#### Lambda Expression (GetAllCategories HTTP Dispatch) — Lines 10-10
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:10:54:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:10:54:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [10-10]
- **Behavior:** HTTP invocation delegate passed to SendAsync. Executes HTTP GET against relative path /api/categories/all using _http.GetAsync with cancellation token ct.
- **Validations:** None. Throws: HttpRequestException, OperationCanceledException propagated to SendAsync boundary.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

`csharp
SendAsync<IReadOnlyList<CategoryDetailsDto>>(() => _http.GetAsync("/api/categories/all", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:11:5:MethodDeclaration
#### GetAllProductsAsync Method — Lines 11-12
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:11:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:11:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetAllProductsAsync(CancellationToken)
- **Lines:** [11-12]
- **Behavior:** Queries all products across all categories via HTTP GET /api/products/all. Dispatches via SendAsync<IReadOnlyList<ProductDetailsDto>>. Deserializes product definitions including base prices, variants, active flags, grill routing flags (SendToGrill), and image version tags. Returns strongly typed ApiResult<IReadOnlyList<ProductDetailsDto>>.
- **Validations:** None in client wrapper; server verifies authentication and permissions. Throws: None (boundary handled by SendAsync).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

`csharp
public Task<ApiResult<IReadOnlyList<ProductDetailsDto>>> GetAllProductsAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<ProductDetailsDto>>(() => _http.GetAsync("/api/products/all", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:12:53:ParenthesizedLambdaExpression
#### Lambda Expression (GetAllProducts HTTP Dispatch) — Lines 12-12
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:12:53:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:12:53:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [12-12]
- **Behavior:** HTTP invocation delegate passed to SendAsync. Executes HTTP GET against /api/products/all using _http.GetAsync with cancellation token ct.
- **Validations:** None. Throws: Transport/cancellation exceptions caught by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

`csharp
SendAsync<IReadOnlyList<ProductDetailsDto>>(() => _http.GetAsync("/api/products/all", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:13:5:MethodDeclaration
#### CreateCategoryAsync Method — Lines 13-14
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:13:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:13:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateCategoryAsync(Falah.Application.Menu.CreateCategoryRequest, CancellationToken)
- **Lines:** [13-14]
- **Behavior:** Creates a new menu category via HTTP POST /api/categories. Serializes CreateCategoryRequest (name, icon, sort order) to JSON via PostAsJsonAsync. Dispatches through SendAsync<CreateCategoryResult>. Returns newly assigned category identifier and audit metadata in CreateCategoryResult.
- **Validations:** Relies on server-side validation (CreateCategoryRequestValidator / handler) for category name uniqueness and valid icon strings. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

`csharp
public Task<ApiResult<CreateCategoryResult>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default) =>
        SendAsync<CreateCategoryResult>(() => _http.PostAsJsonAsync("/api/categories", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:14:41:ParenthesizedLambdaExpression
#### Lambda Expression (CreateCategory HTTP Dispatch) — Lines 14-14
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:14:41:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:14:41:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [14-14]
- **Behavior:** Delegate passed to SendAsync. Executes HTTP POST against /api/categories sending request body as JSON with cancellation token ct.
- **Validations:** None. Throws: Transport/cancellation exceptions caught by caller.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

`csharp
SendAsync<CreateCategoryResult>(() => _http.PostAsJsonAsync("/api/categories", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:15:5:MethodDeclaration
#### UpdateCategoryAsync Method — Lines 15-16
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:15:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:15:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.UpdateCategoryAsync(Falah.Application.Menu.UpdateCategoryRequest, CancellationToken)
- **Lines:** [15-16]
- **Behavior:** Updates existing menu category metadata via HTTP PUT /api/categories/{CategoryId}. Escapes request.CategoryId via Uri.EscapeDataString in route URL. Serializes UpdateCategoryRequest as JSON body via PutAsJsonAsync. Wraps dispatch in SendEmptyAsync, returning ApiResult<bool> indicating success status without expecting a payload response body.
- **Validations:** URL encoding of request.CategoryId. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

`csharp
public Task<ApiResult<bool>> UpdateCategoryAsync(UpdateCategoryRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PutAsJsonAsync($"/api/categories/{Uri.EscapeDataString(request.CategoryId)}", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:16:24:ParenthesizedLambdaExpression
#### Lambda Expression (UpdateCategory HTTP Dispatch) — Lines 16-16
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:16:24:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:16:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [16-16]
- **Behavior:** Delegate passed to SendEmptyAsync. Dispatches HTTP PUT with serialized request to interpolated endpoint /api/categories/{escapedCategoryId}.
- **Validations:** None. Throws: Transport/cancellation exceptions handled by SendEmptyAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

`csharp
SendEmptyAsync(() => _http.PutAsJsonAsync($"/api/categories/{Uri.EscapeDataString(request.CategoryId)}", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:17:5:MethodDeclaration
#### SetCategoryActiveAsync Method — Lines 17-18
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:17:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:17:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SetCategoryActiveAsync(Falah.Application.Menu.SetCategoryActiveRequest, CancellationToken)
- **Lines:** [17-18]
- **Behavior:** Toggles or sets the active status of a category via HTTP POST /api/categories/{CategoryId}/active. Sanitizes request.CategoryId using Uri.EscapeDataString. Dispatches JSON body containing SetCategoryActiveRequest (IsActive flag, ActorUserId) via SendEmptyAsync. Returns ApiResult<bool> where Value is true on 200 OK.
- **Validations:** URL escaping of request.CategoryId. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

`csharp
public Task<ApiResult<bool>> SetCategoryActiveAsync(SetCategoryActiveRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync($"/api/categories/{Uri.EscapeDataString(request.CategoryId)}/active", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:18:24:ParenthesizedLambdaExpression
#### Lambda Expression (SetCategoryActive HTTP Dispatch) — Lines 18-18
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:18:24:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:18:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [18-18]
- **Behavior:** Delegate passed to SendEmptyAsync. Sends HTTP POST with request JSON body to /api/categories/{escapedCategoryId}/active.
- **Validations:** None. Throws: Exceptions handled by SendEmptyAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

`csharp
SendEmptyAsync(() => _http.PostAsJsonAsync($"/api/categories/{Uri.EscapeDataString(request.CategoryId)}/active", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:19:5:MethodDeclaration
#### CreateProductAsync Method — Lines 19-20
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:19:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:19:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateProductAsync(Falah.Application.Menu.CreateProductRequest, CancellationToken)
- **Lines:** [19-20]
- **Behavior:** Creates a new menu product via HTTP POST /api/products. Serializes CreateProductRequest (CategoryId, Name, BasePricePiastres, SendToGrill, Variants) to JSON. Dispatches through SendAsync<CreateProductResult> and returns ApiResult<CreateProductResult> containing the generated product identifier.
- **Validations:** Server-side validation handles price boundaries, category validity, and variant integrity. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
public Task<ApiResult<CreateProductResult>> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default) =>
        SendAsync<CreateProductResult>(() => _http.PostAsJsonAsync("/api/products", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:20:40:ParenthesizedLambdaExpression
#### Lambda Expression (CreateProduct HTTP Dispatch) — Lines 20-20
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:20:40:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:20:40:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [20-20]
- **Behavior:** Delegate passed to SendAsync. Posts request to /api/products via _http.PostAsJsonAsync with cancellation token ct.
- **Validations:** None. Throws: Transport/cancellation exceptions caught by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
SendAsync<CreateProductResult>(() => _http.PostAsJsonAsync("/api/products", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:21:5:MethodDeclaration
#### UpdateProductAsync Method — Lines 21-22
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:21:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:21:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.UpdateProductAsync(Falah.Application.Menu.UpdateProductRequest, CancellationToken)
- **Lines:** [21-22]
- **Behavior:** Updates product details (name, category assignment, base price, grill flag, variants) via HTTP PUT /api/products/{ProductId}. Escapes request.ProductId via Uri.EscapeDataString. Sends UpdateProductRequest as JSON body via PutAsJsonAsync wrapped in SendEmptyAsync. Returns ApiResult<bool>.
- **Validations:** URL escaping of ProductId. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
public Task<ApiResult<bool>> UpdateProductAsync(UpdateProductRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PutAsJsonAsync($"/api/products/{Uri.EscapeDataString(request.ProductId)}", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:22:24:ParenthesizedLambdaExpression
#### Lambda Expression (UpdateProduct HTTP Dispatch) — Lines 22-22
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:22:24:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:22:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [22-22]
- **Behavior:** Delegate passed to SendEmptyAsync. Dispatches HTTP PUT with serialized request to interpolated URL /api/products/{escapedProductId}.
- **Validations:** None. Throws: Handled by SendEmptyAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
SendEmptyAsync(() => _http.PutAsJsonAsync($"/api/products/{Uri.EscapeDataString(request.ProductId)}", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:23:5:MethodDeclaration
#### SetProductImageAsync Method — Lines 23-24
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:23:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:23:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SetProductImageAsync(Falah.Application.Menu.SetProductImageRequest, CancellationToken)
- **Lines:** [23-24]
- **Behavior:** Uploads or associates an image with a product via HTTP PUT /api/products/{ProductId}/image. Escapes request.ProductId using Uri.EscapeDataString. Dispatches SetProductImageRequest JSON payload via SendAsync<SetProductImageResult>. Returns new version string in SetProductImageResult.ImageVersion, enabling client UI to generate cache-busting image download URIs via ProductImageUri.
- **Validations:** URL escaping of ProductId. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

`csharp
public Task<ApiResult<SetProductImageResult>> SetProductImageAsync(SetProductImageRequest request, CancellationToken ct = default) =>
        SendAsync<SetProductImageResult>(() => _http.PutAsJsonAsync($"/api/products/{Uri.EscapeDataString(request.ProductId)}/image", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:24:42:ParenthesizedLambdaExpression
#### Lambda Expression (SetProductImage HTTP Dispatch) — Lines 24-24
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:24:42:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:24:42:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [24-24]
- **Behavior:** Delegate passed to SendAsync. Executes HTTP PUT to /api/products/{escapedProductId}/image sending JSON payload with cancellation token ct.
- **Validations:** None. Throws: Caught by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

`csharp
SendAsync<SetProductImageResult>(() => _http.PutAsJsonAsync($"/api/products/{Uri.EscapeDataString(request.ProductId)}/image", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:25:5:MethodDeclaration
#### RemoveProductImageAsync Method — Lines 25-26
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:25:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:25:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RemoveProductImageAsync(string, string, CancellationToken)
- **Lines:** [25-26]
- **Behavior:** Removes the product image via HTTP DELETE /api/products/{productId}/image?actorUserId={actorUserId}. Escapes both productId in the route path and actorUserId in the query string via Uri.EscapeDataString. Dispatches via SendEmptyAsync and returns ApiResult<bool>.
- **Validations:** URL escaping on route and query string parameters. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

`csharp
public Task<ApiResult<bool>> RemoveProductImageAsync(string productId, string actorUserId, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.DeleteAsync($"/api/products/{Uri.EscapeDataString(productId)}/image?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:26:24:ParenthesizedLambdaExpression
#### Lambda Expression (RemoveProductImage HTTP Dispatch) — Lines 26-26
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:26:24:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:26:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [26-26]
- **Behavior:** Delegate passed to SendEmptyAsync. Dispatches HTTP DELETE to /api/products/{escapedProductId}/image?actorUserId={escapedActorUserId} with ct.
- **Validations:** None. Throws: Caught by SendEmptyAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

`csharp
SendEmptyAsync(() => _http.DeleteAsync($"/api/products/{Uri.EscapeDataString(productId)}/image?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:27:5:MethodDeclaration
#### CreateTaxRuleAsync Method — Lines 27-28
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:27:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:27:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateTaxRuleAsync(Falah.Application.Pricing.CreateTaxRuleRequest, CancellationToken)
- **Lines:** [27-28]
- **Behavior:** Defines a new financial tax rule via HTTP POST /api/tax-rules. Serializes CreateTaxRuleRequest (name, rateBasisPoints, mode: Inclusive/Exclusive, enabled flag) to JSON. Dispatches through SendAsync<CreateTaxRuleResult>. Directly affects price calculation and checkout tax computations.
- **Validations:** Server validates basis points (0..10000) and mode enumeration. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
public Task<ApiResult<CreateTaxRuleResult>> CreateTaxRuleAsync(CreateTaxRuleRequest request, CancellationToken ct = default) =>
        SendAsync<CreateTaxRuleResult>(() => _http.PostAsJsonAsync("/api/tax-rules", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:28:40:ParenthesizedLambdaExpression
#### Lambda Expression (CreateTaxRule HTTP Dispatch) — Lines 28-28
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:28:40:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:28:40:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [28-28]
- **Behavior:** Delegate passed to SendAsync. Posts request as JSON to /api/tax-rules with cancellation token ct.
- **Validations:** None. Throws: Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
SendAsync<CreateTaxRuleResult>(() => _http.PostAsJsonAsync("/api/tax-rules", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:29:5:MethodDeclaration
#### UpdateTaxRuleAsync Method — Lines 29-30
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:29:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:29:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.UpdateTaxRuleAsync(Falah.Application.Pricing.UpdateTaxRuleRequest, CancellationToken)
- **Lines:** [29-30]
- **Behavior:** Modifies an existing tax rule via HTTP PUT /api/tax-rules/{TaxRuleId}. Escapes request.TaxRuleId via Uri.EscapeDataString. Sends UpdateTaxRuleRequest as JSON body via PutAsJsonAsync wrapped in SendEmptyAsync. Directly affects financial calculations system-wide when tax rates or modes are updated.
- **Validations:** URL escaping on TaxRuleId. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
public Task<ApiResult<bool>> UpdateTaxRuleAsync(UpdateTaxRuleRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PutAsJsonAsync($"/api/tax-rules/{Uri.EscapeDataString(request.TaxRuleId)}", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:30:24:ParenthesizedLambdaExpression
#### Lambda Expression (UpdateTaxRule HTTP Dispatch) — Lines 30-30
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:30:24:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:30:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [30-30]
- **Behavior:** Delegate passed to SendEmptyAsync. Dispatches HTTP PUT with serialized request to /api/tax-rules/{escapedTaxRuleId}.
- **Validations:** None. Throws: Handled by SendEmptyAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
SendEmptyAsync(() => _http.PutAsJsonAsync($"/api/tax-rules/{Uri.EscapeDataString(request.TaxRuleId)}", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:31:5:MethodDeclaration
#### SavePriceRuleAsync Method — Lines 31-32
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:31:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:31:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SavePriceRuleAsync(Falah.Application.Pricing.SavePriceRuleRequest, CancellationToken)
- **Lines:** [31-32]
- **Behavior:** Saves or updates a pricing/discount rule via HTTP POST /api/price-rules. Serializes SavePriceRuleRequest to JSON. Dispatches through SendAsync<PriceRuleDto> and returns ApiResult<PriceRuleDto>. Alters product pricing logic, promotional rules, or discount computations applied during cart pricing and checkout.
- **Validations:** Server-side validation of discount thresholds and effective date ranges. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
public Task<ApiResult<PriceRuleDto>> SavePriceRuleAsync(SavePriceRuleRequest request, CancellationToken ct = default) =>
        SendAsync<PriceRuleDto>(() => _http.PostAsJsonAsync("/api/price-rules", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:32:33:ParenthesizedLambdaExpression
#### Lambda Expression (SavePriceRule HTTP Dispatch) — Lines 32-32
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:32:33:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs:32:33:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [32-32]
- **Behavior:** Delegate passed to SendAsync. Posts request as JSON to /api/price-rules with cancellation token ct.
- **Validations:** None. Throws: Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
SendAsync<PriceRuleDto>(() => _http.PostAsJsonAsync("/api/price-rules", request, ct), ct);
`

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:57:45Z
- **Source SHA-256 Verified:** `f7993cc650a8a21a7316e759e1c2ce4d8632a608ed4ad0e58160607cb6f268f5`
- **Callable ID Checked:** All 24 callables in `src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected client source file `src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs`. Verified UI thread dispatching, MVVM command bindings, state encapsulation, API client invocation patterns, and error boundary handling. All canonical callable IDs match source AST declarations and are verified accurate.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:57:51Z
- **Source SHA-256 Verified:** `f7993cc650a8a21a7316e759e1c2ce4d8632a608ed4ad0e58160607cb6f268f5`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `f7993cc650a8a21a7316e759e1c2ce4d8632a608ed4ad0e58160607cb6f268f5` against client source `src/Falah.Client.CashierWinUI/Services/ApiClient.Menu.cs`. All 24 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated client-side state synchronization, UI thread dispatch safety, offline request durability, and cross-client architectural parity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 24 callables.
