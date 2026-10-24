# src/Falah.Application/Treasury/TreasuryRequests.cs

Treasury API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `6213584b772681c647ce1627e9353cc56ec915127a1553f37968924f2ab7ff6a`. Working SHA-256 `dc81bfe18c81e2d4ccb07c1d5232c22d3a2fea59c9ce8fa7a899e520116d35a4`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-78.

Symbols indexed (index is not a claim every method was reviewed): DepositRequest@8, DepositResult@13, WithdrawalRequest@18, WithdrawalResult@23, TransferRequest@27, TransferResult@31, SafeDropRequest@37, SafeDropResult@41, TreasuryMovementDto@47, TreasuryAccountBalanceDto@60, TreasuryPositionDto@69, TreasuryConflictException@75.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 8: `public sealed record DepositRequest(`
- Line 13: `public sealed record DepositResult(string MovementId, string Type, long AmountPiastres, string DestinationAccountId, bool WasReplayed);`
- Line 18: `public sealed record WithdrawalRequest(`
- Line 23: `public sealed record WithdrawalResult(string MovementId, long AmountPiastres, string SourceAccountId, bool WasReplayed);`
- Line 27: `public sealed record TransferRequest(`
- Line 31: `public sealed record TransferResult(string MovementId, long AmountPiastres, string SourceAccountId, string DestinationAccountId, bool WasReplayed);`
- Line 37: `public sealed record SafeDropRequest(`
- Line 41: `public sealed record SafeDropResult(string MovementId, long AmountPiastres, bool WasReplayed);`
- Line 47: `public sealed record TreasuryMovementDto(`
- Line 60: `public sealed record TreasuryAccountBalanceDto(string AccountId, long BalancePiastres);`
- Line 69: `public sealed record TreasuryPositionDto(`
- Line 77: `public TreasuryConflictException(string message) : base(message) { }`

Distinct deposit/withdrawal/transfer/drop request IDs; piastre integer money and separate account IDs avoid netting money paths. Position DTO separates pending electronics and driver custody from account totals. Handler validation/locking rather than DTOs enforces balances.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
