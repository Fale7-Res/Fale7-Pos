from ui_source_receipts import *
# All 852 lines were read. Explicit exact source pairs include both forward and
# individually observed one-line throwing reverse bodies; no index-based promotion.
pairs=[
(10,13,'BoolToVisible','Only boxed true visible; false/null/wrong type collapsed. Does not coerce strings.'),
(19,22,'InverseBoolToVisible','Only true collapsed; false/null/wrong type visible, so unset value is fail-visible.'),
(30,33,'NullToCollapsed','Null or exactly empty string collapsed, whitespace visible. Boolfalse/nonempty object visible.'),
(43,46,'NullablePiastresToDisplay','Only long formats money; unknown/null or boxed int says unvalued, preserving unknown cost but requiring correct binding type.'),
(56,91,'TreasuryMovementTypeLabel','Maps stored snake_case ledger types to Arabic, null/empty blank and unknown string passed through. Does not change financial type identity; case-sensitive.'),
(119,120,'PaymentMethodLabel','Casts to string then helper maps case-insensitively. Wrong nonstring becomes blank; no two-way mapping to payment code.'),
(125,135,'UserRoleLabel','Maps owner/manager/supervisor/cashier case-sensitively, unknown raw label preserved. Display label does not grant privileges.'),
(141,159,'EmployeeLedgerTypeLabel','Maps PascalCase ledger-entry taxonomy including payroll and adjustments. Unknown raw values preserved, no ledger mutation.'),
(179,180,'StockMovementTypeLabel','Delegates to static stock label map; null/wrong type blank and unknown raw string visible. Waste wording differs Inventory row inline label but code identity unchanged.'),
(198,199,'PrintRouteLabel','Delegates six-route map including DeliveryOrder/DriverDispatch, unlike four-route Printing selector UI009. Label support alone does not expose queues.'),
(209,224,'SignificantActionTypeLabel','Requires full SignificantActionDto then chooses treasury/stock/delivery map by category; expense categories already Arabic passed through. Unknown DTO type blank, unknown event preserved.'),
(233,244,'TreasuryAccountLabel','Maps five fixed account IDs, null/empty outside-system. Unknown IDs raw; no client account authorization or balance calculation.'),
(251,254,'MilliUnitsToDisplay','Formats only boxed long via MilliUnits; wrong numeric type blank. Integer milliunits not piastres and must not use monetary formatter.'),
(263,266,'PositiveAmountToVisible','Visible only long strictlypositive, zero/negative/null/int collapsed. Relevant to pending amounts; wrong type can hide warning.'),
(276,293,'DeliveryStatusLabel','Maps PascalCase persisted delivery states; unknown string returned and nullblank. Distinct from lower-case order states; case mismatch exposes raw label.'),
(306,317,'OrderStatusLabel','Lowercases invariant then maps six order states, unknown original string retained. Does not conflate delivered delivery state with completed order.'),
(326,336,'OrderTypeLabel','Maps pickup/takeaway/delivery and historical dine_in/dinein to pickup label. Historical dine-in presented as renamed pickup per contract, no table data removed.'),
(348,379,'GeneralStatusLabel','Case-insensitive business/payroll/backup/severity labels, including Valid/Invalid plus legacy Verified. Actual BackupVM expects Verified and needs server status consistency challenge.'),
(388,391,'EmptyStringToVisible','Exact inverse null/empty visibility, but whitespace hides placeholder even if visually empty. Other object types hide placeholder.'),
(402,405,'ShiftDotBrush','Returns cached literal green/slate brushes for true/other. Not theme resources; visual contrast must be measured against actual header, no native screenshot.'),
(418,425,'BoolToBrush','Splits required string selected|unselected resource keys with no null/count validation; malformed XAML parameter throws. ThemeResources.Get returns concrete brush, no automatic reevaluation on theme-only change UI004.'),
(442,445,'MoneyOrUnknown','Long formats known money includingzero/negative; null/wrong numeric type says unknown. Distinguishes missing cost from actualzero.'),
(453,456,'PercentOrUnknown','Only double formatted invariant onefractional digit pluspercent; decimal/int says unknown. NaN/infinity are not rejected and can display special strings.'),
(464,467,'CountToVisible','Visible only positive int; long counts collapse even when positive. Binding type must agree, no general truthiness.'),
(477,480,'FileName','Nonempty string uses Path.GetFileName without filesystem access. Reduces backup table width; malformed platform path handling may throw, full path remains DTO.'),
(487,490,'LocalTimestamp','DateTimeOffset converts machine local and formats date/time, not Cairo explicitly. NonDTO timestamps dash; ambient calendar affects customdate pattern.'),
(498,510,'SeverityToBrush','bad/warn/info map to theme danger/warning/info, unknown maps SuccessBrush. New severity unknown can misleadingly green; source vocabulary expected fixed.'),
(519,530,'DeltaStateToBrush','up green/down red/other muted. Direction not desirability, so expense increase can green per comment; UX interpretation must be explicit.'),
(540,543,'FractionToWidth','Doublepositive becomes min4px/max150px track;zero/negative/wrongtype0. No responsive track knowledge, may overflow narrow column; NaNfalse, positiveinfinity150.'),
(549,552,'NullToBool','True for anynonnull except emptystring, including false/zero/whitespace. Appropriate nullable-message presence, not numeric/boolean value predicate.'),
(558,564,'BoolToLabel','Splits parameter trueText|falseText no validation then chooses only boxedtrue first. Missing separator/null throws; labels cannot contain unescaped pipe.'),
(570,577,'StringEqualsToBrush','Parameter expected|selected|unselected, ordinal string equality then concrete theme lookup. Malformed parameter throws; same UI004 theme-only reevaluation risk.'),
(585,592,'IntEqualsToBrush','Requires boxedint and parseable target part, then selects brush. int.TryParse uses currentculture; malformed parts count still throws. Concrete theme lookup UI004.'),
(600,603,'StringEqualsToVisible','Ordinal equality of value/parameter asstrings; both nonstrings castnull compareequal and becomevisible. Fixed string XAML parameters avoid ordinarynull/null path.'),
(610,613,'CountGreaterThanOneToVisible','Requires non-generic ICollection Count>1, not IReadOnlyCollection-only implementations. Current list DTO concrete type determines whether variant badge appears.'),
(622,632,'PiastresToDisplay','Long/int format, null dash, anyother type misleadingzero string. Nullablelong boxes aslong ornull. Currency display format uses ambient culture unlike CashInput invariant.'),
(649,652,'EmptyCollectionToVisible','Null or ICollection countzero visible, unknown collection type collapsed. Binding collection property itself may not update on contents; Count converter preferred.'),
(660,663,'NonEmptyCollectionToVisible','Visible only non-generic ICollection countpositive. ObservableCollection works but contents event alone may not re-run property converter; counts should be bound.'),
(678,693,'CairoBusinessDate','Accepts DateOnly, DateTimeOffset.DateTime, DateTime or culture-parsed string then Arabic weekday/date. Does not actually timezone-convert DTO despite name; correct if input date-only contract, wrong for arbitrary UTC instant near Cairo midnight.'),
(702,709,'TimeOfDay','DTO uses machine-local time, DateTime uses unchanged kind/time, both ar-EG12hour. No Cairo conversion; timestamp intent must match user machine locale.'),
(744,747,'NavKeyToGlyph','Looks up fixed navigation glyph by exact key, unknown genericglyph. No navigation/permission side effect; glyph font availability and accessible name belong view.'),
(758,764,'AttendanceStatusLabel','Lowercases known attendance states and returns original value or dash if unknown. MissingFingerprint key no separator; enum vocabulary needs matching DTO contract.'),
(769,785,'StatusToPillBrush','Substring heuristic checks success paid/delivered/active before danger/warning. Words unpaid/inactive contain success tokens and would green if used; current bound vocabulary must be traced before canonical defect. Concrete theme brush UI004.'),
(807,820,'ImageUriToSource','Caches BitmapImage by AbsoluteUri under lock,320decodewidth. RelativeUri.AbsoluteUri throws; unbounded versions remain processlifetime, failed download cached too. Native image HTTP does not shareApi bearer. No file/image downloaded in review.'),
(829,836,'NullToText','Returns nonblank string unchanged, otherwise parameter text/dash. Nonstring numeric value treated missing; intended nullable text only.'),
(846,849,'ZeroCountToVisible','Visible only boxedintzero, not null/longzero; binding collection.Count makes content changes observable. Unlike collectionconverter avoids stale empty overlays when correct source notification.')
]
facts={}
for forward,back,name,reason in pairs:
 facts[(forward,5)]=(reason+' TargetType/language generally ignored, presentation only.',name+'.Convert; related helper if described','Wrongtype/null/parameter/culture/theme and native binding tests; relevant callers must be verified separately.')
 facts[(back,5)]=('Exact '+name+' reverse method unconditionally throws NotSupportedException; forward display transformation intentionally has no inverse. Safe only in OneWay/OneTime bindings, and an accidental TwoWay binding can raise XAML conversion error rather than produce source value.',name+'.ConvertBack -> throw new NotSupportedException','Verify all actual consuming bindings use OneWay/OneTime; no native binding pass executed.')
