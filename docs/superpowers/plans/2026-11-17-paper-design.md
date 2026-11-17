# Falah Paper Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** One professional, consistent visual system for every printed paper: the Dubai typeface, the
owner's logo on customer receipts, a one-piece cashier receipt, and the shift-opening slip retired.

**Architecture:**
- Composition stays in `Falah.Domain.Printing` (`PrintRules` + `TicketLayout`), gaining a formatting
  vocabulary (`TicketFormat`) and a `TicketLogo` element.
- `Falah.PrintAgent.BitmapTicketRenderer` draws the new typeface and the embedded logo bitmap.
- Server, clients and money code are touched only to retire the opening slip and to isolate order
  numbers in one statement line.

**Tech Stack:** .NET 8, C#, System.Drawing (GDI+), xUnit, WinUI 3 (one XAML button).

**Spec:** `docs/superpowers/specs/2026-11-17-paper-design-design.md` (owner-approved 2026-11-17)

## Global Constraints

- **No git commit, push or stash.** The owner commits. Each task ends with a `git diff --stat`
  checkpoint instead of a commit (CLAUDE.md).
- No migration, no settings change, no change to any amount or calculation (spec Non-goals).
- Integration tests use `FalahTest_<guid>` databases only. Never touch the `Falah` database.
- Paper: 576-dot raster, 24-dot margins, 1-bit. Printer commands limited to ESC @, GS v 0, LF, GS V 0.
  Never ESC 7 or DC2 #.
- Typeface: Dubai Medium (normal) / Dubai Bold (bold). Fallback when Dubai is missing: today's faces
  (Segoe UI Semibold / Segoe UI Bold), plus one warning in the agent log at start.
- Size scale (px): Normal 23, Large 30, Title 40, Huge 64, Display 88.
- `ThickenStrokes` stays on. The logo and QR are never thickened.
- Money on paper: `12,500 ج`. Invariant culture with Latin digits, whatever the thread culture.
- Time on paper: date `dd-MM-yyyy` and time `hh:mm ص|م`, Cairo, each led by U+200E.
- Logo: 256 × 256 dots, centred, Receipt and CallCenter order receipts only.
- Tests change only where behaviour deliberately changes. Each such edit names the spec line that
  changed it. Never delete a test to get green.

## Review Focus

1. **A Dubai-less station** (Windows LTSC, or a stripped image) must print in the fallback faces, not
   in GDI's silent substitute font that loses Arabic shaping. Task 3 pins it.
2. **Bidi on paper:** a date/time after an Arabic label, and `(#0281)` inside an Arabic sentence, must
   read left-to-right. Unit tests pin the strings. Only paper proves the rendering (Task 8 checklist
   items P3 and P10).
3. **An older agent receiving `logo` / `fitStart`** must print, not fail. The logo becomes a blank
   line, and the row wraps as it does today. Task 2 pins it with a DTO round-trip test.
4. **Kitchen ticket length** must not grow more than 10% (spec anti-goal). Task 5 pins it against
   today's 54.8 mm Charcoal and 49.4 mm Grill sample tickets.
5. **A `ShiftOpen` job queued before deployment** must still compose and print, while new opens queue
   none. Task 7 pins both.

---

### Task 1: Ticket formatting vocabulary

**Files:**
- Create: `src/Falah.Domain/Printing/TicketFormat.cs`
- Modify: `src/Falah.Domain/Printing/PrintRules.cs`. Every `$"{x.Egp} ج"`, every
  `static string Egp(long)`, and `FormatCairo` / `FormatCairoDate` / `FormatCairoTime` (lines ~433-455)
  route through `TicketFormat`.
- Modify: `src/Falah.Domain/Printing/TicketLayout.cs:106-113`. `CashierItemRow` uses `TicketFormat.Amount`.
- Modify: `src/Falah.Infrastructure/Shifts/ShiftCloseStatements.cs:99`. Order numbers wrapped with
  `TicketFormat.Code`.
- Test: `tests/Falah.Domain.Tests/TicketFormatTests.cs`, plus amount/time assertions in
  `PrintRulesTests.cs` and `ShiftCloseStatementCompositionTests.cs` updated to the new format.

