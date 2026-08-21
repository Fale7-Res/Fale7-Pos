const coreArrayKeys = Object.freeze([
  'users','devices','categories','products','orders','grillTickets','employees',
  'ledgerEntries','shifts','expenses','cashMovements','attendance','auditEvents',
]);

const enterpriseArrayKeys = Object.freeze([
  'treasuryAccounts','treasuryMovements','cashHandovers','suppliers','supplierLedger',
  'purchases','inventoryItems','stockMovements','payrollRuns','payrollLines','leaveRequests',
  'taxRules','businessDays','drivers','deliveries','deliveryTrips','driverCustodies',
  'deliveryReassignments','deliveryEvents','driverSettlements','diningTables',
  'dailyCloseSnapshots','cashDenominationCounts','checklistItems','checklistCompletions',
  'controlAuditEvents',
]);

const own = (value, key) => Object.prototype.hasOwnProperty.call(value, key);

function normalizeArrays(candidate, defaults, keys, label) {
  const normalized = { ...defaults, ...candidate };
  for (const key of keys) {
    if (!own(candidate, key)) normalized[key] = defaults[key] ?? [];
    else if (!Array.isArray(candidate[key])) throw new Error(`${label}.${key} must be an array`);
    else normalized[key] = candidate[key];
  }
  return normalized;
}

export function normalizeCoreState(candidate, defaults) {
  if (!candidate || typeof candidate !== 'object' || Array.isArray(candidate)) throw new Error('core state must be an object');
  const normalized = normalizeArrays(candidate, defaults, coreArrayKeys, 'core');
  if (own(candidate, 'settings') && (!candidate.settings || typeof candidate.settings !== 'object' || Array.isArray(candidate.settings))) throw new Error('core.settings must be an object');
  normalized.settings = { ...defaults.settings, ...(candidate.settings || {}) };
  return normalized;
}

const normalizeBusinessDay = day => {
  if (!day || typeof day !== 'object' || !day.id) throw new Error('enterprise.businessDays contains an invalid record');
  const businessDate = day.businessDate || day.date;
  return { ...day, businessDate, date:day.date || businessDate, closeRevision:Number.isInteger(day.closeRevision) ? day.closeRevision : 0 };
};

const normalizeSnapshot = snapshot => {
  if (!snapshot || typeof snapshot !== 'object' || !snapshot.id || !snapshot.businessDayId) throw new Error('enterprise.dailyCloseSnapshots contains an invalid record');
  return { ...snapshot, closeRevision:Number.isInteger(snapshot.closeRevision) ? snapshot.closeRevision : 1, authoritative:snapshot.authoritative !== false, exceptions:Array.isArray(snapshot.exceptions) ? snapshot.exceptions : [], checklist:Array.isArray(snapshot.checklist) ? snapshot.checklist : [] };
};

const normalizeTrip = trip => {
  if (!trip || typeof trip !== 'object' || !trip.id) throw new Error('enterprise.deliveryTrips contains an invalid record');
  return { ...trip, orderIds:Array.isArray(trip.orderIds) ? trip.orderIds : [] };
};

export function normalizeEnterpriseState(candidate, defaults) {
  if (!candidate || typeof candidate !== 'object' || Array.isArray(candidate)) throw new Error('enterprise state must be an object');
  const normalized = normalizeArrays(candidate, defaults, enterpriseArrayKeys, 'enterprise');
  if (own(candidate, 'exceptionStates') && (!candidate.exceptionStates || typeof candidate.exceptionStates !== 'object' || Array.isArray(candidate.exceptionStates))) throw new Error('enterprise.exceptionStates must be an object');
  normalized.exceptionStates = candidate.exceptionStates || {};
  normalized.businessDays = normalized.businessDays.map(normalizeBusinessDay);
  normalized.dailyCloseSnapshots = normalized.dailyCloseSnapshots.map(normalizeSnapshot);
  normalized.deliveryTrips = normalized.deliveryTrips.map(normalizeTrip);
  return normalized;
}

export const NORMALIZED_CORE_ARRAY_KEYS = coreArrayKeys;
export const NORMALIZED_ENTERPRISE_ARRAY_KEYS = enterpriseArrayKeys;
