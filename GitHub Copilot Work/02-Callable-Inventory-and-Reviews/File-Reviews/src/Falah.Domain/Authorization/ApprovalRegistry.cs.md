# src/Falah.Domain/Authorization/ApprovalRegistry.cs

**path**: src/Falah.Domain/Authorization/ApprovalRegistry.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 029edee4286b6aea725da962ddaafff1e6bc20f57ddc922801c39095b43585d0

**git_blob_sha256**: dfbfe472a37ed4e98d1411877726f6c6834f5fbfcb38165e0aefdf1252dea4ee

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 155]]

**functionality_and_reasoning**: Single-use registry abstraction and pure permission rules fully read. Production uses EF, in-memory dictionaries not threadsafe. Owner/manager can issue approval for any action independent permission list; supervisor requires held action. ValidateApproval binds only action+issuer+unused, no amount/entity/beneficiary/expiry/current issuer active. AuthorizeCommand requireApproval means fallback not mandatory when actor already permitted. Incidentally sent role-based approval escalation lead to Security coordinator, not duplicate full scan.

**significant_methods**: IApprovalRegistry; InMemoryApprovalRegistry Register/Find/IsConsumed/MarkConsumed; AuthorizationPolicy HasPermission/IssueManagerApproval/AuthorizeCommand/ValidateApproval/AuthorizeShiftClose/AuthorizeExpense/AuthorizeShiftOpen/AuthorizeAddSupplier

**interactions_and_risks**: Single-use registry abstraction and pure permission rules fully read. Production uses EF, in-memory dictionaries not threadsafe. Owner/manager can issue approval for any action independent permission list; supervisor requires held action. ValidateApproval binds only action+issuer+unused, no amount/entity/beneficiary/expiry/current issuer active. AuthorizeCommand requireApproval means fallback not mandatory when actor already permitted. Incidentally sent role-based approval escalation lead to Security coordinator, not duplicate full scan.

**findings**: []

**evidence**: Full bounded source read and cross-path reasoning, no runtime/user data mutation.

**recommendations**: Coordinate approval/role/PIN session incidental leads with dedicated Security coordinator; add concurrency and malformed-state regressions only after approved design.

**uncertainties**: No runtime reproduction. Endpoint authorization/rate limits owned root. No Roslyn semantic coverage claim.
