import type { Plan } from '@/types';

export const MOCK_PLANS: Plan[] = [
  {
    id: 'starter',
    name: 'Starter',
    maxBranches: 2,
    maxSupportUsers: 3,
    priceMonthly: 29,
    priceYearly: 290,
    features: ['2 branches', '3 support users', 'Email support'],
  },
  {
    id: 'growth',
    name: 'Growth',
    maxBranches: 10,
    maxSupportUsers: 15,
    priceMonthly: 79,
    priceYearly: 790,
    features: ['10 branches', '15 support users', 'Priority support', 'Widget branding'],
  },
  {
    id: 'enterprise',
    name: 'Enterprise',
    maxBranches: 100,
    maxSupportUsers: 200,
    priceMonthly: 199,
    priceYearly: 1990,
    features: ['Unlimited branches', '200 support users', '24/7 support', 'Custom integrations'],
  },
];
