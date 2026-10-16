# src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs

**path**: src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 97e4427ded182f726e97ed7a1bc362cdaf4b94a3ab009635317fc36f0f9b0e57

**git_blob_sha256**: 97e4427ded182f726e97ed7a1bc362cdaf4b94a3ab009635317fc36f0f9b0e57

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 116]]

**functionality_and_reasoning**: Full116 lines read. Reads target +other days no lock/token; reopen vs newday can both produce active days FIN-009. SameRequest replay only if current Reopened, after reclosed old replay reopens again if new request ID unchanged? guard not durable receipt, can reapply old reopen after reclose. Reopen leaves previous snapshot Authoritativetrue while active day (intended historical lastclose). Clears closing checklist and saves audit with operation. Catch DomainException writes blocked log but consumed approval may still save even rejected reopen.

**significant_methods**: Constructor; HandleAsync; ToDto

**interactions_and_risks**: Full116 lines read. Reads target +other days no lock/token; reopen vs newday can both produce active days FIN-009. SameRequest replay only if current Reopened, after reclosed old replay reopens again if new request ID unchanged? guard not durable receipt, can reapply old reopen after reclose. Reopen leaves previous snapshot Authoritativetrue while active day (intended historical lastclose). Clears closing checklist and saves audit with operation. Catch DomainException writes blocked log but consumed approval may still save even rejected reopen.

**findings**: ["FIN-009"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared Open handler single-active race and transient request state.

**recommendations**: Use active-day singleton lock and durable reopen receipt independent current status; distinguish historical authoritative closed snapshot while active; rollback unused approval then separate attempt log.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
