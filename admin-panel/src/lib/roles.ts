import type { Role } from '@/types';

export const ROLE_LABEL: Record<Role, string> = {
  SuperAdmin: 'Super Admin',
  CompanyAdmin: 'Company Admin',
  BranchManager: 'Branch Manager',
  SupportUser: 'Support User',
};

export const ROLE_BADGE_COLOR: Record<Role, string> = {
  SuperAdmin: 'bg-purple-100 text-purple-800',
  CompanyAdmin: 'bg-blue-100 text-blue-800',
  BranchManager: 'bg-amber-100 text-amber-800',
  SupportUser: 'bg-slate-100 text-slate-800',
};

export function canManageBranches(role: Role): boolean {
  return role === 'SuperAdmin' || role === 'CompanyAdmin';
}

export function canManageSupportUsers(role: Role): boolean {
  return role === 'SuperAdmin' || role === 'CompanyAdmin' || role === 'BranchManager';
}

export function canViewPlanUsage(role: Role): boolean {
  return role === 'SuperAdmin' || role === 'CompanyAdmin';
}

export function canManageWidget(role: Role): boolean {
  return role === 'SuperAdmin' || role === 'CompanyAdmin' || role === 'BranchManager';
}
