'use client';

import Link from 'next/link';
import { useAuthStore } from '@/store/useAuthStore';
import { useCompanyStore } from '@/store/useCompanyStore';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Building2, Users, BarChart3, Code2 } from 'lucide-react';
import { canManageBranches, canManageSupportUsers, canViewPlanUsage, canManageWidget } from '@/lib/roles';

const TILES = [
  { href: '/dashboard/branches', label: 'Branches', icon: Building2, guard: canManageBranches, key: 'branches' },
  { href: '/dashboard/users', label: 'Support users', icon: Users, guard: canManageSupportUsers, key: 'users' },
  { href: '/dashboard/usage', label: 'Plan usage', icon: BarChart3, guard: canViewPlanUsage, key: 'usage' },
  { href: '/dashboard/widget', label: 'Widget', icon: Code2, guard: canManageWidget, key: 'widget' },
];

export default function DashboardPage() {
  const auth = useAuthStore((s) => s.auth);
  const branches = useCompanyStore((s) => s.branches);
  const supportUsers = useCompanyStore((s) => s.supportUsers);
  const plan = useCompanyStore((s) => s.plan);

  if (!auth) return null;

  const role = auth.role;
  const visibleTiles = TILES.filter((t) => !t.guard || t.guard(role));
  const companyName = auth.profile?.name ?? auth.companyId;

  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900">Dashboard</h1>
      <p className="mt-1 text-slate-600">
        Welcome back. Manage your branches and support team.
      </p>

      <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {visibleTiles.map((tile) => (
          <Link key={tile.key} href={tile.href}>
            <Card className="h-full transition-shadow hover:shadow-md">
              <CardContent className="flex items-center gap-4 p-6">
                <div className="rounded-lg bg-primary-100 p-3">
                  <tile.icon className="h-6 w-6 text-primary-600" />
                </div>
                <div>
                  <p className="font-medium text-slate-900">{tile.label}</p>
                  {tile.key === 'branches' && (
                    <p className="text-sm text-slate-500">
                      {branches.length} branch{branches.length !== 1 ? 'es' : ''}
                    </p>
                  )}
                  {tile.key === 'users' && (
                    <p className="text-sm text-slate-500">
                      {supportUsers.length} user{supportUsers.length !== 1 ? 's' : ''}
                    </p>
                  )}
                </div>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>

      <Card className="mt-8">
        <CardHeader>
          <CardTitle>Quick info</CardTitle>
        </CardHeader>
        <CardContent className="text-sm text-slate-600">
          {plan && <p>Plan: {plan.name}</p>}
          <p>Company: {companyName}</p>
        </CardContent>
      </Card>
    </div>
  );
}