**Interfaces:**
- Produces: `public static class TicketFormat` in `Falah.Domain.Printing`:
  - `string Amount(Money m)`
  - `string Amount(long piastres)`
  - `string Date(DateTimeOffset utc)`
  - `string Time(DateTimeOffset utc)`
  - `string DateTime(DateTimeOffset utc)` (Date + two spaces + Time)
  - `string Code(string text)` (returns `"‎" + text + "‎"`)

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public void Amount_groups_thousands_and_keeps_latin_digits_under_arabic_culture()
{
    using var _ = new CultureScope("ar-EG");
    Assert.Equal("12,500 ج", TicketFormat.Amount(Money.FromPiastres(1_250_000)));
    Assert.Equal("25.50 ج", TicketFormat.Amount(Money.FromPiastres(2_550)));
    Assert.Equal("0 ج", TicketFormat.Amount(Money.Zero));
    Assert.Equal("-25 ج", TicketFormat.Amount(Money.FromPiastres(-2_500)));
}
[Fact] public void Date_and_time_are_cairo_with_ltr_marks()
{
    var at = new DateTimeOffset(2026, 11, 17, 17, 42, 0, TimeSpan.Zero);   // 19:42 Cairo
    Assert.Equal("‎17-11-2026", TicketFormat.Date(at));
    Assert.Equal("‎07:42 م", TicketFormat.Time(at));
    Assert.Equal("‎17-11-2026  ‎07:42 م", TicketFormat.DateTime(at));
    Assert.Equal("‎12:05 ص", TicketFormat.Time(new DateTimeOffset(2026, 11, 16, 22, 5, 0, TimeSpan.Zero)));
}
[Fact] public void Code_isolates_order_numbers() => Assert.Equal("‎#0281‎", TicketFormat.Code("#0281"));
[Fact] public void No_composed_paper_contains_an_unformatted_thousand()
    // ComposeShiftCloseStatement(Statement(...)) from ShiftCloseStatementCompositionTests (sales 1,250,000 piastres)
    // and ComposeReceiptTicket(Receipt, header, items, totals with Total = 1,250,000 piastres):
    // no text or cell matches @"\d{4,}(\.\d+)? ج"; "12,500 ج" is present
```

`CultureScope` is a test helper that sets and restores `CultureInfo.CurrentCulture`/`CurrentUICulture`.

- [ ] **Step 2: Run, expect FAIL** (`TicketFormat` does not exist)

Run: `dotnet test tests/Falah.Domain.Tests --filter FullyQualifiedName~TicketFormatTests`

- [ ] **Step 3: Implement `TicketFormat` and route every call site through it.** Use the format
  `"#,0.##"` with `CultureInfo.InvariantCulture`, and `BusinessClock.CairoTimeZone`. Replace the
  existing `FormatCairo*` bodies with calls to `TicketFormat`, or delete them.

- [ ] **Step 4: Run the Domain suite; update only the amount/time-format assertions that changed, naming the spec line in each edit**

Run: `dotnet test tests/Falah.Domain.Tests`. Expected: all pass (519 + new).

- [ ] **Step 5: Checkpoint** with `git diff --stat` (no commit).

### Task 2: `TicketLogo` element and `TicketRow.FitStart` on the wire

**Files:**
- Modify: `src/Falah.Domain/Printing/PrintRecords.cs`:
  - add `public sealed record TicketLogo : TicketElement;`
  - append `bool FitStart = false` to `TicketRow`.
- Modify: `src/Falah.Application/Printing/ComposedTicketDto.cs`. Append `bool FitStart = false` to
  `TicketElementDto`.
- Modify: `src/Falah.Application/Printing/TicketElementMapping.cs`:
  - add `KindLogo = "logo"`;
  - map both directions;
  - carry `FitStart`.
- Modify: `src/Falah.Domain/Printing/TicketLayout.cs`. `SlipLine` and `MoneyLine` set `FitStart: true`.
- Test: `tests/Falah.Domain.Tests/TicketElementMappingTests.cs` (create if absent)

**Interfaces:**
- Produces: `TicketLogo` (no data); `TicketRow.FitStart` (the start cell takes its unwrapped width,
  up to 70%; the end cell takes the rest).

- [ ] **Step 1: Failing tests**

```csharp
[Fact] public void Logo_round_trips() => Assert.IsType<TicketLogo>(TicketElementMapping.ToDomain(TicketElementMapping.ToDto(new TicketLogo())));
[Fact] public void FitStart_round_trips() =>
    Assert.True(((TicketRow)TicketElementMapping.ToDomain(TicketElementMapping.ToDto(
        new TicketRow(new[] { new TicketCell("أ", 60), new TicketCell("ب", 40) }, FitStart: true)))).FitStart);
