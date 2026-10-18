from ui_source_receipts import *
sets=[('Services/ApiClient.Menu.cs',[
(9,10,54,'GET /api/categories/all','Reads all category authoring DTOs includinginactive, through SendAsync rather than ReadAsync; transport read errors misleadinglyunknown-operation copy.'),
(11,12,53,'GET /api/products/all','Reads all product/variant detail for editor, not active menuonly. No repricing or stock mutation; permission scope backend.'),
(13,14,41,'POST /api/categories','Creates category from immutable DTO/key, typed result normalized by MenuEditor Confirm. No automatic retry or keygeneration here.'),
(15,16,24,'PUT /api/categories/{escaped CategoryId}','Updates category via empty-success wrapper; no returned refreshed DTO, editor reloads. Escapedroute prevents slash query injection.'),
(17,18,24,'POST /api/categories/{escaped CategoryId}/active','Posts category state DTO unchanged, empty acknowledgment. Does not cascade availability locally; backend rules own products under disabledcategory.'),
(19,20,40,'POST /api/products','Creates product with captured variant/recipe DTO; server validates links/price, returns typed ID. No client sequence/ID inference.'),
(21,22,24,'PUT /api/products/{escaped ProductId}','Updates product/variants unchanged through empty success. UI021 wrongtarget header/fields can reach this endpoint if caller state raced; backend variant-parent validation mitigation must be challenged.'),
(23,24,42,'PUT /api/products/{escaped ProductId}/image','Posts image DTO JSON including base64 bytes and actor; not multipart. Transport preserves captured productA while UI021 continuation retargets editor. Server validates content/size; no actual image uploaded.'),
(25,26,24,'DELETE /api/products/{escaped productId}/image?actorUserId=escaped','Escapes productpath and assertedactor query. Shared bearer authority server-side; no requestbody/key, empty acknowledgment. UI021 deletion targetrace caller-owned.'),
(27,28,40,'POST /api/tax-rules','Creates tax DTO through typed result, backend validates rate/mode/active rules. No client tax computation from request here.'),
(29,30,24,'PUT /api/tax-rules/{escaped TaxRuleId}','Updates tax metadata via empty-success wrapper; server must maintain financial-history rules and permission. No local active-tax selection.'),
(31,32,33,'POST /api/price-rules','Saves create/update price rule DTO to one endpoint, preserving captured ID/key/schedule. API does not evaluate overlap or timezone, backend authoritative.')]),
('Services/ApiClient.People.cs',[
(16,17,32,'POST /api/employees','Creates employee with full compensation/lifecycle/PII DTO, no logs in transport. Server validates employees_manage/uniqueness; no realperson record used.'),
(19,20,32,'PUT /api/employees/{escaped EmployeeId}','Updates entire captured employee record and returns EmployeeDto. No partial merge clientside; editor snapshots all fields to preserve independent compensationfigures.'),
(25,26,54,'GET /api/delivery/services/all','Reads active/retired service directory for authoring, unlike active customerchoices. Uses commandwrapper for GET with misleadingunknown copy.'),
(28,29,39,'POST /api/delivery/services','Creates delivery service DTO, fee/backendvalidation authoritative. Does not derive fee from area or create a separate backend.'),
(31,32,39,'PUT /api/delivery/services/{escaped ServiceId}','Updates service record with escaped ID and unchanged DTO. Existing selected service snapshots in other VMs not automatically refreshed.'),
(40,41,40,'GET /api/business-day/cash-count-denominations','Reads allowed physical denominations, distinct drawerbalance inquiry. No cashcount saved by read; future denominations supported through list.'),
(43,44,60,'GET /api/business-day/{escaped businessDayId}/cash-counts','Reads saved denomination counts for day; does not compute/alter ledger. UI must match selectedday after asynchronous read.'),
(47,48,45,'POST /api/business-day/{escaped BusinessDayId}/cash-counts','Sends physical count DTO with notequantity; server computes total/difference. Actual human count must not be fabricated; audit invokesnone.'),
(55,56,36,'POST /api/backups/{escaped BackupRecordId}/restore','Destructive restore adapter forwards exactrecord/reason/phrase/actor/approval unchanged through unknown-aware SendAsync. No client-safe retry proof from transport alone. NEVER invoked by reviewer.'),
(61,63,38,'GET /api/reports/owner-dashboard?actorUserId&from&to','One aggregate owner read replaces multiple metricqueries. Escapesactor but formatsdates ambientculture yyyy-MM-dd; no explicitInvariant calendar. Backend reports_view/scope authoritative.')])]
for suffix,pairs in sets:
 facts={}
 for line,ll,col,route,reason in pairs:
  facts[(line,5)]=(reason+' CancellationToken passed through shared wrapper.',route+' -> shared SendAsync/SendEmptyAsync according to source','Exactverb/path/DTO/token and error classification mock; no liveHTTP.')
  facts[(ll,col)]=('Exact send delegate '+route+'. '+reason+' Uses one shared HttpClient call per invocation, no keyrotation or UI selection check.',route+' -> HttpClient.Get/Post/Put/Delete method as exactbody','Caller generation/actor validation and malformed response tests separately required.')
 persist_exact(P+suffix,facts,'Complete short API partial read; every exact adapter and sendlambda reconciled from actualbody. Destructive/PII/financial endpoints reviewed statically only.')
