export const EGYPTIAN_DENOMINATIONS_PIASTRES = Object.freeze([20000,10000,5000,2000,1000,500,200,100,50,25]);
export const CONTROL_THRESHOLDS = Object.freeze({ largeRefundPiastres:50000, largeExpensePiastres:100000, deliveryDelayMinutes:60, backupOverdueHours:24 });
export const ACTIVE_BUSINESS_DAY_STATUSES=Object.freeze(['open','closing','reopened']);
export const isActiveBusinessDay=day=>Boolean(day&&ACTIVE_BUSINESS_DAY_STATUSES.includes(day.status));
export function assertBusinessDayTransition(from,to){const legal={open:['closing'],reopened:['closing'],closing:['closed'],closed:['reopened']};if(!legal[from]?.includes(to))throw new Error(`انتقال حالة يوم العمل غير صالح: ${from} → ${to}`);return true}
export function buildOpenedBusinessDay({requestId,businessDate,actor,timestamp,businessDays=[]}){if(businessDays.some(isActiveBusinessDay))throw new Error('يوجد يوم عمل نشط بالفعل.');if(!/^\d{4}-\d{2}-\d{2}$/.test(businessDate))throw new Error('تاريخ العمل غير صالح.');return{id:`day-${requestId}`,date:businessDate,businessDate,openedAt:timestamp,openedBy:actor.name,status:'open',closeRevision:0}}
export function buildReopenedBusinessDay({day,businessDays=[],reason,actor,approval,timestamp}){if(day.status!=='closed')throw new Error('لا يمكن إعادة فتح يوم غير مغلق.');if(businessDays.some(item=>item.id!==day.id&&isActiveBusinessDay(item)))throw new Error('لا يمكن إعادة الفتح أثناء وجود يوم عمل نشط.');if(!day.snapshotId)throw new Error('لقطة الإغلاق الأصلية غير موجودة.');if(!reason?.trim())throw new Error('سبب إعادة الفتح مطلوب.');return{...day,status:'reopened',previousSnapshotId:day.snapshotId,previousClosedAt:day.closedAt,previousClosedBy:day.closedBy,reopenReason:reason.trim(),reopenedAt:timestamp,reopenedBy:actor.name,reopenApprovalId:approval.approvalId,reopenApprovedBy:approval.approverName}}
export const snapshotsForBusinessDay=(snapshots,id)=>snapshots.filter(s=>s.businessDayId===id).sort((a,b)=>a.closeRevision-b.closeRevision);
export const latestBusinessDaySnapshot=(snapshots,id)=>snapshotsForBusinessDay(snapshots,id).at(-1);

const amount = value => Number.isFinite(value) ? Math.round(value) : 0;
const stamp = value => String(value || '');
export const belongsToBusinessDay = (record, day, shifts=[]) => {
  if (!record || !day) return false;
  if (record.businessDayId) return record.businessDayId === day.id;
  if (record.shiftId) { const shift=shifts.find(item=>item.id===record.shiftId); if (shift?.businessDayId) return shift.businessDayId===day.id; }
  const value=stamp(record.createdAt || record.date || record.timestamp || record.openedAt);
  return !record.businessDayId && value.slice(0,10) === (day.businessDate || day.date);
};
export const selectBusinessDayRecords = (records, day, shifts=[]) => (records||[]).filter(item=>belongsToBusinessDay(item,day,shifts));

export function calculateDenominationCount(rows, expectedPiastres=0) {
  const normalized=(rows||[]).map(row=>({denominationPiastres:amount(row.denominationPiastres),quantity:Math.max(0,amount(row.quantity)),totalPiastres:amount(row.denominationPiastres)*Math.max(0,amount(row.quantity))}));
  const totalPiastres=normalized.reduce((sum,row)=>sum+row.totalPiastres,0);
  return {rows:normalized,totalPiastres,expectedPiastres:amount(expectedPiastres),differencePiastres:totalPiastres-amount(expectedPiastres)};
}

