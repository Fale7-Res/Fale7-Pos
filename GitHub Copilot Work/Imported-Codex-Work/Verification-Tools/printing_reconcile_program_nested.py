from pathlib import Path
import json
from knowledge_store import WORK,load_symbols,save_review
from printing_knowledge_continuation import A,record,checkpoint
p='src/Falah.Server/Program.cs';target=WORK/'Knowledge/Reviews'/A/(p+'.json');old=json.loads(target.read_text());done={r['stable_id'] for r in old['symbols']};src=(WORK.parent/p).read_text(encoding='utf-8-sig').splitlines();notes={
36:'Sets Windows service name FalahServer in host options only. Normal executable mode remains supported; configuring service integration doesnot itself install/start a Windows service. Hardcoded name can differ from installer customname; deploy identity mismatch must be tested.',
51:'Empty AddRateLimiter option action registers infrastructure without global limiter. Actual named policy is installed by later configuration, so no unannotated endpoint inherits login limit just because this callback runs.',
52:'Reads final IConfiguration values with default60seconds/10permits; sets429 and adds login policy. FinalDI configuration supports testoverrides. No positive/window range guard here, so invalid config can fail limiter construction; doesnot create universal limit.',
57:'Builds fixed-window partition key from RemoteIpAddress string or unknown. Requests sharingIP share rate budget; forwarded headers not consulted here. MultiIP guessing bypasses singleIP ceiling; it isnot peraccount lockout.',
59:'Factory allocates limiter options with capturedwindow/permitcount, QueueLimit0. Fixedwindow burst boundary and noqueue semantics apply; no record of peruserfailed attempts. Capturedinvalid config can cause runtime initialization failure.',
66:'Resolves final connectionstring Falah fromDI IConfiguration then fallsback SQLExpress/Falah and calls UseSqlServer. Lazy resolution respects later testconfig but default can mutate restaurantnamedDB when startup Migrate/Seed follows. No requestactor or pertenant database selection.',
192:'OUTER_INNER',
223:'Filters connectionstring parts for diagnostic target by Server/Database prefixes. Excludes ordinaryPassword/UserID parts but crude splitting and logexception can still expose externalexception details; no claim fullsecretredaction. This predicate only builds startupfailure log target, never changes connection.',
253:'OUTER_INNER',
313:'Queries only shift.Status==Open; FirstOrDefault returns one and hasno ordering. Correct sharedshift context relies on filteredunique open-shift constraint elsewhere, not this predicate. Read can race close; no mutation.',
315:'Looks up User.Id matching openedBy ID to display name. Missinguser becomesnull in outer expression and fallsback through DTO. No permission check or identity binding; this is historical/display lookup, not command authorization.',
534:'Looks up actorUserId query for auditview permission calculation. Equality is parameterizedEF, no rawSQL concatenation; security depends middlewarematching this queryclaim to session. Doesnot itself get principal fromItems.',
1482:'Looks up queryactor for orderlist visibility permission. Selectedrowpermissions determine fullhistory versus currentshift; queryclaim integrity upstream. No bodyJSON use, so SEC001 JSON proofalone doesnot establish querycasing bypass.',
1486:'Include navigation selects order.Items for list DTO snapshots; not an authorizationfilter. Can load manylineitems when outer historylistunbounded; avoids lazy N+1 but multiplies resultvolume.',
1489:'Selects first currentlyOpen shift for narroworderlist visibility. No openshift makes outer queryfalse; doesnot permit history when shiftmissing. Concurrentclose may alter which recordsvisible within request.',
1490:'FALSE_SHIFT',
1492:'Orders fullmatchinghistory by monotonicSequenceNumber descending. No Take/paging in containingroute; memory/SQLsort grow with accumulatedorderhistory. Doesnot change financialstate.',
1500:'INCLUDE_ID',
1508:'INCLUDE_NUMBER',
1520:'Selects ProductImage by routeproductId; parameterized match and null404 in outerroute. No versioncondition/cacheheader here despite comment claiming versionsegment; eachread can load storedimagebytes.',
1783:'Selects orders with electronicstatus capitalPending and nonnull electronicamount. Domainwire currently lowerpending; typicalcase-insensitive SQLcollation mayequate, so do not assertmissingallrows withoutcollationproof. Queryfilters actualleg amount not total.',
1784:'Projects nonnull electronicpaymentamount after priorpredicate, using nullforgiving Value. EF query should preserveguard; sum later canoverflow long at extremevolume. No independentorderprice recalculation.',
1790:'Includes all custodyrows whose statusisnotSettled, not onlyOutForDelivery. This deliberatelycapturescurrentunsettledobligations; whetherclosed/cancelled statuses requireexclusion depends domaincustodystates, not provenhere.',
1791:'Projects DriverId and ExpectedPiastres for unsettledcustody. Expected obligationdistinct actualcollectedcash; no userpersonalcontact data. Projection prevents loading fullrow and enableslaterdistinctdrivercount.',
1796:'Sums balances alreadycomputed by fivesequentialaccountreads. Inmemorysum iscorrect for suppliedrows but cannot create atomicsnapshot; concurrenttransfer betweenreads canmake overallposition impossible. No storedrunningtotal updated.',
1798:'SUM_DRIVER',
1813:'Optionalaccountfilter includes movements where requestedaccount is sourceORdestination, preservingbothdebit/credit visibility. Unlike singlebalance endpoint, no catalogcheck, so unknownaccount yields emptyledger. All statuses remain included by outerquery.',
1817:'Filters CreatedAtUtc using Cairoday UTC half-openstart/end. This answers creationcalendarwindow rather than storedBusinessDayId attribution; prioropenbusinessday records mayappear byactualwriteclock, which UI must label.',
1820:'Orders movements descendingCreatedAtUtc before boundedTake1..500/default100. Equal timestamps have no ID tiebreak; cursorpaging absent. This limitsrows but still needsindex for largehistory.',
1821:'Selects ReferenceType/ReferenceId pair from alreadyboundedmovementrows for batchedhumanlabelresolver. Retains categoryso nonorderreferences areexcludeddownstream; no onequerypermovement.',
1822:'Builds TreasuryMovementDto carrying both internalreferenceID and optionalhumanorderlabel. Uses dictionary GetValueOrDefault on ReferenceId, regardless category; possibleIDcollision with nonorderreference can attach misleadinglabel if sameIDstringexists in dictionary. No ledgertruth mutated.',
1830:'Orders handoverhistory byCreatedAtUtc; positive callerlimit hasnoupppercap in outerTake. Predicate iskeyselection notpermission; endpoint lacksreportsgate. Memory/response canbe large.',
1831:'Projects handover accountIDs, amount, names,shift,notes andmovementID/time into anonymousDTO. No credentialfields, but financial/personnelmetadata exposedtoauthenticatedcaller; no endpoint-specificgate.',
1840:'Returns activeusersonly for listsummary; no hash/PIN columns selectedatEFlevel beforeToList but summarymappingexcludes them fromresponse. Names/roles/permissionIDs remain discoverable. Doesnot providepreloginaccess becausemiddlewareblocksroute.',
1855:'Selects allactiveuserrows for PIN-onlylogin KDFscan; disabledusers omitted. Fullsalt/hashloadedinternally but notresponse. Cost scalesstaffcount times slowhash perattempt; IPpolicybounds butotherapprovalendpoint separate.',
1856:'Verifies suppliedPIN against eachcandidaterandomsalt/hash/iterations until firstmatch; no equalitylookup possible. Doesnot authorize userIDclaim. DuplicatePINuniqueness mustbe enforcedelsewhere; FirstOrDefault wouldchoosefirstifduplicatelegacydata.',
2274:'Keeps only referencetype exactlyorder and nonemptyID for humanlabelbatch. Case-sensitive in-memorystringcomparison couldskip noncanonicalOrder; records areexpectedcanonical. Doesnot query/mutate yet.',
2275:'Projects orderreferenceID then Distinct removesduplicates, reducing parameterlist/querycost. OrderID isnot itself displaylabel. Pure in-memoryoperation on boundedupstreamreferences.',
2278:'EF filters Orders to IDs in deduplicatedbatch via Contains; parameterized collectiontranslation, no manuallybuiltSQL. Largecallerset outsideboundedroute couldinflateparameters; no direct limitwithinhelper.',
2279:'KEY_VALUE',
2284:'Loads actorrow namedbyreportshelper input. Activeandpermission checks occur outsidepredicate/HasPermissionhelper? Herehelper checks permissions but doesnot independentlyActivefilter; globalvalidsessionforgenuineactor shouldcoveractive ifidentitybound. Queryclaim distinction retained.',
2297:'Loads actorrow namedbyusersmanagehelper. Itdoesnot consultHttpContextactoror requireActivehere; thenall/users_manage stringcheck. Trustedqueryclaim matching isexternal, so absenceofbodycasinghereisnotprotection ofothercommands.',
}
out=list(old['symbols']);new=0
for s in load_symbols(p)['symbols']:
 if not s['isCallable'] or s['stable_id'] in done:continue
 line=s['startLine'];snippet='\n'.join(src[line-1:s['endLine']]);snippet=snippet[s['startColumn']-1:] if s['startLine']==s['endLine'] else snippet
 reason=notes[line]
 if line==192:
  reason=('OuterCORSoptions callback installsdefaultpolicy whoseinnerbuilder permitsAnyOrigin/Method/Header; doesnot set AllowCredentials. Configuringdefaultpolicy mayenable cross-origin APIaccess, but bearerrequirements stillapply. No browserrequestsent.' if s['startColumn']<75 else 'InnerdefaultCORSbuilder permits arbitraryOrigin/Method/Header, no credentialcookies. Itdoesnot relax authentication byitself; a maliciousorigin stillneeds validBearer. Broadpolicy expandsreachablebrowserclients, so networkbinding/tokenstorage determineeffect.')
 elif line==253:
  reason=('Outerexceptionbranch attaches terminal Run delegate to exceptionpipeline; itdoesnot wrap earlierAuthenticationMiddleware because registrationorderisafterit. Errors in middleware tokenresolution/bodyparse mayescape thiscustomJSONtranslation.' if s['startColumn']<60 else 'Innerexceptionhandler reads capturedfeature/TraceIdentifier, mapsBadHttpRequest tostatus withgenericArabicerror+reference, logsothers andresponds500. No businessretry or rollback occurs; data mayalreadycommitbefore laterfailure, so clientmustpreserveidempotency.')
 elif line==1490:
  reason=('Predicate returns literalfalse when noOpenShift exists, suppressingallordersfornarrowviewer; doesnotfallthroughhistory. EFcanreducequerytoempty; purefilter, not write.' if '=> false' in snippet[:25] else 'Predicate scopes order.ShiftId to currentOpenShift.Id, regardless cashieridentity. This isintendedsharedshiftvisibility; no historicalorders when shiftchanged. CapturedopenShift resultisaseparateread, not transactionalsnapshot.')
 elif line in (1500,1508):
  reason=('Include selectsfrozenItems navigation for orderdetail/lookupDTO, avoidinglazyqueries butloadingallitems. Itdoesnotrestrictcallerpermissionandmustnotbe confusedwith visibilityfilter.' if '=> o.Items' in snippet[:35] else ('Matches exact Order.Id pathparameter; GUIDdifficultyisnot authorization. Endpointdoesnotrepeatorderlistshiftpermission, so knownIDreadcanbypasslistvisibilitypolicy.' if line==1500 else 'Matches exacthumanOrderNumber for lookup, easierenumerablethanknownGUID. No currentshift/userscopefilterhere; null404otherwise. DBcollationdeterminescasecomparison.'))
 elif line==1798:
  reason=('Sums ExpectedPiastres acrossunsettledcustodyrows; expectedmoneyowed notphysicallyheldcash. Querysnapshotseparatefrombalances/pending, and no uncheckedvaluevalidationdonehere.' if 'ExpectedPiastres' in snippet[:40] else 'Projects DriverId beforeDistinct.Count so multipleunsettledorderssameDrivercountonce. A null/invaliddriverID wouldformonegroup; domainassignmentintegritymustpreventit. This isaggregateidentity, notdriverauthorization.')
 elif line==2279:
  reason=('Dictionary keyselector uses order.Id, assumeduniqueprimarykey; duplicatekey wouldthrow ratherthansilentlyoverwrite. ResultmapsstableinternalIDtohumannumber for displayonly.' if '=> o.Id' in snippet[:25] else 'Dictionary valueselector returns persisted OrderNumber ratherthan derivingcurrentsequenceformat. Historicalhumanreferencepreserved; no moneyororderstate changes.')
 db='EF predicate/projection evaluated by containingquery; no ownwrite' if line in (313,315,534,1482,1486,1489,1490,1492,1500,1508,1520,1783,1784,1790,1791,1813,1817,1820,1830,1840,1855,2278,2279,2284,2297) else ('ConfiguresSQLcontext; actualstartup migrations elsewhere' if line==66 else 'No ownDBoperation; containingroute/helper sideeffectseparate')
 trust='Captured route/query/body input and middlewareprincipal mustnot be conflated; identity/securityeffect only as reasoned' if line>=287 else 'Frameworkoptions/exception/config boundary; no directcalleridentityassertion unless described'
 out.append(record(s,reason,'Actual source lambda syntax at '+str(line)+':'+str(s['startColumn'])+'; capturedvalues per reasoning','Predicate/projection/options/result described individually above','Expression itself doesnot executeoutsidecontainingpipeline; described registration/log/response effects only',trust,db,'Read-model/config/error behavior affects clients; no hardwareinteraction','Per-element/expression cost; queryvolume andKDF/crosssnapshot issues explicitlyreasoned','StableID includessyntaxcolumn; same-line Include/predicate andkey/value lambdas distinct','No newruntimeclaim; case/attribution/dataavailability hypotheses stated above','Pure/isolatedquerytranslation andinvalid/auth/ordering/exception fixtures; no liveDBtests',[f'Program.cs source line{line}', 'Containing startup callback/registeredendpoint/utilityhelper; source reviewed']))
 new+=1
assert new==49,new
save_review(A,p,{'scope':'All241 Program callables now individuallyreconciled:188routes+4helpers+49nested/options/exception/querycallbacks','symbols':out})
checkpoint(p+'all241 callables','89remaining stablecallables in printagent/preview and exceptioncontracts; preserve SEC001 matrix and parentharnessboundary')
print('Saved49 individuallyreasoned nestedProgramcallables')
