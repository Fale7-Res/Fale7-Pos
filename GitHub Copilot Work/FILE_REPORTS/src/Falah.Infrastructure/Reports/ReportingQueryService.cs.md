# src/Falah.Infrastructure/Reports/ReportingQueryService.cs

**path**: src/Falah.Infrastructure/Reports/ReportingQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 452d58690fb3e6e06613cdd5892969c1afd137b7be0ed9cb3758b1e210c99509

**git_blob_sha256**: a3116e7a0550fc9352bfa07c998b3ea496b27660fd37f138662f6adaed633cb9

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 586]]

**functionality_and_reasoning**: Full586 lines read in2 bounded chunks. Sales selection SQL excludes nameof Cancelled uppercase whereas persisted wire lowercase; depends SQL collation. COD settled-custody exclusion deliberately supplements frozen IsPaid, so paid-state mutation absence not finding. Current recipes/categories/cost used on old sales with acknowledged approximate historical cost, but summary claims no duplicate truth too strong. Business-day treasury figures per-day movement deltas labelled balances, shift float/handover BusinessDayIdnull absent. Sales uses lifetime refunds on sale-date, activity uses refund-date. N+1 shift summaries; unrestricted range materializes all sales/stock. Significant actions duplicate expense and treasury approvals. Food completeness line counts omit unknown order packaging, can claim complete profit despite missing bag cost.

**significant_methods**: Constructor; LoadSalesOrdersAsync; LoadOrderIdsWithCashOutsideRestaurantAsync; GetSalesReportAsync overloads; GetTaxReportAsync overloads; ToDto; GetExpenseReportAsync overloads; GetRefundActivityAsync overloads; GetBusinessDayReconciliationAsync/local Balance; GetDailySummaryAsync/local SumType; Range; GetTopSellingItemsAsync; GetSoldProductsAsync; GetProductHourlySalesAsync; GetCategorySalesAsync; GetFoodCostReportAsync; GetSignificantActionsAsync; GetShiftSummaryAsync; GetShiftsHistoryAsync; GetShiftsForBusinessDateAsync; BuildShiftSummaryAsync; GetDeliveryOutcomesReportAsync

**interactions_and_risks**: Full586 lines read in2 bounded chunks. Sales selection SQL excludes nameof Cancelled uppercase whereas persisted wire lowercase; depends SQL collation. COD settled-custody exclusion deliberately supplements frozen IsPaid, so paid-state mutation absence not finding. Current recipes/categories/cost used on old sales with acknowledged approximate historical cost, but summary claims no duplicate truth too strong. Business-day treasury figures per-day movement deltas labelled balances, shift float/handover BusinessDayIdnull absent. Sales uses lifetime refunds on sale-date, activity uses refund-date. N+1 shift summaries; unrestricted range materializes all sales/stock. Significant actions duplicate expense and treasury approvals. Food completeness line counts omit unknown order packaging, can claim complete profit despite missing bag cost.

**findings**: ["FIN-010", "FIN-004"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared domain SalesRules/ProfitabilityRules and full delivery settlement; FIN-010 whole-order pending deduction found; tests pending broader inspection.

**recommendations**: Separate cash-basis payment events from sale-date revenue, define day opening/closing vs delta, include packaging completeness, freeze cost snapshots if true historical margins required, paginate/date-bound and batch shift summaries.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
