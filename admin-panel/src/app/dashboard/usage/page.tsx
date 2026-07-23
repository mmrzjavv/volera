'use client';

import { useAuthStore } from '@/store/useAuthStore';
import { useCompanyStore } from '@/store/useCompanyStore';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';

export default function UsagePage() {
  const auth = useAuthStore((s) => s.auth);
  const { branches, supportUsers, plan } = useCompanyStore();

  if (!auth) return null;

  const branchPct = plan && plan.maxBranches > 0 ? (branches.length / plan.maxBranches) * 100 : 0;
  const userPct = plan && plan.maxSupportUsers > 0 ? (supportUsers.length / plan.maxSupportUsers) * 100 : 0;

  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900">Plan usage</h1>
      <p className="mt-1 text-slate-600">
        {plan ? `Current usage for ${plan.name} plan.` : 'Usage from your account.'}
      </p>

      <div className="mt-8 grid gap-6 sm:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Branches</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex justify-between text-sm">
              <span className="text-slate-600">Used</span>
              <span className="font-medium text-slate-900">
                {branches.length} / {plan?.maxBranches ?? '—'}
              </span>
            </div>
            <div className="h-2 w-full overflow-hidden rounded-full bg-slate-200">
              <div
                className="h-full rounded-full bg-primary-600 transition-all"
                style={{ width: `${Math.min(branchPct, 100)}%` }}
              />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Support users</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex justify-between text-sm">
              <span className="text-slate-600">Used</span>
              <span className="font-medium text-slate-900">
                {supportUsers.length} / {plan?.maxSupportUsers ?? '—'}
              </span>
            </div>
            <div className="h-2 w-full overflow-hidden rounded-full bg-slate-200">
              <div
                className="h-full rounded-full bg-primary-600 transition-all"
                style={{ width: `${Math.min(userPct, 100)}%` }}
              />
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
