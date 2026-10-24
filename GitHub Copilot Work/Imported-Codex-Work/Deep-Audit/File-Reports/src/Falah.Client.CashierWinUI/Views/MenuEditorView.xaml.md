# src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `2e2c0737ca5ba81716a108b3f323e865b6c540825c9ed5368bc887bfc06e884a`; worktree SHA256 `eb56e6edfc726a96d63207e7c41035e80dce3838591a9e11ed936be978ed73be`; LF-equivalent `True`.

Actual role: RTL overlay editing menu, variants, tax and offers.

## Function/resource review

- `SetKindCommand` at `src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml:34`: Four equal segments bind kind commands and parameterized converter colors. Selected white/brand pairing differs from unselected neutral/muted. Permission-specific controls reflected through CanManageKind and CanEdit.

- `SelectRowCommand` at `src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml:91`: Catalogue buttons bind root VM explicitly; they have no explicit IsImageBusy disabling. VM CanBrowse does not exclude image busy, corroborating async A image completion versus B selection candidate.

- `HasEditor` at `src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml:135`: Empty prompt is duplicated: decorative Border and later TextBlock share same inverse flag and grid cell, a source-observed overlap risk requiring native render. Fixed360px list plus editor can compress at narrow windows.

- `CanEditImage` at `src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml:241`: Only choose/delete image buttons use CanEditImage; other fields/selection/save use CanEdit. Spinner shows image busy but does not overlay form. ProductImageUri uses URL cache converter; hint states512KB formats.

- `Variants` at `src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml:268`: Root explicit bindings scope nested item commands and recipe choices correctly. Fixed120/170px columns plus name/delete create horizontal width pressure at text scale.

- `PriceProducts` at `src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml:327`: Days and products use two-way selection with write gating. Max260px offers list scrolls inside larger editor scroll; focus/keyboard testing required.

- `SaveLabel` at `src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml:374`: Save caption can express retry; CanEdit remains actual enablement. Action errors/success wrap at footer. Overlay is Grid not ContentDialog: no modal focus trap or Escape dismissal in this file.

## Tests and limitations

NOT_EXECUTED. Verify selection while picker/upload pending with fake delayed image API, narrow viewport/text scaling, nested commands, focus containment and recovery after restart.

## Recommendations (not implemented)

Bind all editor navigation to coherent image/write busy state and capture selection generation before applying image result. Remove duplicate empty prompt after visual verification; no source changes implemented.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
