# src/Falah.Application/Expenses/ExpenseRequests.cs

Expenses API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `662f77c18749b832c04e7eac87d58c4447cd68c1f12a6c77b9c62239028da254`. Working SHA-256 `662f77c18749b832c04e7eac87d58c4447cd68c1f12a6c77b9c62239028da254`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-23.

Symbols indexed (index is not a claim every method was reviewed): AddExpenseRequest@8, AddExpenseResult@12, ExpenseDto@16, ExpenseConflictException@20.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 8: `public sealed record AddExpenseRequest(`
- Line 12: `public sealed record AddExpenseResult(`
- Line 16: `public sealed record ExpenseDto(`
- Line 22: `public ExpenseConflictException(string message) : base(message) { }`

Request ID covers a financial expense, with source account and optional approval. Result separates treasury posting from replay. Shape alone cannot prevent invalid amount/account/shift; handler must. Refund/reprint must not recreate this request.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