[Fact] public void An_old_agent_reading_a_logo_gets_harmless_text()
    // a DTO with Kind "logo" deserialised by a mapping that does not know it (simulate: Kind = "logo-v0") → TicketText with empty text
[Fact] public void Slip_and_money_lines_fit_their_label() =>
    Assert.True(((TicketRow)TicketLayout.SlipLine("طلبات ملغاة (خارج المبيعات)", "3")).FitStart);
```

- [ ] **Step 2: Run, expect FAIL.** Run: `dotnet test tests/Falah.Domain.Tests --filter FullyQualifiedName~TicketElementMappingTests`
- [ ] **Step 3: Implement the records, the mapping and the layout flags.**
- [ ] **Step 4: Run the Domain suite. Expected: all pass.**
- [ ] **Step 5: Checkpoint** with `git diff --stat`.

### Task 3: Renderer typeface, scale, line advance, logo drawing, FitStart

**Files:**
- Create: `src/Falah.PrintAgent/Assets/receipt-logo.png`. The 256 × 256 1-bit file is generated by
  `tools/ReceiptLogo` from `docs/assets/fale7-logo-original.png`.
- Create: `tools/ReceiptLogo/ReceiptLogo.csproj` and `Program.cs`. They hold the owner-approved variant
  C conversion:
  - silver/grey with saturation < 0.18 is ink when value < 0.80;
  - green (hue 75-200) is ink;
  - red is ink only at x > 0.505 × width, or value < 0.45;
  - yellow, cream and white are paper;
  - near-black (all channels < 28) is paper.
- Create: `docs/assets/fale7-logo-original.png`. Copy the owner's original from the session scratch
  `images\1.png`.
- Modify: `src/Falah.PrintAgent/Falah.PrintAgent.csproj`. `<EmbeddedResource Include="Assets\receipt-logo.png" />`
- Modify: `src/Falah.PrintAgent/BitmapTicketRenderer.cs`:
  - `PixelSize` uses the global scale;
  - `CreateFont` resolves Dubai Medium / Dubai, falling back to the Segoe faces;
  - the text line advance uses `font.Size × 1.25` instead of the `MeasureString` height (a wrapped
    text uses lines × advance);
  - `TicketLogo` is measured as 256 dots and stamped centred, and added to the protected regions
    like the QR;
  - for a `FitStart` row, the start-cell width = min(measured unwrapped width + CellGap, 70% of the
    content).
- Modify: `src/Falah.PrintAgent/Program.cs`. One log line at start when the Dubai fallback is in use.
- Test: `tests/Falah.Server.IntegrationTests/TicketTypographyTests.cs` (no database; plain xUnit class)

**Interfaces:**
- Consumes: `TicketLogo`, `TicketRow.FitStart` (Task 2).
- Produces:
  - `internal static (string Normal, string Bold, bool IsFallback) ResolveFaces(IEnumerable<string> installedFamilies)`
    on `BitmapTicketRenderer` (InternalsVisibleTo the test project if needed);
  - `public static bool UsesFallbackFaces { get; }`.

- [ ] **Step 1: Failing tests**

```csharp
[Fact] public void Dubai_is_used_when_installed() =>
    Assert.Equal(("Dubai Medium", "Dubai", false), BitmapTicketRenderer.ResolveFaces(new[] { "Dubai", "Dubai Medium", "Segoe UI", "Segoe UI Semibold" }));
[Fact] public void Missing_dubai_falls_back_to_the_proven_segoe_faces() =>
    Assert.Equal(("Segoe UI Semibold", "Segoe UI", true), BitmapTicketRenderer.ResolveFaces(new[] { "Segoe UI", "Segoe UI Semibold" }));
[Fact] public void Logo_is_256_dots_centred_and_not_thickened()
    // render [TicketLogo, TicketText("x")]; decode the GS v 0 raster; the ink bounding box of the first 256 rows
    // equals the embedded PNG's ink box shifted to x = (576-256)/2, pixel for pixel (no 1-dot growth)
[Fact] public void A_fitted_label_never_wraps()
    // render SlipLine("طلبات ملغاة (خارج المبيعات)", "3") at Large; the row height < 1.5 × the line advance
