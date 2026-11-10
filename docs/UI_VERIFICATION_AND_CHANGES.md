# UI verification and changes — 2026-11-07

## How the screens were run

**Server and data**
- The real `Falah.Server` was started against the disposable database `FalahTest_ui_cd466ccd7ab14db0879cd62735b60429`. The launch script `start-test-backend.ps1` refuses to start unless the name begins with `FalahTest_`.
- Data and log files are under `E:\SqlData\Tests`, with automatic backup off.
- A trading day was then pushed through the API by `seed-workflow.ps1`: 8 orders paid by cash, card, InstaPay (left unconfirmed), wallet (confirmed) and pickup; a partial refund; a capital deposit; employees; suppliers.

**Native clients**
- Both native clients were built x64 from the current source in the separate verification tree `E:\DevCaches\Temp\falah-codex-verify`.
- They were launched with process-scoped `FALAH_SERVER_URL=http://localhost:5799` and `FALAH_CLIENT_DATA_ROOT=E:\SqlData\Tests\ui-client-state`.
  - `FALAH_CLIENT_DATA_ROOT` is a new, tested override. No station's pending operations were read, cleared or replayed.
- Login used the real PIN keypad through UI Automation (`winapp ui`).
- Screenshots were taken from the live window.

**Scripts:** `E:\DevCaches\Temp\claude\df5d42d76b05a4cc\6372eeb5-b791-4cfd-911a-e4153ef34efe\scratchpad\ui\`

**Screenshots:** `artifacts/claude-20261107/ui/<cell>/NN.png`
- The index files `*-index.txt` map NN to a screen.
- `sheet-N.png` files are contact sheets used for review.

> **Correction (2026-11-08): what the sizes mean.** This laptop's display is 1920×1080 physical at **125% scaling** (120 DPI). Read every size label in this report as follows:
>
> | Label | Physical size | Layout size | Equivalent to |
> |---|---|---|---|
> | "1920×1080" | 1920×1080 (maximized) | 1536×864 units | 1920×1080 at 125% |
> | "1366×768" | about 1707×960 | 1366×768 units | 1366×768 at 100% |
> | "150eq" | about 1600×900 | 1280×720 units | 1920×1080 at 150% |
>
> - The "1366×768" window was sized by a DPI-unaware script, so Windows scaled it up.
> - Layout depends only on layout units; text and icons are drawn at 125% in every capture.
> - So 125% was covered from the start, and 150% is covered by layout equivalence. 100% at 1920×1080 cannot be reproduced on this display.

## Coverage matrix: Cashier and management client

Each cell is a full capture of all 26 shell screens: every entry of every navigation group.

| Cell | Before fixes | After fixes |
|---|---|---|
| Light, 1920×1080 | `light-1920/` | `light-1920-after/` |
| Dark, 1920×1080 | `dark-1920/` | `dark-1920-after/` |
| Light, 1366×768 | `light-1366/` (after the first five fixes) | — |
| Light, 150% equivalent | — | `light-150eq/` (2026-11-08) |
| Dark, 150% equivalent | — | `dark-150eq/` (2026-11-08) |

**Screens (NN):**

| NN | Screen | NN | Screen |
|---|---|---|---|
| 01 | Dashboard | 14 | Financial control |
| 02 | POS | 15 | Employees |
| 03 | Orders | 16 | Attendance |
| 04 | Delivery | 17 | Payroll |
| 05 | Customers | 18 | Reports |
| 06 | Menu | 19 | Daily statement |
| 07 | Recipes | 20 | Daily summary |
| 08 | Inventory | 21 | Audit |
| 09 | Waste | 22 | Control center |
| 10 | Purchasing | 23 | Printing |
| 11 | Suppliers (same combined screen as 10) | 24 | Users |
| 12 | Treasury | 25 | Backup |
| 13 | Shifts | 26 | Settings |

**Also covered:** the login screen (`00-login.png`).

**Call Center:** login and main order desk, light, 1920 (`callcenter/01.png`).

## Defects found in the real app and fixed

| # | Screen | Defect | Fix | Proof |
|---|---|---|---|---|
| 1 | Shell header (all management screens) | The shift pill said "مغلق" while a shift was open. Only the POS screen ever loaded the current shift. | `MainViewModel.LoadCurrentShiftAsync` after login | `HeaderShiftStateTests`: RED ("Strings differ"), then GREEN. The native screenshot now shows "الصباحية". |
| 2 | Inventory | The screen always showed "الطلب ناقص أو غير صالح". The movements read sent `from=&to=`, which the server cannot bind to `DateTimeOffset?` (400, reproduced with curl against the test server). | `ApiClient.StockMovementsQuery` sends only parameters that have values | `InventoryMovementsQueryTests`: RED, then GREEN. Native `light-1920-after/08.png` shows recent movements. |
| 3 | Orders | An unconfirmed InstaPay order showed only "مكتمل". A partly refunded order showed no sign of the refund. | "تحويل لم يُؤكَّد" and "به استرجاع" pills next to the status | Native `light-1920-after/03.png` |
| 4 | Orders and dashboard | The pickup order type was named two ways ("استلام من المحل" / "استلام من المطعم"). | Unified to "استلام من المطعم", the wording on the cashier button and the receipt | Native screenshots |
| 5 | Dashboard, sales mix | The label was clipped to "استلام من الـ" by a 72px column, and the bars were drawn on a fixed 150px track (73% looked like half). | Auto label column; a `ProgressBar` over the whole row | Native `02-dashboard-after.png` |
| 6 | Treasury | The ledger printed internal values: `order_refund ord-1e2e…` and `external_funding 7bd4…`. | The server resolves the order number for refunds (`ResolveOrderNumbersAsync`). The client labels refunds "استرجاع طلب #0138"; unknown types show nothing technical. | `TreasuryReferenceDisplayTests` (written after the fix; RED not observed) plus native `dark-1920-after/12-crop.png` |
| 7 | Control Center | Day-close blockers printed machine codes (`OPEN_SHIFT`, `CHECKLIST_INCOMPLETE`) under the Arabic message. | Code line removed | Native `light-1920-after/22-crop.png` |
| 8 | POS in dark theme | Category chips, best-seller badges and offer pills used fixed white or light-green colours, so the text was unreadable on dark. 22 segmented selectors (order type, delivery outcome, waste type, menu editor tabs, payment method) used a fixed white "selected" fill. | Theme brushes. Light theme is unchanged: `CardBackgroundBrush` is #FFFFFF in light. | Native `dark-1920/02-crop.png` (before) vs `dark-1920-after/02-crop.png` (after) |
| 9 | Test isolation (not visual) | Three test classes swapped the global pending-request store outside its xUnit collection, so 2–3 random failures occurred per full client run. | `[Collection(RetainedRequestStorageCollection.Name)]` | Client suite 229/229 three times in a row, now 239/239 |

### Phase 1 additions (2026-11-07 evening)

| # | Screen | Defect | Fix | Proof |
|---|---|---|---|---|
| 10 | Delivery board and assign dialog | A card, InstaPay or wallet order that was already paid showed "للتحصيل 90.00 ج", so the driver was told to collect again (UI-0032). | Same rule as the server's custody (`DeliveryViewModel.CollectPiastres`) | `DeliveryCollectDisplayTests`: RED against the old view model, then GREEN. `ui/phase1-dark/04-crop.png`. The assign dialog was not opened natively. |
| 11 | Dashboard "يحتاج تدخل" | A paid delivery that failed was invisible (FIN-0042). | New item "طلبات توصيل مدفوعة لم تصل للعميل … تحتاج استرجاعًا" | SQL RED, then GREEN. `ui/phase1-light/01-crop.png` and `phase1-dark/01`. |
| 12 | Financial control: voids | An unclassified void showed the word `other` (FIN-0006). | "إلغاء لسبب آخر" | Client RED, then GREEN. Not seen natively: no such void exists in the test data. |
| 13 | Treasury after a crash | A movement whose response was lost was forgotten on restart and could be typed again, recording the money twice (UI-0001). | Durable request; the dialog reopens refilled and is resent with the same id | `TreasuryDurabilityTests` RED, then GREEN. Real app: `ui/ui0001/1–5`; deposit and transfer each recorded once. |
| 14 | Treasury account pickers | Showed `tre-main`, `tre-bank`. | Arabic account names | `ui/ui0001/4-after-restart-fixed.png` |
| 15 | Restart banner | Named the storage key `treasury.action`. | Names the screen ("الخزينة") | `RecoveredNoticeLabelTests` RED, then GREEN. `ui/ui0001/4-after-restart-fixed.png` |

### Phase 1b additions (2026-11-08)

| # | Area | Defect | Fix | Proof |
|---|---|---|---|---|
| 16 | Shifts screen | Open/close minted a new request id per press and kept nothing on disk (UI-0001). | Durable requests, a recovered-operation banner with retry, and listing in the recovery panel | `ShiftsDurabilityTests` RED, then GREEN. Real app via a response-losing proxy: the same `requestId` on the wire, `wasReplayed:true`, one shift and one float (`ui/ambiguous/s1–s4`, `proxy-shifts.log`) |
| 17 | Treasury, ambiguous success | — (proof) | — | Server committed, response dropped, app killed, restarted, retried: the same `transferRequestId`, the same movement, `wasReplayed:true`; database has one movement and a correct balance (`ui/ambiguous/t1–t3`, `db-*.txt`) |
| 18 | Startup notice | Still said "one unresolved operation" after it had been resolved. | Refreshes when pending operations change | `RecoveredNoticeLabelTests` RED, then GREEN; `ui/ambiguous/c3` |
| 19 | Recovery panel (all screens) | Up to 260 px tall; pushed the dialog's error and submit button below the fold. | One row per item, maximum 140 px | `ui/ambiguous/c1`, `c2` |
| 20 | Treasury dialog | The footer scrolled with the form. | Footer pinned below the scroll area | `ui/ambiguous/c2` |
| 21 | Treasury ledger | `shift_opening_float` and seven other raw types | Shared Arabic labels | 8 cases RED, then GREEN; `ui/ambiguous/c3` |
| 22 | Call Center | No dark theme; fixed light panels (UI-0033) | Light/Dark dictionaries, live switch, header toggle, per-station preference | `CallCenterThemeTests` RED (12), then GREEN; `ui/callcenter-theme/` |
| 23 | Call Center cart, Cashier POS cart, Daily statement | Lines ran to the panel edges because `ItemsControl` Padding is ignored (UI-0034) | `Margin` | `ui/callcenter-theme/*-cart-after.png` |

**Call Center screens captured natively.**
- In Dark: login, desk, order lookup (empty and not-found), customer/delivery editor, size picker, populated cart.
- Populated cart in Light and Dark at 1920 (125%), at 1366×768 units, and at the 150% equivalent.

**A crash that turned out to be a harness error.**
- A scripted Call Center run crashed in `dwmcorei.dll` (`0x80070057`), reproducibly.
- An A/B build with all of today's Call Center changes reverted crashed identically.
- The cause was the script itself: `list-windows` returned a tooltip `PopupHost` first, and the script resized WinUI's internal popup window to 1366×768.
- With the main window selected, every flow ran clean. This is not an app defect.

**Observations, not changed.**
- At narrow widths the Shifts history table scrolls horizontally inside its card; the scrollbar is hidden until hover, so the last column looks cut.
- The Call Center order lookup reports a missing order with the generic "العنصر المطلوب غير موجود.".

**Build after the changes:** CashierWinUI x64 builds with 0 warnings and 0 errors (`artifacts/claude-20261107/cashier-native-build-4.log`). CallCenterWinUI x64 also builds with 0 warnings and 0 errors.

## Checked and found acceptable

- RTL layout of all 26 screens at both sizes; there is no horizontal page scroll.
  - At 1366, the POS category strip and the printing-station cards scroll horizontally inside their own rows.
- Arabic text, Cairo dates and "ج" amounts.
- Empty states: Delivery, Customers, Waste, Purchasing invoices, Attendance, Payroll and Backup each show an explanatory Arabic empty state.
- Error state: shown by the Inventory screen before fix 2.
- Dark theme: all other management screens are consistent.

## Not covered: explicit gaps, not passes

- **Display scaling.** Superseded; see the correction at the top. 125% is the native scaling of every capture, and 150% is covered by layout equivalence for all 26 Cashier screens and the Call Center desk. Still missing: 100% at 1920×1080, and true 150% text rendering.
- **Light at 1366 after the last fixes.** Fixes 6–8 were captured at 1920 only.
- **Contrast.** The 4.5:1 check was done by eye from screenshots, not measured.
- **Modals and nested dialogs.** Checkout, refund, shift close, cash count, menu editor, purchasing receive/pay, employee actions and recipe editor were not opened one by one. Their behaviour is covered by view-model tests, not native screenshots.
- **Call Center.**
  - The dark theme was added on 2026-11-08 (row 22).
  - The order-success dialog was not opened, because no order was submitted from the Call Center in this run.
  - Observation: the "تسجيل الطلب • 0.00 ج" button looks active with an empty cart.
- **Accessibility.**
  - FIXED: the login keypad buttons in both clients now carry AutomationIds `PinKey0`–`PinKey9`, `PinClear` and `PinBackspace`. UI Automation finds all 10 digit keys in the rebuilt app.
  - Keyboard-only and touch passes were not performed.
- **Bidirectional text.** Server-composed reasons such as "تحصيل طلب #0138" render as "0138#" inside Arabic text. The text is stored history, so it was left unchanged.
- **Audit log.** Lines contain stored raw values ("tre-main (capital_injection)", amounts in قرش). This is historical text and was left unchanged.


## Codex takeover — 2026-11-08: shift modal visibility (UI-0035)

The current normal Cashier build includes a bounded, scrolling form body and persistent action/error footer for opening and closing a shift. Both modal overlays now span the page's two rows rather than being measured inside its Auto header row. Existing command bindings and blind-count rules are preserved.

A native WinUI fixture reproduced 8 failures in 36 cases before the fix. Final coverage is 72/72 for owner/cashier roles, Light/Dark, and content viewports 1280×640, 1093×534 and 960×480. Primary actions remain visible. Actual button automation peers exercise invalid-input/PIN validation, cancel and recount; no HTTP requests occur. Expected cash remains hidden during counting and the cashier cannot see the main treasury balance.

Evidence: artifacts/codex-takeover-20261108/native-shifts-red/ and native-shifts-roles/ (results.json and native captures). A separate existing construction/management regression is 285/285 in native-regression/. Normal Cashier x64 builds with zero warnings/errors. These fixtures reported rasterization scale 1.0; they do not constitute new physical 125%/150% acceptance, a keyboard-only pass, or end-to-end SQL verification.

Printing: the closing slip now identifies the shift name, verified RED→GREEN; full Domain454/454 and print-rendering13/13. The comprehensive owner-requested handover report and per-driver statement remain incomplete. Current SQL/DPAPI execution blockers, exact scope and resume point are recorded in docs/CODEX_TAKEOVER_20261108.md.

## UI consistency pass — 2026-11-09 (both active WinUI clients)

The pass was UI-only. It changed no business behaviour, binding, command, AutomationId, API or data.

**What was unified**
- **Shared style dictionaries.** The Call Center had its own palette: green brand `#156B5A` (dark `#3FAF8F`),
  green-tinted surfaces, borders and text, and seven private brushes. Ten of its control styles (keypad,
  segment, quantity, note chip, offer badge, success button and others) also differed.
  - Both clients now carry the same `Styles/Colors.xaml`, `Typography.xaml` and `Controls.xaml`, with the
    Cashier's as the reference.
  - The Call Center views' 18 references to private brushes now use the shared tokens:
    `BrandTint`→`InfoBackground`, `SubtleSurface`/`InputSurface`→`NeutralBackground`,
    `SuccessSoft`→`SuccessBackground`, `OfferBorder`→`Success`, `OfferText`→`SuccessText`,
    `SegmentText`→`TextMuted`.
  - `SharedDesignSystemTests` fails if the dictionaries diverge again or a retired brush returns.
- **Product cards, both clients.** `ItemsWrapGrid` sizes every cell to the first card. A first card without a
  station chip and with a one-line name clipped every taller card below it: the card bottoms were missing on the
  Call Center desk. The chip row and the two-line name now have fixed heights, the name shows an ellipsis, and a
  tooltip carries the full name. Guarded by `ProductCardHeightTests`.
- **Legitimate differences kept:**
  - the Cashier's navigation bar and the Call Center's single desk;
  - the Call Center header's theme toggle.

**What was verified, and how**

| Check | Kind | Result |
|---|---|---|
| BEFORE/AFTER, both clients, Light and Dark: login, POS/desk, checkout, delivery, driver statement, shifts, Call Center delivery editor, lookup, cancel confirmation | visual | `artifacts/claude-20261109/ui-consistency/before`, `…/after` |
| Every Cashier navigation entry (26 screens) in Dark | visual + pixel measure | ≤0.7% bright area on every screen, so no light surface is left; inventory and payroll read for legibility (`…/after/sweep-dark`) |
| Call Center theme toggle on a live screen | runtime | Dark → background `#14171C`, brand `#4A90E2`; Light/"حسب ويندوز" → `#F5F5F5`, `#0F52BA`, applied immediately |
| Keyboard order on the Call Center desk | runtime | Shift+Tab walks the payment methods, customer button, phone field, lock, theme and product cards in order; every stop is named; the focus rectangle is visible |
| Short window, 1100×640 physical (880×512 layout units at 125%) | runtime + visual | Call Center "تسجيل الطلب"/"إلغاء الطلب" visible; Cashier checkout body scrolls with "تأكيد وتسجيل الطلب" pinned; the Cashier navigation bar scrolls and "المزيد" opens its menu |
| Product card cells after the fix | runtime (UIA bounds) | every cell 338 px; the first card no longer sets a short height |
| Client regressions | tests | `Falah.Client.Tests` 371/371 |
| Builds | build | Cashier x64 and Call Center x64: 0 warnings, 0 errors |

**Limits**
- **Display scaling:** only this machine's 125% was used. The other sizes are equivalent window dimensions, not
  real 150% scaling.
- **Light-theme management screens:** not re-captured; their dictionaries did not change in this pass.
- **Failure states:** error, no-shift and print-retry states need an injected failure. They are marked
  `UNVERIFIED 2026-11-09` in the matrix.
- **Narrator:** not run.
- **TimePicker placeholders** follow the Windows display language.
- **Captures with other windows in front:** screen captures taken while another window was in front were
  detected and deleted, and none is kept as evidence.