const sum=(items, fn)=>items.reduce((total,item)=>total+amount(fn(item)),0);
const movementNet=(items, accountId)=>sum(items,m=>(m.status==='posted'&&m.destinationAccountId===accountId?m.amount:0)-(m.status==='posted'&&m.sourceAccountId===accountId?m.amount:0));
export function reconcileBusinessDay(input) {
  const {day,orders=[],shifts=[],expenses=[],treasuryMovements=[],driverCustodies=[],driverSettlements=[],purchases=[],payrollRuns=[]}=input;
  if(!day) return {status:'balanced',expectedPiastres:0,actualPiastres:0,differencePiastres:0,checks:[],sales:{grossSalesPiastres:0,refundsPiastres:0,netSalesPiastres:0,orderCount:0,averageOrderValuePiastres:0},payments:{cashPiastres:0,cardPiastres:0,instapayPiastres:0},expenses:{totalPiastres:0},treasury:{balances:{'tre-main':0,'tre-drawer':0,'tre-bank':0,'tre-instapay':0},movementCount:0,ownerFundingPiastres:0,ownerWithdrawalsPiastres:0},delivery:{expectedPiastres:0,settledPiastres:0},shifts:{count:0,expectedPiastres:0,actualPiastres:0,shortagePiastres:0,overagePiastres:0},purchasing:{paidPiastres:0},payroll:{paidPiastres:0}};
  const dayShifts=selectBusinessDayRecords(shifts,day,shifts), dayOrders=selectBusinessDayRecords(orders,day,dayShifts), dayExpenses=selectBusinessDayRecords(expenses,day,dayShifts), movements=treasuryMovements.filter(m=>m.businessDayId===day.id&&m.status==='posted');
  const sales=dayOrders.filter(o=>o.status!=='cancelled'), grossSalesPiastres=sum(sales,o=>Math.round(o.total*100)), refundsPiastres=sum(sales,o=>Math.round((o.refundAmount||0)*100)), netSalesPiastres=grossSalesPiastres-refundsPiastres;
  const paymentPiastres=method=>sum(sales.filter(o=>o.paymentMethod===method),o=>Math.round((o.total-(o.refundAmount||0))*100));
  const expectedDrawerPiastres=sum(dayShifts,s=>Math.round((s.expectedCash??0)*100));
  const actualDrawerPiastres=sum(dayShifts,s=>Math.round((s.actualCash??0)*100));
  const expectedDriverPiastres=sum(driverCustodies.filter(c=>c.status!=='settled'&&dayOrders.some(o=>o.id===c.orderId)),c=>c.expectedAmountPiastres);
  const settledDriverPiastres=sum(driverSettlements.filter(s=>movements.some(m=>m.referenceId===s.tripId)),s=>s.actualPiastres);
  const checks=[
    {code:'DRAWER_RECONCILIATION',source:'shifts',expectedPiastres:expectedDrawerPiastres,actualPiastres:actualDrawerPiastres,differencePiastres:actualDrawerPiastres-expectedDrawerPiastres,severity:actualDrawerPiastres===expectedDrawerPiastres?'balanced':'critical',affectedEntityIds:dayShifts.filter(s=>s.cashDifference).map(s=>s.id)},
    {code:'DELIVERY_CUSTODY',source:'driverCustody',expectedPiastres:expectedDriverPiastres,actualPiastres:0,differencePiastres:-expectedDriverPiastres,severity:expectedDriverPiastres?'critical':'balanced',affectedEntityIds:driverCustodies.filter(c=>c.status!=='settled').map(c=>c.id)},
  ];
  const keys=new Map(); for(const m of treasuryMovements){keys.set(m.idempotencyKey,(keys.get(m.idempotencyKey)||0)+1)}
  const duplicates=[...keys].filter(([,count])=>count>1).map(([key])=>key); if(duplicates.length)checks.push({code:'DUPLICATE_IDEMPOTENCY_KEY',source:'treasury',expectedPiastres:0,actualPiastres:duplicates.length,differencePiastres:duplicates.length,severity:'critical',affectedEntityIds:duplicates});
  for(const accountId of ['tre-main','tre-drawer','tre-bank','tre-instapay']){const balance=movementNet(treasuryMovements,accountId);if(balance<0)checks.push({code:'NEGATIVE_PROTECTED_BALANCE',source:'treasury',expectedPiastres:0,actualPiastres:balance,differencePiastres:balance,severity:'critical',affectedEntityIds:[accountId]})}
  const status=checks.some(c=>c.severity==='critical')?'critical':checks.some(c=>c.severity==='warning')?'warning':'balanced';
  return {status,expectedPiastres:expectedDrawerPiastres+expectedDriverPiastres,actualPiastres:actualDrawerPiastres,differencePiastres:actualDrawerPiastres-expectedDrawerPiastres-expectedDriverPiastres,checks,
    sales:{grossSalesPiastres,refundsPiastres,netSalesPiastres,orderCount:sales.length,averageOrderValuePiastres:sales.length?Math.round(netSalesPiastres/sales.length):0},
    payments:{cashPiastres:paymentPiastres('cash'),cardPiastres:paymentPiastres('card'),instapayPiastres:paymentPiastres('instapay')},
    expenses:{totalPiastres:sum(dayExpenses,e=>Math.round(e.amount*100))},
    treasury:{balances:Object.fromEntries(['tre-main','tre-drawer','tre-bank','tre-instapay'].map(id=>[id,movementNet(treasuryMovements,id)])),movementCount:movements.length,ownerFundingPiastres:sum(movements.filter(m=>['owner_deposit','capital_injection','other_external_inflow'].includes(m.type)),m=>m.amount),ownerWithdrawalsPiastres:sum(movements.filter(m=>m.type==='owner_withdrawal'),m=>m.amount)},
    delivery:{expectedPiastres:expectedDriverPiastres,settledPiastres:settledDriverPiastres},
    shifts:{count:dayShifts.length,expectedPiastres:expectedDrawerPiastres,actualPiastres:actualDrawerPiastres,shortagePiastres:sum(dayShifts.filter(s=>(s.cashDifference||0)<0),s=>Math.abs(Math.round(s.cashDifference*100))),overagePiastres:sum(dayShifts.filter(s=>(s.cashDifference||0)>0),s=>Math.round(s.cashDifference*100))},
    purchasing:{paidPiastres:sum(purchases.filter(p=>movements.some(m=>m.referenceId===p.id)),p=>p.paid||0)}, payroll:{paidPiastres:sum(payrollRuns.filter(p=>p.status==='paid'&&movements.some(m=>m.referenceId===p.id)),p=>movements.find(m=>m.referenceId===p.id)?.amount||0)} };
}