```

- [ ] **Step 2: Run, expect FAIL.** Run: `dotnet test tests/Falah.Server.IntegrationTests --filter FullyQualifiedName~TicketTypographyTests`
- [ ] **Step 3: Generate the logo asset with `tools/ReceiptLogo`. Implement the renderer changes and the startup log line.**
- [ ] **Step 4: Run the new class plus `MenuQrRenderingTests` and `EscPosTicketRendererTests`. Expected: all pass.**
- [ ] **Step 5: Checkpoint** with `git diff --stat`.

### Task 4: Customer papers — one-piece cashier receipt, converged call-center copy

**Files:**
- Modify: `src/Falah.Domain/Printing/PrintRules.cs`:
  - `ComposeReceiptTicket`: Receipt no longer emits `ComposeCustomerNumberSection` or the `TicketCut`;
  - `ComposeBrandHeader` starts with `TicketLogo`;
  - CallCenter uses the same detail section as Receipt, plus the customer/area/address block.
- Modify: `src/Falah.Domain/Printing/TicketLayout.cs`. Add `CustomerHeader(ReceiptHeader)`:
  `TicketLogo` → Brand (Large, bold) → Tagline when SubName is set → ThinRule.
- Test: `tests/Falah.Domain.Tests/PrintRulesTests.cs`. Lines ~153, ~435-439 and ~508-523 assert two
  pieces; change them per spec Goal 5 ("Cashier receipt becomes one piece").

**Interfaces:**
- Consumes: `TicketLogo`, `TicketFormat.*`.
- Produces: `TicketLayout.CustomerHeader(ReceiptHeader header) : IEnumerable<TicketElement>`.

- [ ] **Step 1: Failing tests**

```csharp
[Fact] public void Cashier_receipt_is_one_piece_led_by_the_logo()
{
    var t = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, Header(), Items(), Totals());
    Assert.Empty(t.Lines.OfType<TicketCut>());
    Assert.IsType<TicketLogo>(t.Lines[0]);
    Assert.Contains(Texts(t), s => s == "#0286");          // the big number the customer collects with
}
[Fact] public void Call_center_copy_uses_the_receipt_layout_plus_the_customer_block()
    // first line TicketLogo; contains the same item rows as the Receipt route; contains "العنوان" and the area
[Fact] public void Reprint_banner_precedes_the_logo()
    // MarkAsReprint(receipt): Lines[0..banner] are banner, then TicketLogo
[Fact] public void Kitchen_and_slips_carry_no_logo()
    // ComposeKitchenTicket, ComposeShiftCloseStatement, ComposeExpenseTicket: no TicketLogo
```

- [ ] **Step 2: Run, expect FAIL.** Run: `dotnet test tests/Falah.Domain.Tests --filter FullyQualifiedName~PrintRulesTests`
- [ ] **Step 3: Implement. Update the two-piece assertions listed above, citing spec Goal 5.**
- [ ] **Step 4: Run the Domain suite plus `PrintingVerticalSliceTests` (integration). Expected: all pass.**
- [ ] **Step 5: Checkpoint** with `git diff --stat`.

### Task 5: Operational papers — kitchen, delivery order, driver dispatch

**Files:**
- Modify: `src/Falah.Domain/Printing/TicketLayout.cs`:
  - add `InternalHeader(string restaurantName, string title)`: name (Normal, bold, centred), then
    title (Title, bold, centred), then a heavy rule;
  - `LargeOrderNumber` prints the number at `TicketTextSize.Huge`.
- Modify: `src/Falah.Domain/Printing/PrintRules.cs`. `ComposeKitchenTicket` (title = station name),
  `ComposeDeliveryOrderTicket` (title «أوردر دليفري») and `ComposeDriverDispatchTicket` (title
  «تسليم للسواق») use `InternalHeader`.
- Test: `tests/Falah.Domain.Tests/PrintRulesTests.cs`; `tests/Falah.Server.IntegrationTests/TicketTypographyTests.cs`

**Interfaces:**
- Produces:
  - `TicketLayout.InternalHeader(string restaurantName, string title) : IEnumerable<TicketElement>`;
  - `TicketLayout.Footer(string caption) : IEnumerable<TicketElement>` (ThinRule, then a centred
    caption). Used by every non-customer paper's closing caption, in Tasks 5 and 6.

- [ ] **Step 1: Failing tests**

```csharp
[Fact] public void Operational_papers_open_with_the_internal_header()
    // kitchen Charcoal: Texts[0] == restaurant name, Texts[1] == "الفحم"; delivery: Texts[1] == "أوردر دليفري"
