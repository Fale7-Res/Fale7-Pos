# Native/process/timer/network boundary review

Source-only bounded assignment at `c35816ecae49633674775dd32061c6ee92f94aaf`. **20 manual boundary records,86 source references,35 affected source files baseline/hash verified.** A lexical search of468 tracked production-src C# files produced62 selected native/type/timer/transport/file API hits, all reconciled to explicit reviewed spans. Expanded process/assembly/COM/timer/network probes are recorded separately. This is not a complete security-phase verdict or exhaustive callable review.

- [Native spooler, transports, WinRT picker, print HTTP](batch-01-native-print-network.json)
- [Agent config, explicit test mode, poll worker and journal](batch-02-agent-config-loop-storage.json)
- [Client HTTP and framework image authentication boundary](batch-03-client-http-image.json)
- [Visible client health and clock timers](batch-04-visible-client-timers.json)
- [Scheduled backup and bounded retries](batch-05-background-maintenance-retries.json)
- [Local runtime-type journal loader and theme store](batch-06-local-journal-type-resolution.json)
- [Backup filesystem destination](batch-07-backup-file-destination.json)
- [Managed GDI+ font/raster boundary](batch-08-managed-gdi-renderer.json)
- [Finite coverage/discovery ledger](finite-coverage-ledger.json)
- [Source validation](validation.json)

No explicit process spawning or general Assembly.Load/LoadFrom/NativeLibrary/COM activation call was found by selected production-source probes. Seven winspool imports and one WinRT picker interop serve explicit printing/photo functions. This does not rule out aliased/reflection-generated calls or external-library behavior.

Periodic work has identified purposes:5s connectivity,30s display clock, configured print polling, configured backup age/retention, bounded report and lock-contention retry. Timers alone are not evidence of a backdoor. Backup can create/verify/delete recorded files and therefore must not be started merely for inspection. Retention protects newest verified-valid backup.

Material observations: client environment/agent configuration controls network authorities; default HTTP loopback does not ensure safe plaintext LAN deployment. Raw printer TCP has no TLS. Local journal Type.GetType has no type allowlist, but no remote/RCE chain is established. SEC002 separately covers PIN-bearing plaintext retention. Framework BitmapImage fetch has no visible bearer-header handoff while server image route is authenticated: source-supported candidate, actual native request/cache/render behavior NOT_EXECUTED.

No POS/helper process, native API, network test, authentication, database, backup or physical printer operation executed. No operational journal, credential, config or customer data read. All evidence records describe source; actual OS drivers, installed binaries, ACLs, third-party runtime behavior, individual HTTP endpoint permissions and build/install scripts require their separate review/test queues.
