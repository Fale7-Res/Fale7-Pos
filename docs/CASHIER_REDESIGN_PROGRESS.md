# Cashier redesign implementation and acceptance

Latest cross-product verification: [FINAL_VERIFICATION_PROGRESS.md](FINAL_VERIFICATION_PROGRESS.md)
(2026-09-08). The historical test counts and acceptance notes below are retained
as the record of the earlier Cashier-only pass.

This continues the historical takeover audit in `UI_TAKEOVER_AUDIT.md`. The current Cashier screenshot was not used as a design reference. Existing contracts, bindings, and workflow code provided the functional starting point. Pre-existing Call Center work and backend source were preserved.

## Implemented

- Light white/sage POS composition: compact header, horizontal category chips, responsive product cards, a fixed light cart, operational toolbar, and a dedicated two-column payment view.
- Arabic labels, RTL layout, protected manager PIN fields, 44-pixel quantity controls, decimal keypad entry, first-key replacement of the default tender, long-name wrapping, scrollable operational forms, and fixed checkout confirmation.
- Customer lookup and saved addresses; delivery area is separate from the selected service. Services retry after failure and delivery fees notify totals. Delivery details occupy a separate form rather than the cart header.
- Egyptian/Persian digits and piastre-safe money parsing. Tax estimates reuse the existing domain tax calculation with the real tax-rules API. The server remains authoritative at checkout.
- Discount permission enforcement without unsupported manager-approval fields. Positive hospitality always supplies approval identifiers as required by the current server handler.
- Immutable requests retained after ambiguous responses for checkout, expenses, safe drops, cash movements, drawer requests, shift open/close, refunds, and reprints. Retry preserves identifiers and payloads. An uncertain operation blocks editing, cancellation, navigation, and switching users until resolved.
- Current-shift recovery through the API and per-user in-memory cashier drafts across lock/navigation. This is not disk persistence across process termination.
- Refund submission from order lookup and reopening pending electronic payment confirmation/cancellation. Pending and cash-on-delivery states do not claim money was collected.
- Drawer-balance inquiry now has an accurate label and permission gate. A physical denomination-count workflow and persisted cash-count record are still separate work; this screen does not claim to perform them.
- Reprint/drawer copy describes recorded requests, not confirmed physical hardware execution.
- Cashier/recipe/payroll navigation permission corrections and protection against closing a shift with an unfinished cart.

## Validation

- The full solution builds successfully, with one NU1900 warning described below.
- Domain tests: 255 passed.
- Client tests: 19 passed, including touch-keypad replacement of the default tender.
- Focused client tests compile the actual cashier, API, money parser, and model sources into a portable test assembly. Only the native header clock is substituted. Coverage includes money units, malformed responses, exact tender, tax rounding, request replay, permissions, refunds, and draft retention.
- NuGet vulnerability metadata could not be fetched from `api.nuget.org` in the restricted environment; restore/build reported NU1900 for the new test project. This is separate from compilation and test results.
- The actual WinUI executable launched and its PIN screen was inspected through Windows accessibility.

## Acceptance still pending

Native screenshot capture failed twice after fresh window selection with `SetIsBorderRequired failed: No such interface supported (0x80004002)`. The installed Computer Use skill prohibits automating authentication dialogs, so no PIN was entered. The new cashier screen has **not** received visual acceptance. Runtime checks with long Arabic product names, large catalogs/carts, touch interactions, and 100/125/150% Windows scaling remain required after sign-in and usable capture.

There is no product-photo mapping in the existing menu contract. Cards currently use names, prices, variant hints, and a small neutral icon. No stock photos or fabricated product imagery were presented as real menu data.

The historical SQL integration suite was not rerun for this client-only change. Its earlier flaky failures and rerun evidence remain recorded in the takeover audit; this work does not claim the entire backend suite newly passed.

The wider management modules and pre-existing Call Center project have not been declared complete by this cashier pass.