[Fact] public void Kitchen_tickets_stay_within_ten_percent_of_todays_length()   // TicketTypographyTests
    // the review-page sample, copied into the test: order "#0286", takeaway, 2026-11-17 17:42 UTC, items
    // شاورما فراخ/جامبو×2 (Grill, note "بدون بصل، زيادة طحينة"), كفتة على الفحم/نص كيلو×1 (Charcoal),
    // كبدة إسكندراني/ربع×2 (Charcoal, note "حار"), بطاطس محمرة/كبير×3, بيبسي×2 (no station).
    // Decoded raster height <= 482 dots (Charcoal: 54.8 mm × 1.10) and <= 434 dots (Grill: 49.4 mm × 1.10)
```

- [ ] **Step 2: Run, expect FAIL.**
- [ ] **Step 3: Implement.** If the length test fails, shrink spacing within the Task 3 scale. Never
  drop content.
- [ ] **Step 4: Run the Domain suite plus `TicketTypographyTests` plus `PrintingVerticalSliceTests`. Expected: all pass.**
- [ ] **Step 5: Checkpoint** with `git diff --stat`.

### Task 6: Slips — driver statements, shift close statement, expense voucher

**Files:**
- Modify: `src/Falah.Domain/Printing/PrintRules.cs`. `SlipHeading`, `ComposeDriverStatement`,
  `ComposeShiftCloseStatement` and `ComposeExpenseTicket` use `InternalHeader`, `TicketFormat.*`,
  and `SlipLine`/`MoneyLine` (FitStart).
- Test: `tests/Falah.Domain.Tests/ShiftCloseStatementCompositionTests.cs`, `PrintRulesTests.cs`

- [ ] **Step 1: Failing tests**

```csharp
[Fact] public void Statement_labels_are_fitted_and_order_numbers_isolated()
    // ComposeShiftCloseStatement(Statement(..., outstanding: new[]{ "تحويلات إلكترونية بانتظار التأكيد: 1 (‎#0131‎)" })):
    // every two-cell TicketRow has FitStart; the outstanding text keeps the U+200E marks
[Fact] public void Slips_open_with_the_internal_header()
    // expense: Texts[1] == "إيصال مصروف"; driver statement summary: Texts[1] == "كشف يومية سائق - ملخص"
```

- [ ] **Step 2: Run, expect FAIL.**
- [ ] **Step 3: Implement.** Section headings and figures are unchanged (spec Non-goals).
- [ ] **Step 4: Run the Domain suite plus integration `ShiftCloseVerticalSliceTests`, `SlipPrintingAndReprintTests` and the driver statement classes (`--filter FullyQualifiedName~DriverStatement`). Expected: all pass.**
- [ ] **Step 5: Checkpoint** with `git diff --stat`.

### Task 7: Retire the shift-opening slip

**Files:**
- Modify: `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs:123-129`. Remove the
  `DocumentPrintJobs.Build(PrintDocumentKind.ShiftOpen, …)` add, and update the comment.
- Modify: `src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs:42`. `ShiftOpen` is refused with
  a `DomainException("إيصال افتتاح الوردية لم يعد يُطبع.")` (HTTP 422 through the existing mapping).
- Keep: `PrintJobQueryService.ComposeShiftOpenSlipAsync` and `PrintRules.ComposeShiftOpenTicket`, for
  jobs queued before deployment. Add a comment saying why.
- Modify: `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:90-91`. Remove
  `ReprintShiftOpenAsync` and its command.
- Modify: `src/Falah.Client.CashierWinUI/Views/ShiftsView.xaml:~268-275`. Remove the
  `Shifts_ReprintOpen` button.
- Test, each change citing spec Goal 5 ("Shift-opening slip is retired"):
  - `CashierShiftStartupTests.cs:51,83`: expect 0 ShiftOpen jobs;
  - `DefaultOpeningCashTests.cs:126-130`: assert the default float on the shift record (`OpeningCashPiastres`)
    instead of the slip text;
  - `SlipPrintingAndReprintTests.cs:69-70,163,172`;
  - `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:137,146`: expected kinds `{ "ShiftClose", "Expense" }`.

- [ ] **Step 1: Failing tests**

```csharp
[Fact] public async Task Opening_a_shift_queues_no_slip()            // CashierShiftStartupTests
    => Assert.Equal(0, await Q(db => db.PrintJobs.CountAsync(j => j.DocumentKind == "ShiftOpen")));
