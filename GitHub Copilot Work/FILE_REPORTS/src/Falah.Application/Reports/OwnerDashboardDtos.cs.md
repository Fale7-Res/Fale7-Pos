# src/Falah.Application/Reports/OwnerDashboardDtos.cs

Reports API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `46998d7ab7f677438be4477df32b02e33a9be00ccb105a63a9e690b4f69389d1`. Working SHA-256 `0cbcea3fcb9e03851fe45a316b0fdd6f2b2655f0b95795db98eac04137a822da`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-187.

Symbols indexed (index is not a claim every method was reviewed): OwnerDashboardDto@18, DashboardTrendDto@36, ChangeDto@50, TrendPointDto@52, CashPositionDto@56, LeakageDto@81, ProfitabilityDto@93, AttentionDto@112, and@126, StockPositionDto@135, LowStockItemDto@140, ObligationsDto@147, PeopleDto@160, SystemHealthDto@171, PrintRouteHealthDto@180.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 18: `public sealed record OwnerDashboardDto(`
- Line 36: `public sealed record DashboardTrendDto(`
- Line 50: `public sealed record ChangeDto(long Current, long Previous, long Delta, double? Percent);`
- Line 52: `public sealed record TrendPointDto(string Label, long NetSalesPiastres, int OrderCount);`
- Line 56: `public sealed record CashPositionDto(`
- Line 81: `public sealed record LeakageDto(`
- Line 93: `public sealed record ProfitabilityDto(`
- Line 112: `public sealed record AttentionDto(`
- Line 135: `public sealed record StockPositionDto(`
- Line 140: `public sealed record LowStockItemDto(string Id, string Name, string Unit, long BalanceMilliUnits, long MinimumMilliUnits);`
- Line 147: `public sealed record ObligationsDto(`
- Line 160: `public sealed record PeopleDto(`
- Line 171: `public sealed record SystemHealthDto(`
- Line 180: `public sealed record PrintRouteHealthDto(`

Read-only reports distinguish unknown cost/null percentages, current balances versus date-scoped flows, driver custody and pending electronics. Several comments claim missing payroll/purchasing despite existing projects: comments are stale evidence. Owner print-health comment claims physical success though agent only reports transport completion (PRT-002). Aggregate correctness lives outside DTOs.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
