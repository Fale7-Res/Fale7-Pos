"""Field-specific source reasoning for employee persistence accessors, not generated-method counting."""
from knowledge_store import *
path='src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs';source=load_symbols(path)
fields={
'EmployeeEntity':{
'Id':'Employee primary identity; deterministic create RequestId, used by employee anchor lock in payment. Setter not identity authorization.',
'Name':'Human profile/display name, current name can replace historical display but ledger RecordedBy snapshots separate. Personal information; no values copied.',
'EmployeeNumber':'Unique index in current model; nonblank guard and database uniqueness distinct. Request replay catch only succeeds when same deterministic employee ID exists.',
'JobTitle':'Nonblank profile field, does not confer financial user permission.',
'Phone':'Required nonblank profile contact but no number-format validation in EmployeeRules. Private values never enumerated.',
'HireDate':'DateOnly profile employment start, not automatic wage accrual boundary in examined commands.',
'CompensationType':'Raw string after case-insensitive TryParse; check-in requires exact Daily, producing casing-admission lead. No enum/check in accessor.',
'DailyWagePiastres':'Signed long daily wage basis, used automatic daily credit and late/overtime rules. Profile create/update do not reject negatives.',
'ShiftValuePiastres':'Signed long extra-shift value; handler uses positive value else DailyWage fallback, unchecked multiply by count.',
'MonthlySalaryPiastres':'Nullable long salary configuration; null differs zero, no positivity/max guard in accessor.',
'UserId':'Optional login reference; create sets null, profile update preserves; not employee PIN or separate authentication mechanism.',
'CanDeliver':'Profile capability metadata, not automatic authorization to settle driver custody or money.',
'Role':'Employee profile role string, separate Users.Role and actual actor permissions; updating it does not itself prove privilege escalation.',
'IsActive':'Financial report filters active employees, owner obligations query ledger includes inactive. Profile update changes flag, no automatic debt settlement.',
'Notes':'Optional administrative free text, no sensitive values read; DTO exposes stored text to authorized employee callers.',
'Address':'Optional private address field, included ToDto without service-level redaction; endpoint permission external.',
'NationalId':'Optional private government identifier schema, no values accessed; ToDto includes so access controls important.',
'Department':'Optional organizational classification, no account/ledger partition enforced by field.',
'EmploymentArrangement':'Optional loose taxonomy separate pay-basis CompensationType; raw string, not enforced enum in setter.',
'FingerprintId':'Optional device reference, not biometric template bytes or authenticated user token in this entity. Actual device validation outside current source.',
'LifecycleStatus':'Optional raw lifecycle classification plus independent IsActive Boolean can disagree; no derived normalization in setter.',
'OvertimeRatePiastres':'Nullable employee rate metadata; examined manual overtime uses request/settings rate and automatic overtime uses daily wage, so this field is not universal authoritative overtime rate.',
'CreatedAtUtc':'Profile creation timestamp, update preserves; not immutable financial ledger sequence or wage earning date.'},
'EmployeeLedgerEntryEntity':{
'Id':'Ledger primary key; deterministic in most automated/keyed writers, random manual entry by disclosed contract. Setter no append-only guarantee.',
'EmployeeId':'Indexed employee reference; no explicit FK/navigation in current model block. Existence generally command validated but overtime writer needs separate provider/invariant check.',
'Type':'Raw string financial category; case-normalized manual but numeric undefined enum still possible. Exact enum-name consumers omit unsupported categories.',
'Description':'Stored accounting narrative, no amount/type validation or privacy redaction in accessor.',
'DebitPiastres':'Signed long debit, intended nonnegative but backing field accepts negative. Subtracted in employee running balance, not independent treasury cash posting.',
'CreditPiastres':'Signed long credit, intended nonnegative; wage/custom rate paths can lack guard. Added to current employee obligation.',
'BalanceAfterPiastres':'Cached running balance snapshot, not authoritative immutable fact sum; helper unlocked writers can race even when credit/debit facts survive.',
'RecordedBy':'Actor/display attribution string, not actor FK/session proof; commands generally map persisted user or automatic employee name.',
'AuthorizedBy':'Nullable human authority attribution; validated approval/direct actor policy in writers, field not token evidence.',
'ShiftId':'Optional operational shift reference; cashout requires Open while general earnings may carry arbitrary optional shift key.',
'ReferenceId':'Optional source reference polymorphic string, not typed FK proof; some writers link attendance/treasury IDs.',
'Hours':'Nullable double work/rate input snapshot; no finite/nonnegative check by setter, caller math policy applies.',
'RateSnapshotPiastres':'Optional long original rate snapshot; correction currently recomputes from live wage/settings, so preserving this value alone does not ensure historical adjustment accuracy.',
'ValueSnapshotPiastres':'Optional input value snapshot, not automatically equal debit/credit or net by accessor.',
'PayrollRunId':'Optional run association; no explicit relationship in employee-ledger model block, no automatic salary settlement/inclusion guarantee.',
'TreasuryMovementId':'Optional actual external payment link; ordinary earning/manual adjustment may be null. Ledger fact alone cannot prove cash moved.',
'IdempotencyKey':'Nullable filtered unique key in current model; cashout sets it, many other deterministic writers rely on Id instead. Null does not establish retry safety.',
'CreatedAtUtc':'Posting timestamp used range reports, distinct work/earning attendance date. UTC source clock and operational date may differ.'},
'AttendanceRecordEntity':{
'Id':'Attendance identity; correction locks row by ID and deterministic audit snapshot confirms replay.',
'EmployeeId':'Employee/date unique pair in current model, unlike per-shift daily wage key. No public-setter authorization.',
'EmployeeName':'Stored name snapshot for attendance display, can differ current employee profile.',
'Date':'Cairo business date for unique employee-day upsert, not stored UTC check-in calendar date.',
'CheckIn':'Nullable HH:mm string storing UTC time without date/offset. Repeated check-in overwrites; UTC-midnight checkout reconstruction lead preserved.',
'CheckOut':'Nullable UTC HH:mm string; repeated checkout overwrites hours while deterministic overtime credit may not adjust.',
'ShiftName':'Display label snapshot, no ShiftId FK in attendance entity; automatic no-open-shift label fallback not proof actual scheduled shift.',
'Status':'Raw string after correction case-insensitive parse without canonical storage; exact dashboard status comparisons may omit case/numeric values.',
'LateMinutes':'Signed int stored lateness, automatic clamps0; correction assigns request value without explicit nonnegative guard here.',
'WorkHours':'Double stored hours, automatic reconstructed UTC date calculation or raw correction request; no setter invariant.',
'OvertimeHours':'Double overtime inputs to PayrollRules and correction deltas, not proof actual approved overtime payment.',
'TatbiqCount':'Int extra-shift count metadata, correction replaces directly; no direct wage posting merely by setting property.',
'IsManualCorrection':'Metadata flag marks administrative correction; later biometric writes do not inherently erase every correction field.',
'CorrectedBy':'Verified correction authority display string, not issued approval token or immutable user identity.',
'CorrectionReason':'Optional text, correction command requires reason even though entity nullable.',
'LastCheckInEventId':'Last device event ID stored only, not unique replay admission guard; DTO documents future device dedup intention.',
'LastCheckOutEventId':'Last outgoing event marker, duplicate logic does not compare it before updating record.',
'Source':'Raw origin label such as biometric; no device authentication evidence implied by this string.',
'CreatedAtUtc':'Row creation timestamp; updating check-in/out/correction does not automatically revise creation time.'}}
records=[]
for s in source['symbols']:
 if not s['isCallable']:continue
 sig=s['qualifiedSymbol'];parts=sig.split('.');cls,field,access=parts[-3:]
 assert cls in fields and field in fields[cls] and access in ('get','set'),sig
 reason=('Getter returns stored backing value without query/recomputation. ' if access=='get' else 'Public setter assigns backing field without validation, actor check or persisted write by itself. ')+fields[cls][field]
 records.append({'stable_id':s['stable_id'],'status':'ANALYZED','signature':sig,'span':[s['startLine'],s['startColumn'],s['endLine'],s['endColumn']],
 'reasoning':reason,'inputs':'For setter typed assigned value; getter current backing field. Default null-forgiven references are not runtime nonnull validation.',
 'outputs':'Stored '+field+' value or field assignment, not derived financial operation.',
 'side_effects':'Plain object state only; EF tracking may later persist caller mutations on SaveChanges. Audit never instantiates live context or changes records.',
 'trust_boundaries':'Persistence DTO/accessor is not user authorization; command validation, foreign keys and transaction constraints are separate.',
 'database':'Current model EmployeeNumber unique, EmployeeLedger Id/nullable idempotency unique and employee index, attendance employee/date unique; no amount sign/enum check from these accessors.',
 'printing_ui':'Sensitive fields may feed staff DTOs; schema-only audit records no actual employee data, native UI or print.',
 'performance':'Autoaccessor constant field access; query/index/lock cost belongs caller, not accessor.',
 'duplicates_conflicts':'Typed persistence values correspond shared employee/ledger/attendance DTOs; raw string enum and cached balance differences documented field-specifically.',
 'issues_hypotheses':fields[cls][field],
 'tests_missing':'Provider mapping and valid financial state tests, enum/sign/finite bounds at command boundary, cached balance concurrent append, attendance date/time invariants; no trivial getter-mirroring tests proposed.',
 'verified_callers_callees':'Full EmployeeEntities source and Employee/EmployeeLedgerEntry Domain records read; current EF model employee/ledger/attendance blocks and reviewed payout/profile/attendance/query writers cross-referenced.'})
save_review('financial_coordinator',path,{'symbols':records,'scope':'All120explicit getter/setter source declarations, field-specific reasoning. Not compiled/generated CLR method count or live persistence certification.'})
print(json.dumps({'new_original_autoaccessor_receipts':len(records)}))
