"""Save explicit source reasoning for clock, audit contract, item pricing and backup scheduler."""
from knowledge_store import *
notes={
'src/Falah.Domain/Common/BusinessClock.cs':{
13:'Resolves IANA Cairo time zone once in static initialization; falls back to Windows Egypt Standard Time only on TimeZoneNotFoundException. Invalid zone data or unavailable fallback still fails type initialization; not silently UTC. Financial date semantics depend on installed time-zone rules, not a fixed offset.',
23:'Converts supplied instant to Cairo then derives DateOnly from local DateTime; no late-night restaurant cutoff. Any incoming offset is an instant and ConvertTime applies zone rules. No persisted business-day identity validation occurs here.',
29:'Reads current UTC clock and delegates BusinessDate; repeated calls can cross Cairo midnight. Callers needing one consistent transaction date should retain one instant/date. This audit does not change system clock.',
35:'Constructs unspecified Cairo local midnight and next local midnight, independently converts to UTC and returns half-open range. Correctly avoids fixed24hour assumption across offset changes. DateOnly.MaxValue AddDays overflows and invalid local midnights can throw; explicit boundary/DST tests missing here, not claimed observed.',
47:'Swaps local from/to arguments if reversed, uses beginning of first and end of last business date. Caller record endpoints and DayCount remain unchanged. Contradicts ReportingQueryService.Range which does not swap; financial source lead persisted separately.',
60:'Creates equal endpoint range with no database or current-clock dependency. Distinct BusinessDayEntity identity is not implied by calendar date.',
65:'Resolve prioritizes from over singleDate; missing to means same day, to alone also single day. All-null returns null, leaving caller default policy. Reversed pair is returned unchanged without validation, contributing reporting-range mismatch.',
76:'Delegates both stored endpoints to swapping UtcRangeForBusinessDates; produces normalized UTC interval without mutating original range. Cannot assume DayCount/Days reflect that interval when input reversed.',
79:'Lazy inclusive DateOnly iterator starts From and increments until beyond To; reversed yields empty despite ToUtcRange normalizing. At DateOnly.MaxValue final increment can throw before completion. No rows queried; date series callers must use validated bounded range.'},
'src/Falah.Domain/Audit/AuditEvent.cs':{
41:'Append-only write interface contract contains no read/update/delete methods, but cannot enforce immutable database permissions or validate event fields itself. Production atomicity depends on EF implementation sharing command transaction, not on this abstract declaration.',
51:'InMemoryAuditLog appends supplied immutable record reference to private List; no automatic severity validation, persistence or locking. Events accessor is live read-only wrapper. Test reference log cannot prove production transactional audit rollback behavior.',
59:'Validates severity using case-sensitive HashSet of info/normal/warning/critical and throws DomainException for unknown values. Does not validate actor/category/details, reject secrets inside arbitrary Details or automatically run on AuditEvent record construction.'},
'src/Falah.Domain/Pricing/ItemPricingRules.cs':{
19:'Delegates requested rule to PricingRules.ValidateAppliedRule for matching product, active/time/day eligibility. Null/invalid result freezes unchanged list price with null rule metadata; valid result freezes rule.FixedPrice/id/name. It never accepts client-supplied effective price, but list-price source trust remains caller responsibility. Both branches preserve original list price for reporting.'},
'src/Falah.Infrastructure/Backup/ScheduledBackupService.cs':{
31:'Stores injected scope factory/configuration/logger only. Does not start backup, contact database or enumerate existing backup records during construction.',
50:'Reads Backup:Automatic:key from configuration. Blank uses caller default, Boolean becomes1/0, invariant NumberStyles.Any double parses else default. Does not require positive/finite values; TimeSpan getters can throw after successful numeric parsing. No real config secrets read by audit.',
59:'Disabled flag exits, otherwise awaits startup grace then sequentially executes cycles. Cycle exceptions log and retry, cancellation exits; StartupDelay and CheckInterval conversions/Delay are outside generic per-cycle exception guard, so invalid numeric configuration can terminate worker. No timer-triggered financial posting here; authorized backup service has explicit retention side effects elsewhere.',
84:'Creates scoped context, selects latest successful Backup record by completion time, checks maximum age, creates System backup then verifies it. Failed creation returns; failed verification logs but still invokes guarded prune. Scheduling is based on success rather than Valid verification, so failed verification does not force immediate retry; retained newest verified file protection is counterevidence against deleting last valid backup. No service actually run.',
90:'Latest-age eligibility includes Kind Backup and Status Succeeded but not VerificationStatus Valid or current file existence. Historical success can suppress backup until age threshold even if unreadable/missing; operational recovery risk, not proven deleted production data.',
92:'Orders successful backup metadata by descending nullable CompletedAtUtc and takes first; actual file usability is not rechecked by this ordering.',
124:'Disables prune for KeepCount<=0. Loads recorded successful backup rows with nonnull size, delegates retention to SelectPrunableBackups protecting newest verified-valid ID, then deletes only selected recorded paths and clears size on successful deletion/already-missing file. Per-file exceptions log and preserve size. SaveChanges failure after file deletion can leave stale metadata; no file/DB transaction possible. Audit never invokes File.Delete.',
129:'Retention query filters successful Backup and nonnull FileSizeBytes, not verification-valid only; invalid backups count toward keep window, but domain separately protects newest valid entry.',
135:'Maps row to retention fact using CompletedAt or RequestedAt fallback and exact Valid verification marker; no fresh restore verification. Domain selector protects one newest known-valid identity, not every manually valuable backup.',
141:'Restricts file deletion loop to domain-returned ID set. Recorded paths are trusted operational metadata; this review does not test tampered database paths or filesystem ACLs. Missing file is marked gone; per-file deletion failure remains logged.'}}
added=0
for path,mapping in notes.items():
 source=load_symbols(path)
 old_path=WORK/'Knowledge/Reviews/financial_coordinator'/Path(path+'.json')
 old=json.loads(old_path.read_text(encoding='utf8')) if old_path.exists() else {}
 records=list(old.get('symbols',[]));done={r['stable_id'] for r in records if r.get('status')=='ANALYZED'}
 seen=set()
 for s in source['symbols']:
  if not s['isCallable']:continue
  line=s['startLine'];assert line in mapping,(path,line)
  seen.add(line)
  if s['stable_id'] in done:continue
  records.append({'stable_id':s['stable_id'],'status':'ANALYZED','signature':s.get('qualifiedSymbol') or s['name'],
   'span':[s['startLine'],s['startColumn'],s['endLine'],s['endColumn']],
   'reasoning':mapping[line],
   'inputs':'Declared arguments and explicitly described object/configuration/clock state; no operational records inspected.',
   'outputs':'Exact declared result or guard/append/worker effect described individually above.',
   'side_effects':'Clock/pricing/audit source is read-only except in-memory Append. Scheduler production paths can create/verify/delete backup files and update records; none executed.',
   'trust_boundaries':'Actor authorization belongs financial handlers; system backup identity is trusted scheduling policy, not a user permission bypass demonstrated here.',
   'database':'No persistence in pure domain methods. Scheduler EF queries and SaveChanges specifically documented; no DB connection made by audit.',
   'printing_ui':'Reporting date/price results support financial UI; no UI visual or printer workflow restarted.',
   'performance':'Date helpers constant-time except Days iteration; backup cycles sequential with history pruning materialization, no measured resource claims.',
   'duplicates_conflicts':'Cairo date resolver used across reports; normalization mismatch tracked separately. Pricing resolves central rule rather than duplicating offer math.',
   'issues_hypotheses':mapping[line],
   'tests_missing':'Boundary dates/DST/reversed scopes; invalid backup numeric configuration; verified-retention and filesystem/SaveChanges failures on authorized disposable fixture. No runtime execution claimed.',
   'verified_callers_callees':'BusinessClock full source1-83; ItemPricing Resolve plus PricingRules validation source; scheduler1-158 plus BackupRules due/retention guards read. EF implementation/provider behavior not runtime verified.'})
  added+=1
 assert seen==set(mapping),(path,seen,set(mapping))
 save_review('financial_coordinator',path,{'symbols':records,'scope':'Explicit per-callable static financial reasoning; original reviewer shards retained; no live application execution.'})
print(json.dumps({'new_root_original_callable_receipts':added}))
