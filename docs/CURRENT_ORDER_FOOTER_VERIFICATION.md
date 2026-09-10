# Current Order footer verification

The footer now uses an explicit RTL two-column Grid. Subtotal, delivery,
discount, complimentary amount, tax and final total have separate rows. A
separator precedes the final total; checkout occupies a later row. The outer
Auto / star / Auto Grid keeps the footer outside the cart ScrollViewer.

Below 520 DIP of available panel height, order-type selection becomes a menu
and checkout/cancel share a row below the totals. Financial rows remain
separate. Tax display uses the existing domain TaxEngine, including inclusive
tax labelling.

## Native measurements

The isolated `FooterLayoutProbe=true` build instantiates the real CartPanelView
with fixture HTTP responses; it cannot write to a backend. Its 48 cases passed
on 2026-09-07:

- Empty, small, large (60 lines and wide amounts), discount, exclusive tax,
  inclusive tax, split payment and combined discount/comp/tax states.
- 1920 x 1080 and 1366 x 768 at DIP-equivalent 100%, 125% and 150% scaling.
- RTL column separation, row separation, checkout clearance, footer containment,
  independent cart viewport and unchanged footer position after scrolling.

Results are in `artifacts/footer-layout/results.json`. The smallest nonempty
cart viewport was 51 DIP in the combined 1366 x 768 / 150% case.

These are native layout measurements with scaled available dimensions, not
tests that changed the Windows display scaling setting. Native bitmap capture
returned zero-sized images, so screenshot inspection and verification on an
actual scaled 15-inch terminal remain outstanding. Split-payment coverage here
checks the footer while that ViewModel state is active, not the payment dialog.

Reproduce with an isolated output directory:

```powershell
dotnet build src/Falah.Client.CashierWinUI/Falah.Client.CashierWinUI.csproj --no-restore -p:Platform=x64 -p:FooterLayoutProbe=true -p:OutDir=E:\falah-restaurant\Falah-Restaurant-CSharp\artifacts\footer-probe\
```

Launch the resulting executable interactively. It closes when the matrix
finishes. Normal builds exclude the fixture entry point and probe source.
