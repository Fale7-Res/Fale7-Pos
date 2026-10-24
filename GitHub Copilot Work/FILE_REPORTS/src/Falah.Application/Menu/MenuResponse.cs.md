# src/Falah.Application/Menu/MenuResponse.cs

Menu API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `62ac4d9b795c665df3b3f5bd57ac10bc8aacd213f9f035969d8df1c682ea45ef`. Working SHA-256 `499f509a1b7d9b7b262f14208e132cff996a751a0d87a25c327846826daa1858`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-25.

Symbols indexed (index is not a claim every method was reviewed): MenuOfferDto@6, MenuVariantDto@8, MenuProductDto@10, MenuCategoryDto@17, MenuResponse@19.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 6: `public sealed record MenuOfferDto(string Id, string Name, long FixedPricePiastres);`
- Line 8: `public sealed record MenuVariantDto(string Id, string Name, long PricePiastres);`
- Line 10: `public sealed record MenuProductDto(`
- Line 17: `public sealed record MenuCategoryDto(string Id, string Name, string? Icon, IReadOnlyList<MenuProductDto> Products);`
- Line 19: `public sealed record MenuResponse(`

Client menu prices are display-only and checkout sends product/variant references for server resolution. Images use version stamps rather than inline bytes in menu reads; upload accepts base64 and requires size/type checks in handler. Full variant replacement requires historical snapshot isolation in persistence.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