export function evaluateCloseBlockers(input) {
  const blockers=[]; const push=(code,severity,entityType,entityId,message,actionTarget)=>blockers.push({code,severity,entityType,entityId,message,actionTarget});
  for(const shift of input.shifts||[]) if(shift.status!=='closed') push('OPEN_SHIFT','critical','shift',shift.id,'توجد وردية كاشير غير مغلقة.','shifts');
  for(const trip of input.deliveryTrips||[]) if(trip.status!=='settled') push('UNSETTLED_DELIVERY_TRIP','critical','delivery_trip',trip.id,'توجد رحلة دليفري لم تتم تسويتها.','delivery');
  for(const custody of input.driverCustodies||[]) if(custody.status!=='settled') push('UNSETTLED_DRIVER_CUSTODY','critical','driver_custody',custody.id,'توجد عهدة سائق غير مسواة.','delivery');
  for(const entry of input.journalEntries||[]) if(entry.status!=='complete') push(`JOURNAL_${String(entry.status).toUpperCase()}`,'critical','journal',entry.operationId,'توجد عملية مالية غير مكتملة في سجل الاسترداد.','backup');
  if(input.persistenceHealthy===false) push('PERSISTENCE_UNHEALTHY','critical','system','persistence','التخزين المحلي غير سليم.','backup');
  if(input.writerOwned===false) push('WRITER_NOT_OWNED','critical','system','writer','هذه النافذة ليست الكاتب المالي الحالي.','settings');
  for(const item of input.requiredChecklist||[]) if(!item.completed) push('CHECKLIST_INCOMPLETE','critical','checklist',item.id,`بند الإغلاق غير مكتمل: ${item.label}`,'daily-close');
  for(const check of input.reconciliation?.checks||[]) if(check.severity==='critical') push('RECONCILIATION_CRITICAL','critical','reconciliation',check.code,'يوجد فرق مالي حرج يحتاج تسوية فعلية.','control-center');
  return blockers;
}

