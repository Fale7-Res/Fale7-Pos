# src/Falah.Client.Cashier/Views/CashierView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `f4ad86f2f888998008880ff98facd4c9b027254296c1942b385b5c305649549c`; worktree SHA256 `1144993977b6f66a46a50bb5cb31d5ebc448756cd8a12d4579dee0d3e82ceaad`; LF-equivalent `True`.

Actual role: Legacy WPF POS cart/menu/variant/checkout/result/refund surface; not active WinUI client.

## Function/resource review

- `OpenShiftCommand` at `src/Falah.Client.Cashier/Views/CashierView.xaml:24`: No-shift opening cash/error action inside rootfullgrid, no managerapproval/shiftname controls here. Actual backend/authguard separate. Delivery tab explicitlydisabledincomplete, whereasactiveWinUIdeliveryimplemented.

- `SetOrderTypeCommand` at `src/Falah.Client.Cashier/Views/CashierView.xaml:61`: Pickup/takeaway triggers selectedcolors; UniformGridinheritsRTL. Cartline delete/quantity/note commands use ancestorItemsControlDataContext correctly; no globalpendingCanEdit bindings.

- `GrandTotalDisplay` at `src/Falah.Client.Cashier/Views/CashierView.xaml:181`: Cart footerlabels finaltotal althoughlegacyVM onlysubtotal/no tax/delivery. Different fromactiveWinUIestimatedlabel, scopeexplicit. Lookup/refund and unconditionalClearCartcommands needVMguards.

- `SelectCategoryCommand` at `src/Falah.Client.Cashier/Views/CashierView.xaml:228`: Horizontalcategoryscroll realcategoryIDs, selectedbuttonwhiteforegroundbutchildCategoryLabelTextexplicitTextBaseforegroundmayremain dark ratherthaninheritwhite. Neednativecontrast, notactiveUI002sameconsumer.

- `VisibleProducts` at `src/Falah.Client.Cashier/Views/CashierView.xaml:212`: Fixed159x220WrapPanelcells contain145x180productplus variableofferstack: multipleofferscanexceedfixedcellheight/overlapnextrow. Displaybaseprice evenmultiplevariantproductmayhaveotherstartingprice, activetileusesStartingPriceDisplay.

- `ItemsSource="{Binding VisibleProducts}"` at `src/Falah.Client.Cashier/Views/CashierView.xaml:260`: No MenuError or IsLoadingMenu binding found in this complete view despite VM properties, so failed menu load may leave blank or stale catalog without error/retry feedback. Anchored real catalog binding; absence verified across file. No actual menu API request performed.

- `IsCheckoutOpen` at `src/Falah.Client.Cashier/Views/CashierView.xaml:342`: 520fixed unscrollablemodal withfourpaymentbuttons and4rowkeypad canexceedshortviewport. CashkeypadinheritsRTL unlikeactiveLTR. No directtexttenderentry; integerkeypad insufficientfractionaltender unless exactdefault.

- `ChangeDuePiastres` at `src/Falah.Client.Cashier/Views/CashierView.xaml:366`: DisplayedlegacychangebindingexposesUI013 unitconversionbug inVM. Submitbusy disables button only,back/paymentmethods stillcommandrouted; outcomesunknown protectionnotestablishedbyview.

- `HasCheckoutResult` at `src/Falah.Client.Cashier/Views/CashierView.xaml:400`: ShowsorderID/authoritativetotal andpendingelectronicconfirm; StartNewOrderindependentbutton. No printwarning/status/receiptpreview inmarkup. LookuprefundshowsrawEnglishstatus, priorrefunded/remaining andactualrefund fields.

- `IsVariantPickerOpen` at `src/Falah.Client.Cashier/Views/CashierView.xaml:317`: Alloverlaysrootgridfullboundsbecause rootno rowdefinitions, unlikeShiftsissue. Variant/lookup/resultcards fixedwidth/no maxheight/scroll; manyvariants/longerrorscanoverflow. No nativefocusconfinement/accessiblenames forglyphclose.

## Tests and limitations

NOT_EXECUTED. No legacyruntime/businesswrites. Need productofferscellheight, absentloadfeedback, keypadRTL/decimalmoney, selectedcategorycontrast andcashoutrecovery onlyiflegacyactuallydeployed. No callable declarations.

## Recommendations (not implemented)

Do not confuse legacyfeaturegaps withactiveWinUI. Prefercurrentfrontend operationally; anylegacysourcefixrequiresapproval.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
