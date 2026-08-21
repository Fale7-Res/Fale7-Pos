const issuedApprovals = new Map();
const consumedApprovals = new Set();

export const hasActorPermission = (actor, permission) => Boolean(actor?.active && (actor.permissions?.includes('all') || actor.permissions?.includes(permission)));

export const issueManagerApproval = ({ approver, action, reason, now = new Date().toISOString() }) => {
  const allowedRole = approver?.role === 'owner' || approver?.role === 'manager' || (approver?.role === 'supervisor' && hasActorPermission(approver, action));
  if (!allowedRole || !approver.active) throw new Error('هذا المستخدم غير مخول لاعتماد العملية.');
  const approval = { approvalId: `approval-${crypto.randomUUID()}`, approverUserId: approver.id, approverName: approver.name, approverRole: approver.role, approvedAt: now, action, reason };
  issuedApprovals.set(approval.approvalId, approval);
  return approval;
};

export const authorizeCommand = ({ actor, permission, approval, requireApproval = false, consumeApproval = true }) => {
  if (hasActorPermission(actor, permission)) return { actor, approver: undefined };
  if (!requireApproval || !approval) throw new Error('ليس لديك صلاحية تنفيذ هذه العملية.');
  const issued = issuedApprovals.get(approval.approvalId);
  if (!issued || issued.action !== permission || issued.approverUserId !== approval.approverUserId || consumedApprovals.has(approval.approvalId)) throw new Error('اعتماد المدير غير صالح أو لا يطابق العملية.');
  if (consumeApproval) consumedApprovals.add(approval.approvalId);
  return { actor, approver: issued };
};

export const verifyUserCredentials = (users, userId, pin) => users.find(user => user.id === userId && user.active && user.pin === pin);

export const authorizeShiftClose = ({ actor, approval, discrepancyNeedsManager = false }) => {
  authorizeCommand({ actor, permission: 'shifts_blind_close' });
  if (discrepancyNeedsManager && actor.role !== 'owner' && actor.role !== 'manager') {
    authorizeCommand({ actor, permission: 'shifts_manage', approval, requireApproval: true });
  }
};

export const resetApprovalRegistryForTests = () => { issuedApprovals.clear(); consumedApprovals.clear(); };
