export type Role = 'SuperAdmin' | 'CompanyAdmin' | 'BranchManager' | 'SupportUser';

export interface Plan {
  id: string;
  name: string;
  maxBranches: number;
  maxSupportUsers: number;
  priceMonthly: number;
  priceYearly: number;
  features: string[];
}

export interface Branch {
  id: string;
  name: string;
  address: string | null;
  phone: string | null;
  email: string | null;
  createdAt: string;
}

export interface SupportUser {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string | null;
  role: Role;
  branchIds: string[];
  isActive: boolean;
}

export interface Company {
  id: string;
  name: string;
  email: string | null;
  mobile: string;
  planId: string;
  createdAt: string;
}

export interface CompanyAdmin {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  role: Role;
  companyId: string;
}

export interface AuthUser {
  user: CompanyAdmin;
  company: Company;
  plan: Plan;
  role: Role;
}

export interface WidgetConfig {
  branchId: string;
  color: string;
  position: 'bottom-right' | 'bottom-left' | 'top-right' | 'top-left';
  scriptUrl: string;
}
