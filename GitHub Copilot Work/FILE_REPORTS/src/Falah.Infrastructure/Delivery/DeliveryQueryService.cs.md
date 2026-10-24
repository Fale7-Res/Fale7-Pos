# src/Falah.Infrastructure/Delivery/DeliveryQueryService.cs

**path**: src/Falah.Infrastructure/Delivery/DeliveryQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 42b35dd12c3663583c3ddd69e2b15f86968043295a9ede98b2efcebaa10627fb

**git_blob_sha256**: e2b5553a921ea0d31937f6da513bde91613f88d0447576d6754f9c2a8b767fc7

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 212]]

**functionality_and_reasoning**: Full212 lines read. Batched dictionaries avoid per-delivery N+1. Unbounded board/history settlement lists. Account scope preview same algorithm but not transactional point-in-time. Statistics counts by current driver and overwritten AssignedAt; reassign shifts historical attribution away from original driver, timeline retains evidence. AmountToCollect nonzero prepaid facts may be display misleading unless client reads IsPaid. CanSettle requires resolved deliveries unlike handler custody-only allowance.

**significant_methods**: Constructor; ListActiveDriversAsync; ListAllDriversAsync; ListActiveDeliveryServicesAsync; ListAllDeliveryServicesAsync; ListReadyForDispatchAsync; ListDispatchBoardAsync; ListActiveForDriverAsync; ListByStatusAsync; QueryToDtos; GetOneAsync; GetTimelineAsync; GetDriverAccountAsync; GetDriverStatisticsAsync; local InRange; DescribeOperationalStatus

**interactions_and_risks**: Full212 lines read. Batched dictionaries avoid per-delivery N+1. Unbounded board/history settlement lists. Account scope preview same algorithm but not transactional point-in-time. Statistics counts by current driver and overwritten AssignedAt; reassign shifts historical attribution away from original driver, timeline retains evidence. AmountToCollect nonzero prepaid facts may be display misleading unless client reads IsPaid. CanSettle requires resolved deliveries unlike handler custody-only allowance.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared facts and settlement code. No live customer data read.

**recommendations**: Define assignment vs outcome attribution using events and stable snapshots; pagination plus SQL range filtering; align CanSettle allowed scope.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
