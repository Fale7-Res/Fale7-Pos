# src/Falah.Application/Approvals/IssueManagerApprovalRequest.cs

Approvals API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `d525bbbc6bc48c2a37c71247e0335de1af4fa09dcfc56bbcc5b3f77ddc00be73`. Working SHA-256 `0e09a943a6383f4b9f8d0c20555a0df5444fe136f43ba81074afaaa14cf5baa7`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-16.

Symbols indexed (index is not a claim every method was reviewed): IssueManagerApprovalRequest@10, IssueManagerApprovalResult@15.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 10: `public sealed record IssueManagerApprovalRequest(string Pin, string Action, string? Reason);`
- Line 15: `public sealed record IssueManagerApprovalResult(`

PIN identifies the second approving user; action-scoped approval result includes server-selected identity and timestamp. DTO has no business validation; server handler must enforce action catalog, one-use scope and rate limits. Do not log the PIN.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
