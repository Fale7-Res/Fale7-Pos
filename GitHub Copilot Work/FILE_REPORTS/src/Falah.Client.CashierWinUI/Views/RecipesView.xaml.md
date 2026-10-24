# src/Falah.Client.CashierWinUI/Views/RecipesView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `3c41757e4d1030f744ce7ee818066e61e204b942b1046dbaf1ea2171fdaf3a34`; worktree SHA256 `6d6d87cd0703d327ddbd5210aa92c85e907e360097c14e223e1114b3690f1c63`; LF-equivalent `True`.

Actual role: Recipe cost coverage, selectable dish detail and ingredient editor.

## Function/resource review

- `SelectedRecipe` at `src/Falah.Client.CashierWinUI/Views/RecipesView.xaml:78`: ListView selectionTwoWay has no IsEnabled gate on IsDetailLoading or IsEditing. Confirms UI018 counterargument of native selection disabled is not supported by this markup; lateA response can publish whileBselected as reviewedVM allows.

- `Coverage` at `src/Falah.Client.CashierWinUI/Views/RecipesView.xaml:39`: Coverage/filter search240+type180 separate from400px master list and detailstar. Status text explicitly incomplete and knowncostfloor; cost/marginnotzero forunknown.

- `Detail.ProductName` at `src/Falah.Client.CashierWinUI/Views/RecipesView.xaml:184`: Detail title binds actualreturnedDTO not selectedrow; observantoperator can spot mismatch but it does not guard saving. StartEdit only CanStartEdit visibility, no binding tying detailID toselectedrecipe.

- `DetailCostDisplay` at `src/Falah.Client.CashierWinUI/Views/RecipesView.xaml:211`: Cost/packaging, sellingprice/basis, contribution/foodcostpercent allactualVMdisplays. Fourcardspadded14 insidedetailcolumncancompress. No independent claim of actualcurrentcost accuracy frommarkup.

- `DetailLines` at `src/Falah.Client.CashierWinUI/Views/RecipesView.xaml:272`: Ingredientquantity/basequantity/kind/unitcost/source/asof/extendedcost/missingreason give provenance.480pxfixedcolumns plusnames require widepane; nohorizontal scroll. Emptyrecipeexplicitunknowncostnotfree.

- `EditorLines` at `src/Falah.Client.CashierWinUI/Views/RecipesView.xaml:350`: Quantity/kind/yield inputs have no busy/CanEditAction disable. Yieldlabel pieces-per-unit admitsnumerictext while VMUI019 truncates fractionalinput; nativeplaceholderdoesnotvalidate. Delete rootcommand withtooltip butnoexplicitaccessiblename.

- `CancelEditCommand` at `src/Falah.Client.CashierWinUI/Views/RecipesView.xaml:436`: Cancel/savealwayscommand-bound; selection and editingremain enabledmarkup while requestinflight, so VMimmutabletarget/pendingguardrequired. Footerwarnsmissingcost/margin and staysoutsideingredientscroll. No actualrecipewrite.

## Tests and limitations

NOT_EXECUTED. Need reversedA/Bdetail task completion, selectwhileediting, fractionalyield, unknowncost and480fixedcolumnsoverflow. No callabledeclarations.

## Recommendations (not implemented)

Invalidate stale detail byselectedID/generation,freeze editing target and enforceexact yieldunits. Preserveprovenance/unknowncost explanations.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