[Fact] public async Task Reprinting_an_opening_slip_is_refused()      // SlipPrintingAndReprintTests
    // POST /api/print-jobs/reprint-document {DocumentKind:"ShiftOpen"} → 422, body error contains "لم يعد يُطبع"
[Fact] public async Task A_legacy_opening_job_still_prints()          // SlipPrintingAndReprintTests
    // insert a PrintJobEntity { DocumentKind = "ShiftOpen", OrderId = shiftId } directly; GET /api/print-jobs/{id}/ticket → 200 and contains "رصيد افتتاحي"
[Fact] public void Shifts_screen_offers_no_opening_reprint()           // Falah.Client.Tests
    => Assert.Null(typeof(ShiftsViewModel).GetProperty("ReprintShiftOpenCommand"));
```

- [ ] **Step 2: Run, expect FAIL.** Run: `dotnet test tests/Falah.Server.IntegrationTests --filter "FullyQualifiedName~CashierShiftStartupTests|FullyQualifiedName~SlipPrintingAndReprintTests|FullyQualifiedName~DefaultOpeningCashTests"` and `dotnet test tests/Falah.Client.Tests`
- [ ] **Step 3: Implement. Build the WinUI client:** `dotnet build src/Falah.Client.CashierWinUI/Falah.Client.CashierWinUI.csproj -p:Platform=x64` (0 errors).
- [ ] **Step 4: Run the same commands, plus `ShiftCloseVerticalSliceTests` and `BusinessDayAssociationTests`. Expected: all pass.**
- [ ] **Step 5: Checkpoint** with `git diff --stat`.

### Task 8: Previews, regression run, and owner acceptance on paper

**Files:**
- Modify: `tools/ReceiptPreview/Program.cs`. Add the kitchen (Charcoal, Grill), call-center, delivery,
  dispatch and driver statement papers, with the same synthetic data as today's review page. Remove
  the opening slip. Keep the QR decode check.
- Modify: `docs/PRINT_AGENT.md`, "What a job can print" and "Arabic on the printer". Record Dubai, the
  logo, the one-piece receipt, the retired opening slip, and the physical evidence.

- [ ] **Step 1: Run the full automated set.**
  - `dotnet test tests/Falah.Domain.Tests`
  - `dotnet test tests/Falah.Client.Tests`
  - `dotnet test tests/Falah.Server.IntegrationTests --filter "FullyQualifiedName~PrintAgent|FullyQualifiedName~PrintingVerticalSlice|FullyQualifiedName~PrintJob|FullyQualifiedName~EscPos|FullyQualifiedName~MenuQr|FullyQualifiedName~CashDrawer|FullyQualifiedName~AgentLog|FullyQualifiedName~AgentOptions|FullyQualifiedName~AgentStartup|FullyQualifiedName~Reprint|FullyQualifiedName~TicketTypography|FullyQualifiedName~Shift|FullyQualifiedName~Slip|FullyQualifiedName~DriverStatement|FullyQualifiedName~DefaultOpeningCash"`

  Expected: 0 failures. Report exact counts.
- [ ] **Step 2: Render previews** with `dotnet run --project tools/ReceiptPreview -- <scratch>/previews`.
  Republish them to the review page `https://claude.ai/artifact/33ArrmigjX3xFBkopzgBic`, replacing the
  `papers/*.png` files and the paper list (10 papers).
- [ ] **Step 3: Print each paper on `XP-D200N-Charcoal`, one at a time. The owner checks each against this list and the answer is recorded per paper:**
  - P1 Arabic shaped;
  - P2 nothing clipped at either edge;
  - P3 date/time and `#` numbers read left-to-right;
  - P4 the logo is clear;
  - P5 the bold is lighter than before;
  - P6 the text is a little bigger;
  - P7 no label broken across lines;
  - P8 amounts read `12,500 ج`;
  - P9 one cut per paper;
  - P10 `(#0281)` reads correctly on the shift close.
- [ ] **Step 4: Write the evidence (counts, per-paper owner OK) in `docs/PRINT_AGENT.md`.** Remove the
  temporary `XP-D200N-Charcoal` queue and the throwaway `FalahCharcoalVerify_20261117` database only
  after the owner agrees.
- [ ] **Step 5: Checkpoint** with `git diff --stat`. Hand the owner the list of changed files for his commit.
