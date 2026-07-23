import type { Branch, SupportUser, Company, CompanyAdmin, Plan } from '@/types';
import { MOCK_PLANS } from './plans';

export const createInitialBranches = (): Branch[] => [
  {
    id: 'br-1',
    name: 'Head Office',
    address: '123 Main St',
    phone: '+1234567890',
    email: 'head@company.com',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'br-2',
    name: 'Downtown Branch',
    address: '456 Oak Ave',
    phone: null,
    email: 'downtown@company.com',
    createdAt: new Date().toISOString(),
  },
];

export const createInitialSupportUsers = (companyId: string): SupportUser[] => [
  {
    id: 'su-1',
    username: 'support.jane',
    firstName: 'Jane',
    lastName: 'Doe',
    email: 'jane@company.com',
    role: 'SupportUser',
    branchIds: ['br-1'],
    isActive: true,
  },
  {
    id: 'su-2',
    username: 'support.john',
    firstName: 'John',
    lastName: 'Smith',
    email: null,
    role: 'BranchManager',
    branchIds: ['br-1', 'br-2'],
    isActive: true,
  },
];

export const createMockCompany = (planId: string): Company => ({
  id: 'co-1',
  name: 'Acme Corp',
  email: 'contact@acme.com',
  mobile: '+1987654321',
  planId,
  createdAt: new Date().toISOString(),
});

export const createMockAdmin = (companyId: string, role: 'SuperAdmin' | 'CompanyAdmin' = 'CompanyAdmin'): CompanyAdmin => ({
  id: 'admin-1',
  username: 'admin.acme',
  firstName: 'Alex',
  lastName: 'Admin',
  email: 'admin@acme.com',
  role,
  companyId,
});

export function getPlanById(id: string): Plan | undefined {
  return MOCK_PLANS.find((p) => p.id === id);
}