export function createDailyCloseSnapshot(input) {
  if((input.blockers||[]).some(b=>b.severity==='critical')) throw new Error('لا يمكن إغلاق يوم العمل قبل معالجة الموانع المالية الحرجة.');
  const revision=(input.existingSnapshots||[]).filter(s=>s.businessDayId===input.day.id).length+1;
  const snapshot={id:`snapshot-${input.day.id}-r${revision}`,snapshotVersion:1,closeRevision:revision,authoritative:true,previousSnapshotId:latestBusinessDaySnapshot(input.existingSnapshots||[],input.day.id)?.id,businessDayId:input.day.id,businessDate:input.day.businessDate||input.day.date,generatedAt:input.timestamp,closedAt:input.timestamp,closedBy:input.actor.name,operationId:`business-close-${input.day.id}-r${revision}`,reconciliation:input.reconciliation,cashCount:input.cashCount||null,exceptions:input.exceptions||[],checklist:input.checklist||[]};
  const freeze=value=>{if(value&&typeof value==='object'&&!Object.isFrozen(value)){Object.values(value).forEach(freeze);Object.freeze(value)}return value}; return freeze(snapshot);
}

export function buildControlExceptions(input) {
  const cfg={...CONTROL_THRESHOLDS,...input.thresholds}, out=[];
  const add=(id,category,severity,message,entityType,entityId)=>out.push({id,category,severity,message,entityType,entityId,status:input.states?.[id]?.status||'open',...input.states?.[id]});
  for(const s of input.shifts||[]) if(s.status==='closed'&&s.cashDifference) add(`shift-${s.id}`,'financial',Math.abs(s.cashDifference*100)>=5000?'critical':'warning',s.cashDifference<0?'عجز في الوردية':'زيادة في الوردية','shift',s.id);
  for(const o of input.orders||[]) if(Math.round((o.refundAmount||0)*100)>=cfg.largeRefundPiastres) add(`refund-${o.id}`,'orders','warning','مرتجع كبير يحتاج مراجعة','order',o.id);
  for(const e of input.expenses||[]) if(Math.round(e.amount*100)>=cfg.largeExpensePiastres) add(`expense-${e.id}`,'expenses','warning','مصروف كبير يحتاج مراجعة','expense',e.id);
  for(const c of input.driverCustodies||[]) if(c.status!=='settled') add(`custody-${c.id}`,'delivery','critical','عهدة سائق غير مسواة','driver_custody',c.id);
  for(const j of input.journalEntries||[]) if(j.status!=='complete') add(`journal-${j.operationId}`,'system','critical','عملية مالية معلقة أو تحتاج استردادًا','journal',j.operationId);
  return out;
}

export const updateExceptionState=(states,id,status,actor,reason,timestamp)=>({...(states||{}),[id]:{status,actor:actor.name,reason,timestamp}});
export function buildSafeDropMovement(input) {
  if(!Number.isInteger(input.amountPiastres)||input.amountPiastres<=0) throw new Error('مبلغ الإيداع الآمن يجب أن يكون صحيحًا وأكبر من صفر.');
  return {id:input.movementId,idempotencyKey:`safe-drop-${input.dropId}`,operationId:input.operationId,type:'safe_drop',sourceAccountId:'tre-drawer',destinationAccountId:'tre-main',amount:input.amountPiastres,referenceType:'safe_drop',referenceId:input.dropId,businessDayId:input.businessDayId,shiftId:input.shiftId,reason:input.reason,createdBy:input.actor.name,approvedBy:input.approval?.approverName,createdAt:input.timestamp,status:'posted'};
}
