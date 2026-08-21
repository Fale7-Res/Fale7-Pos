import type { User, UserRole } from './types';
export interface ManagerApproval { approvalId: string; approverUserId: string; approverName: string; approverRole: UserRole; approvedAt: string; action: string; reason: string }
export function hasActorPermission(actor: User | undefined, permission: string): boolean;
export function issueManagerApproval(input: { approver: User; action: string; reason: string; now?: string }): ManagerApproval;
export function authorizeCommand(input: { actor: User; permission: string; approval?: ManagerApproval; requireApproval?: boolean; consumeApproval?: boolean }): { actor: User; approver?: ManagerApproval };
export function verifyUserCredentials(users: User[], userId: string, pin: string): User | undefined;
export function authorizeShiftClose(input: { actor: User; approval?: ManagerApproval; discrepancyNeedsManager?: boolean }): void;
export function resetApprovalRegistryForTests(): void;