facts[(108,5)]=('Payment helper lowercases invariant then maps cash/card/instapay/wallet/multi. Null/empty blank, unknown returns original spelling; display does not normalize requestmethod.','PaymentMethodLabelConverter.Label; Convert','Unknown and mixedcase wire identifiers.')
facts[(166,5)]=('Stock helper maps seven exact PascalCase types with explicit waste/spoilage/breakage labels, unknown unchanged. Label differs from inline inventory wording but cannot alter persisted taxonomy.','StockMovementTypeLabelConverter.Label; SignificantAction converter','New server types remain visible raw, no silentblank.')
facts[(186,5)]=('Print route helper maps allsix enum names case-sensitively; nullblank, unknown unchanged. Confirms UI009 route selector gap not missing localization.','PrintRouteLabelConverter.Label; Convert','Enum completeness and unknown routes.')
facts[(630,5)]=('Divides long piastres by decimal100 and interpolates twofraction digits with currentculture. No integer truncation; negative balances format signed. Unlike CashInput may use locale decimal separator, acceptable display not protocol.','PiastresToDisplayConverter.Format','Long extremes and ar-EG/en-US display.')
facts[(641,5)]=('Bool negation returns value is not true, so null/wrongtype become true. Useful busy inverse but fail-open if required bool binding unresolved.','InverseBoolConverter.Convert','Unset/null binding enabled-state expectation.')
facts[(642,5)]=('Reverse bool negation uses same predicate and supports TwoWay only for actual boolean values. Nonbool input becomes true and loses original type; no throw unlike other converters.','InverseBoolConverter.ConvertBack','TwoWay boolean roundtrip and wrongtype value fixture.')
persist_exact(P+'Converters/Converters.cs',facts,'Read complete852-line converter source; all98 exact methods manually reconciled including explicit forward/reverse line pairs and helpers. No native color/state observation; UI004 and substring/date hypotheses source-only.')
