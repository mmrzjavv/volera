import { apiRequest, getBaseUrl } from '@/lib/api';

const PREFIX = '/api/v1/company';

export interface RegisterCompanyRequest {
  name: string;
  mobileNumber: string;
  email?: string | null;
  address?: string | null;
}

export interface RegisterCompanyResponse {
  companyId: string;
  token: string;
  expiresAt: string;
}

export interface CompanyLoginRequest {
  mobileNumber: string;
  token: string;
}

export interface CompanyLoginResponse {
  companyId: string;
  token: string;
  expiresAt: string;
}

export interface CompanyProfile {
  id: string;
  name: string;
  mobileNumber: string;
  email: string | null;
  address: string | null;
  logoUrl: string | null;
  isActive: boolean;
}

export interface BranchDto {
  id: string;
  companyId: string;
  name: string;
  address: string | null;
  phoneNumber: string | null;
  email: string | null;
  isActive: boolean;
}

export interface CreateBranchRequest {
  name: string;
  address?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
}

export interface UpdateBranchRequest {
  name?: string | null;
  address?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
}

export interface SupportUserDto {
  id: string;
  companyId: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string | null;
  phoneNumber: string | null;
  role: string;
}

/** Backend enum: SupportAgent = 0, SupportManager = 1, CompanyAdmin = 2 */
export type SupportUserRoleBackend = 'SupportAgent' | 'SupportManager' | 'CompanyAdmin';

export interface CreateSupportUserRequest {
  username: string;
  password: string;
  firstName: string;
  lastName: string;
  role: SupportUserRoleBackend | number;
  email?: string | null;
  phoneNumber?: string | null;
}

export interface UpdateSupportUserRequest {
  firstName?: string | null;
  lastName?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
}

export interface CompanyWidgetDto {
  id: string;
  companyId: string;
  branchId: string;
  widgetId: string;
  isActive: boolean;
}

export interface GenerateWidgetResponse {
  widgetEntityId: string;
  widgetId: string;
  widgetToken: string;
}

export const companyApi = {
  register: (body: RegisterCompanyRequest) =>
    apiRequest<RegisterCompanyResponse>(`${PREFIX}/register`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  login: (body: CompanyLoginRequest) =>
    apiRequest<CompanyLoginResponse>(`${PREFIX}/login`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  getProfile: (companyToken: string) =>
    apiRequest<CompanyProfile>(`${PREFIX}/profile`, { method: 'GET' }, companyToken),

  getBranches: (companyToken: string) =>
    apiRequest<BranchDto[]>(`${PREFIX}/branches`, { method: 'GET' }, companyToken),

  createBranch: (companyToken: string, body: CreateBranchRequest) =>
    apiRequest<{ branchId: string }>(`${PREFIX}/branches`, {
      method: 'POST',
      body: JSON.stringify(body),
    }, companyToken),

  updateBranch: (companyToken: string, branchId: string, body: UpdateBranchRequest) =>
    apiRequest<null>(`${PREFIX}/branches/${branchId}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }, companyToken),

  deleteBranch: (companyToken: string, branchId: string) =>
    apiRequest<null>(`${PREFIX}/branches/${branchId}`, { method: 'DELETE' }, companyToken),

  getSupportUsers: (companyToken: string) =>
    apiRequest<SupportUserDto[]>(`${PREFIX}/support-users`, { method: 'GET' }, companyToken),

  createSupportUser: (companyToken: string, body: CreateSupportUserRequest) =>
    apiRequest<{ supportUserId: string }>(`${PREFIX}/support-users`, {
      method: 'POST',
      body: JSON.stringify(body),
    }, companyToken),

  updateSupportUser: (companyToken: string, supportUserId: string, body: UpdateSupportUserRequest) =>
    apiRequest<null>(`${PREFIX}/support-users/${supportUserId}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }, companyToken),

  deleteSupportUser: (companyToken: string, supportUserId: string) =>
    apiRequest<null>(`${PREFIX}/support-users/${supportUserId}`, { method: 'DELETE' }, companyToken),

  assignSupportUserToBranch: (companyToken: string, supportUserId: string, branchId: string) =>
    apiRequest<null>(`${PREFIX}/support-users/${supportUserId}/assign-branch`, {
      method: 'POST',
      body: JSON.stringify({ branchId }),
    }, companyToken),

  unassignSupportUserFromBranch: (companyToken: string, supportUserId: string, branchId: string) =>
    apiRequest<null>(`${PREFIX}/support-users/${supportUserId}/assign-branch/${branchId}`, {
      method: 'DELETE',
    }, companyToken),

  getSupportUserBranches: (companyToken: string, supportUserId: string) =>
    apiRequest<BranchDto[]>(`${PREFIX}/support-users/${supportUserId}/branches`, { method: 'GET' }, companyToken),

  getWidgets: (companyToken: string) =>
    apiRequest<CompanyWidgetDto[]>(`${PREFIX}/widget/list`, { method: 'GET' }, companyToken),

  generateWidget: (companyToken: string, branchId: string) =>
    apiRequest<GenerateWidgetResponse>(`${PREFIX}/widget/generate`, {
      method: 'POST',
      body: JSON.stringify({ branchId }),
    }, companyToken),

  deleteWidget: (companyToken: string, widgetId: string) =>
    apiRequest<null>(`${PREFIX}/widget/${widgetId}`, {
      method: 'DELETE',
    }, companyToken),
};

export function getWidgetScriptUrl(): string {
  const base = getBaseUrl().replace(/\/api.*$/, '');
  return `${base}/widget.js`;
}
