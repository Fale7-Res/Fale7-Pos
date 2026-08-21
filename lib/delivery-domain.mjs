export const DELIVERY_TRANSITIONS = {
  new: ['preparing', 'ready', 'cancelled'], preparing: ['ready', 'cancelled'], ready: ['assigned', 'cancelled'],
  assigned: ['handed_to_driver', 'ready'], handed_to_driver: ['out_for_delivery'],
  out_for_delivery: ['delivered', 'failed', 'returned'], delivered: ['settled'], failed: ['returned'],
  returned: ['settled'], settled: [], cancelled: [],
};

export function assertDeliveryTransition(from, to) {
  if (!DELIVERY_TRANSITIONS[from]?.includes(to)) throw new Error(`انتقال حالة الدليفري غير مسموح: ${from} → ${to}`);
}

export function deliveryCashResponsibility(paymentMethod, paymentStatus, amountPiastres) {
  if (paymentMethod === 'cash') return amountPiastres;
  return paymentStatus === 'paid' ? 0 : amountPiastres;
}

export function createDeliveryTrip({ tripId, driverId, orderIds, createdBy, createdAt }) {
  if (!driverId || !Array.isArray(orderIds) || orderIds.length === 0) throw new Error('يجب اختيار سائق وطلب واحد على الأقل للرحلة.');
  if (new Set(orderIds).size !== orderIds.length) throw new Error('لا يمكن تكرار الطلب داخل الرحلة.');
  return { id: tripId, driverId, orderIds, status: 'open', createdBy, createdAt };
}

export function createCashCustody({ custodyId, tripId, delivery, driverId, createdAt }) {
  const expectedAmountPiastres = deliveryCashResponsibility(delivery.paymentMethod, delivery.paymentStatus, delivery.amountToCollect);
  if (expectedAmountPiastres === 0) return undefined;
  return { id: custodyId, tripId, driverId, orderId: delivery.orderId, expectedAmountPiastres,
    collectedAmountPiastres: 0, settledAmountPiastres: 0, differencePiastres: 0, status: 'open', createdAt };
}

export function calculateTripSettlement(custodies, actualPiastres) {
  if (!Number.isInteger(actualPiastres) || actualPiastres < 0) throw new Error('النقدية الفعلية يجب أن تكون عدد قروش صحيحًا وغير سالب.');
  const unsettled = custodies.filter(item => item.status !== 'settled');
  const expectedPiastres = unsettled.reduce((sum, item) => sum + (item.collectedAmountPiastres || 0), 0);
  return { expectedPiastres, actualPiastres, differencePiastres: actualPiastres - expectedPiastres };
}

export function assertCollectionAmount(expectedPiastres, collectedPiastres, reason) {
  if (!Number.isInteger(collectedPiastres) || collectedPiastres < 0) throw new Error('المبلغ المحصل غير صالح.');
  if (expectedPiastres !== collectedPiastres && !String(reason || '').trim()) throw new Error('فرق التحصيل يتطلب سببًا واضحًا ولا يمكن تسويته تلقائيًا.');
}

export function assertShiftHasNoUnsettledCustody(custodies, deliveries, shiftId) {
  const deliveryIds = new Set(deliveries.filter(item => item.shiftId === shiftId).map(item => item.id));
  const blocked = custodies.filter(item => item.status !== 'settled' && deliveryIds.has(item.deliveryId || item.orderId));
  if (blocked.length) throw new Error(`لا يمكن إغلاق الوردية: توجد عهدة سائق غير مسوّاة لعدد ${blocked.length} طلب.`);
  return true;
}

export function deliveryReport(deliveries, trips, custodies, settlements) {
  const delivered = deliveries.filter(item => item.status === 'delivered' || item.status === 'settled');
  const durations = delivered.filter(item => item.outForDeliveryAt && item.deliveredAt)
    .map(item => new Date(item.deliveredAt).getTime() - new Date(item.outForDeliveryAt).getTime()).filter(value => value >= 0);
  return {
    orders: deliveries.length, delivered: delivered.length, failed: deliveries.filter(item => item.status === 'failed').length,
    returned: deliveries.filter(item => item.status === 'returned').length,
    deliveryFeesPiastres: deliveries.reduce((sum, item) => sum + (item.deliveryFeePiastres || 0), 0),
    averageDeliveryMinutes: durations.length ? Math.round(durations.reduce((a,b)=>a+b,0) / durations.length / 60000) : undefined,
    unsettledOrders: deliveries.filter(item => !['settled','cancelled'].includes(item.status)).length,
    expectedDriverCashPiastres: custodies.filter(item => item.status !== 'settled').reduce((sum,item)=>sum+(item.collectedAmountPiastres||0),0),
    settledCashPiastres: settlements.reduce((sum,item)=>sum+item.actualPiastres,0),
    shortagesPiastres: settlements.reduce((sum,item)=>sum+Math.max(0,-item.differencePiastres),0),
    overagesPiastres: settlements.reduce((sum,item)=>sum+Math.max(0,item.differencePiastres),0), trips: trips.length,
  };
}

export function driverOperationalStatus(driverId, trips, custodies) {
  if (custodies.some(item => item.driverId === driverId && item.status !== 'settled' && item.collectedAmountPiastres > 0)) return 'settlement_pending';
  const trip = trips.find(item => item.driverId === driverId && item.status !== 'settled');
  if (!trip) return 'available'; if (trip.status === 'out') return 'out'; if (trip.status === 'returned') return 'returning'; return 'assigned';
}

export function deliveryExceptions(deliveries, trips, custodies, settlements, now = Date.now(), oldMinutes = 90) {
  const result = [];
  for (const item of deliveries) {
    if (item.status === 'ready' && !item.driverId) result.push({ type:'ready_unassigned', severity:'warning', deliveryId:item.id, message:`طلب ${item.orderNumber || item.orderId} جاهز دون سائق` });
    if (item.status === 'failed' || item.status === 'returned') result.push({ type:item.status, severity:'warning', deliveryId:item.id, message:`طلب ${item.orderNumber || item.orderId}: ${item.status}` });
    if (item.status === 'out_for_delivery' && item.outForDeliveryAt && now-new Date(item.outForDeliveryAt).getTime()>oldMinutes*60000) result.push({type:'out_too_long',severity:'warning',deliveryId:item.id,message:`طلب ${item.orderNumber || item.orderId} خارج للتوصيل منذ وقت طويل`});
  }
  for (const custody of custodies.filter(item=>item.status!=='settled' && item.collectedAmountPiastres>0)) result.push({type:'cash_unsettled',severity:'critical',tripId:custody.tripId,message:`نقدية سائق محصلة وغير مسوّاة: ${custody.collectedAmountPiastres} قرش`});
  for (const trip of trips.filter(item=>item.status==='returned')) result.push({type:'trip_unsettled',severity:'critical',tripId:trip.id,message:`رحلة عادت ولم تتم تسويتها: ${trip.id}`});
  for (const settlement of settlements.filter(item=>item.differencePiastres!==0)) result.push({type:settlement.differencePiastres<0?'driver_shortage':'driver_overage',severity:'critical',tripId:settlement.tripId,message:`فرق تسوية سائق: ${settlement.differencePiastres} قرش`});
  return result;
}
