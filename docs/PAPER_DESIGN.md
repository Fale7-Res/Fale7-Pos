# Falah paper redesign — design

Status: **approved by the owner 2026-11-17** (after reviewing the current eleven papers printed on the XP-D200N). Owner decisions were made in conversation and on the
review page `https://claude.ai/artifact/33ArrmigjX3xFBkopzgBic` (notes collection).

## Intent

The owner has not been satisfied with any printed paper. His words: "design, consistency, more
clarity, a respectable design" and "I need a professional design". Today the eleven papers are
drawn in three different visual styles (the redesigned cashier receipt; the older call-center /
kitchen / delivery table style; the slip style of shift and driver papers), carry no logo, and show
visible defects. Success: every paper looks like it comes from one restaurant, reads at a glance at
its station, and the owner approves each one on real paper.

## Goals

1. One visual system shared by every paper: header, title, order-number treatment, rules, label/value
   lines, item table, totals, footer, one money format, one date/time format.
2. The restaurant logo on customer papers (Receipt and CallCenter routes' order receipts) only.
3. The Dubai typeface, slightly larger than today, with a lighter bold than today's Segoe UI Bold.
4. Fix the defects seen in the current previews:
   - labels that break across lines (e.g. "طلبات ملغاة (خارج المبيعات)", "إيداعات / إضافات من الخزنة");
   - bidi flips such as "(0281#)";
   - mixed time formats (19:42 vs 07:42 م).
5. Two behaviour changes the owner decided:
   - **Cashier receipt becomes one piece.** The separate customer-number ticket and the cut after
     it are removed. The single receipt carries the large order number the customer collects with.
     The counter works from the kitchen tickets.
   - **Shift-opening slip is retired.** It is no longer queued when a shift opens and can no longer
     be reprinted from the Shifts screen. Opening a shift, its float and every money record are unchanged.

## Non-goals

- No change to any amount, calculation, report basis, or which figures a paper shows (apart from the
  two removals above).
- No database migration, no settings change, no change to which route prints which job.
- No change to print-agent reliability behaviour (PRT-0004 status, PRT-0013 retry stay as they are).
- No settings screen for logo or font.

## Anti-goals (a failure even if it works)

- A kitchen ticket that is slower to read or noticeably longer than today.
- Any figure on paper that differs from the same figure on screen.
- Arabic that loses contextual shaping, or digits/codes that flip direction.
- Anything sent to the printer beyond the proven commands (ESC @, GS v 0, LF, GS V 0).

## Constraints

- 80 mm paper, 576-dot raster, 24-dot margins (physically proven). 1-bit output, 203 dpi.
- Fonts: Windows-installed faces only (no downloads). Dubai (Regular/Medium/Bold) ships with
  Windows 10/11. If Dubai is missing on a station, fall back to today's Segoe UI faces and log one
  warning at agent start.
- Keep `ThickenStrokes` (1-dot growth). The owner chose the Dubai sample printed with it.
- Server and agents are deployed together. An older agent that receives the new logo element prints a
  blank line (unknown kinds degrade to text in `TicketElementMapping`), never fails.
- Already-queued `ShiftOpen` jobs must still compose and print, so the composer stays for legacy jobs.
- Tests change only where behaviour deliberately changes (receipt piece count, shift-open slip).

## Approaches

| Approach | Trade-off | Chosen |
|---|---|---|
| 1. One visual system in the existing printing code: shared layout helpers in `TicketLayout`/`PrintRules`, a `TicketLogo` element, a typeface change in `BitmapTicketRenderer`, each composition restyled | Smallest change, no database or API surface, money code untouched | **Yes** (owner handed over the how) |
| 2. Logo and font managed from the Settings screen | Flexible, but needs a migration, UI, and shipping images to agents, for something rarely changed | No |
| 3. Owner-editable paper templates | A large separate product | No |

## Decided design

### Typeface and scale (renderer)

- Normal text: Dubai Medium. Bold: Dubai Bold. Owner-approved sample: Normal 23 px.
- Scale (px, my call, tune on paper): Normal 23, Large 30, Title 40, Huge 64, Display 88.
- Line advance tightened. Dubai's GDI line height is loose: lay text out on its measured glyph height
  plus a small fixed gap, so no paper grows more than ~10% in length from the font change alone.

### Logo

- Source: the owner's colour logo (shield, "F", "Fale7", "ألذ سندوتش"). The original is kept at
  `docs/assets/fale7-logo-original.png`.
- Print version: owner chose **variant C**, a spot-colour separation. Green, dark text and the silver
  rim print solid. Red on the left half becomes paper. Red on the right half ("ale7" and its underline)
  prints solid. Yellow, cream and white stay paper.
- The 256 × 256-dot 1-bit PNG is committed as an embedded resource of `Falah.PrintAgent`
  (`Assets/receipt-logo.png`). It is generated once from the original and reviewed on paper.
- New domain element `TicketLogo` with wire kind `logo`. The renderer draws the embedded bitmap
  centred, is never thickened, and is excluded from stroke growth like the QR.
- Placed at the top of the Receipt and CallCenter order receipts only. On a reprint the reprint
  banner comes first, then the logo.

### Shared structure

- **Customer header:** logo → restaurant name (Large, bold, centred) → sub-name from settings (the owner
  changes it himself to «ألذ سندوتش» in Settings) → address/phones as today.
- **Internal header** (kitchen, delivery, dispatch, driver statements, shift close, expense):
  restaurant name (Normal, bold, centred) → document title (Title, bold, centred).
- **Order number:** label «رقم الطلب» then the number on its own line (Huge), isolated so "#" never
  flips. Used identically on receipt, call center, kitchen, delivery and dispatch.
- **Rules:** one heavy rule closes the header; light rules separate sections and items.
- **Label/value lines:** label on the start side, value on the end side. The label never wraps. A long
  value wraps under itself.
- **Money on paper:** `12,500 ج` with thousands separators (my call). Done in the ticket's own money
  formatter only; the shared `Money` formatting used by screens is unchanged.
- **Date/time on paper:** one format, `dd-MM-yyyy  hh:mm ص/م`, Cairo time (my call; today the receipt
  already uses 12-hour).
- **Codes and numbers inside Arabic text** (order numbers, phone numbers, references) are wrapped in
  directional isolates so they never reorder.
- **Footer:** light rule plus the paper's existing caption, in one style.

### Per paper (content unchanged unless stated)

1. **Cashier receipt (Receipt route).** One piece:
   - customer header;
   - order type and the big order number;
   - date/time and cashier;
   - item table (item and variant, quantity, price), with notes and offers under their item;
   - totals, with the grand total emphasised;
   - payment;
   - menu QR with its caption;
   - name and tagline footer as today (no new slogan or thank-you line).
2. **Call-center copy.** The same layout as the cashier receipt (it converges with it), plus the
   customer / area / address block.
3. **Charcoal and 4. Grill kitchen tickets.** Internal header with the station name as title, big order
   number, type and time, items without prices (bold quantity), bold notes, the pickup-later line
   emphasised.
5. **Delivery order.** Internal header; the amount to collect (or "nothing to collect") emphasised.
6. **Driver dispatch.** Internal header; driver name and the amount to collect emphasised; the
   reassignment marker kept.
7. **Driver statement, summary and 8. detailed.** Internal header; same figures; label/value system.
9. **Shift opening slip.** Retired, as above.
10. **Shift close statement.** Internal header; same sections and figures; labels no longer wrap;
    order references isolated.
11. **Expense voucher.** Internal header; same fields.

## Amendment 1 (owner, 2026-11-17, after approving the first prints)

1. **More lines, separators and clarity.** Every section of a paper opens with a black band carrying
   its title in white (e.g. «الأصناف», «الحساب»; on slips, the existing section titles), and every
   item table is a ruled grid. Each item's notes and offer sit inside the grid under their item.
   This applies to customer papers, kitchen tickets, the delivery order, and slip section titles.
2. **The menu QR moves to the delivery customer's paper.** The QR now prints on the call-center
   copy, the paper that goes with a delivery order, and no longer on the cashier (takeaway) receipt.
3. **Takeaway prints one paper.** Already delivered by Goal 5 (the owner confirmed).

## Amendment 2 (owner, 2026-11-17): the two delivery papers swap roles

The owner described his delivery flow: one paper prints when the order is taken and **goes into the
restaurant**; another prints when the driver comes for the order and **goes with the driver to the
customer**. Amendment 1 is corrected to match that flow.

1. **The call-center copy, printed when the order is taken, becomes a small preparation paper.**
   - Internal header, then the order number, type and time.
   - The customer's name, area and address.
   - The item grid without prices, with notes.
   - No logo, no prices, no totals, no QR.
   - It keeps the one-line unconfirmed-transfer warning: an instruction to the staff, not a figure.
2. **The driver dispatch paper, printed at assignment, becomes the customer's full receipt.**
   - The logo and customer header, the order number, the driver, and the customer's contact and address.
   - The item grid with prices, the totals, and what to collect.
   - The delivery notes, and the menu QR with the brand footer.
   - The reassignment marker stays.
3. The takeaway receipt is unchanged: logo, no QR.

## Verification

- Domain tests:
  - composition tests updated only where content deliberately changes;
  - new tests for one-piece receipt, logo placement, money/date format, label no-wrap, bidi isolation.
- Integration tests:
  - the printing classes (126 today) pass;
  - opening a shift queues no ShiftOpen job;
  - an already-queued ShiftOpen job still composes.
- Client tests: the Shifts screen no longer offers the opening-slip reprint.
- Owner review: previews of all ten papers on the review page, then a physical print of each on the
  XP-D200N with the owner's OK recorded per paper. Paper length per paper compared with today.

## Left to the builder

Exact pixel sizes and gaps within the stated scale, rule thicknesses, and the column widths of item
tables, as long as previews and paper are approved.

## Calls I made (owner can change)

- Thousands separators on paper amounts.
- 12-hour time with ص/م on every paper.
- The size scale above.
- Logo size 256 dots (~32 mm).
- Internal papers carry the restaurant name in text instead of the logo.
