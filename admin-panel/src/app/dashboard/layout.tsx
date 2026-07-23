'use client';

import { usePathname, useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuthStore } from '@/store/useAuthStore';
import { useCompanyStore } from '@/store/useCompanyStore';
import { useEffect, useState } from 'react';
import { Role } from '@/types';
import {
  LayoutDashboard,
  Building2,
  Users,
  BarChart3,
  Code2,
  Bot,
  LogOut,
  Menu,
  X,
} from 'lucide-react';
import { ROLE_LABEL, ROLE_BADGE_COLOR } from '@/lib/roles';
import {
  canManageBranches,
  canManageSupportUsers,
  canViewPlanUsage,
  canManageWidget,
} from '@/lib/roles';
import { cn } from '@/lib/cn';

const NAV = [
  { href: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/dashboard/branches', label: 'Branches', icon: Building2, guard: canManageBranches },
  { href: '/dashboard/users', label: 'Support users', icon: Users, guard: canManageSupportUsers },
  { href: '/dashboard/usage', label: 'Plan usage', icon: BarChart3, guard: canViewPlanUsage },
  {
    href: '/dashboard/ai-widget',
    label: 'محتوای یادگیری هوش مصنوعی',
    icon: Bot,
    guard: canManageWidget,
  },
  {
    href: '/dashboard/widget',
    label: 'ویجت چت‌بات هوش مصنوعی',
    icon: Code2,
    guard: canManageWidget,
  },
];

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const auth = useAuthStore((s) => s.auth);
  const logout = useAuthStore((s) => s.logout);
  const setRole = useAuthStore((s) => s.setRole);
  const loadProfile = useAuthStore((s) => s.loadProfile);
  const rehydrate = useAuthStore((s) => s.rehydrate);
  const loadBranches = useCompanyStore((s) => s.loadBranches);
  const loadSupportUsers = useCompanyStore((s) => s.loadSupportUsers);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    rehydrate();
    setHydrated(true);
  }, [rehydrate]);

  useEffect(() => {
    if (!hydrated) return;
    if (auth === null) {
      router.replace('/');
    }
  }, [hydrated, auth, router]);

  useEffect(() => {
    if (!auth?.token) return;
    if (!auth.profile) {
      loadProfile();
    } else {
      loadBranches(auth.token);
      loadSupportUsers(auth.token);
    }
  }, [auth?.token, auth?.profile, loadProfile, loadBranches, loadSupportUsers]);

  const handleRoleChange = (newRole: Role) => {
    if (auth) setRole(newRole);
  };

  if (!hydrated || auth === null) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-slate-500">{!hydrated ? 'Loading...' : 'Redirecting...'}</div>
      </div>
    );
  }

  const role = auth.role;
  const visibleNav = NAV.filter((n) => !n.guard || n.guard(role));
  const companyName = auth.profile?.name ?? auth.companyId;

  return (
    <div className="min-h-screen bg-slate-50">
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-40 w-64 border-r border-slate-200 bg-white transition-transform lg:translate-x-0',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        <div className="flex h-14 items-center justify-between border-b border-slate-200 px-4 lg:justify-center">
          <Link href="/dashboard" className="font-semibold text-primary-600">
            Widget Admin
          </Link>
          <button
            type="button"
            onClick={() => setSidebarOpen(false)}
            className="lg:hidden"
            aria-label="Close menu"
          >
            <X className="h-5 w-5" />
          </button>
        </div>
        <nav className="space-y-0.5 p-2">
          {visibleNav.map((item) => {
            const isActive = pathname === item.href;
            return (
              <Link
                key={item.href}
                href={item.href}
                onClick={() => setSidebarOpen(false)}
                className={cn(
                  'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium',
                  isActive
                    ? 'bg-primary-50 text-primary-700'
                    : 'text-slate-700 hover:bg-slate-100'
                )}
              >
                <item.icon className="h-5 w-5 shrink-0" />
                {item.label}
              </Link>
            );
          })}
        </nav>
      </aside>

      <div className="lg:pl-64">
        <header className="sticky top-0 z-30 flex h-14 items-center gap-4 border-b border-slate-200 bg-white px-4">
          <button
            type="button"
            onClick={() => setSidebarOpen(true)}
            className="lg:hidden"
            aria-label="Open menu"
          >
            <Menu className="h-5 w-5" />
          </button>
          <div className="flex-1" />
          <span className="text-sm text-slate-600">{companyName}</span>
          <span
            className={cn(
              'rounded-full px-2.5 py-0.5 text-xs font-medium',
              ROLE_BADGE_COLOR[role]
            )}
          >
            {ROLE_LABEL[role]}
          </span>
          <select
            value={role}
            onChange={(e) => handleRoleChange(e.target.value as Role)}
            className="rounded border border-slate-300 bg-white py-1 pl-2 pr-6 text-xs text-slate-600 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
            title="Simulate role (demo)"
          >
            {(['SuperAdmin', 'CompanyAdmin', 'BranchManager', 'SupportUser'] as const).map((r) => (
              <option key={r} value={r}>
                {ROLE_LABEL[r]}
              </option>
            ))}
          </select>
          <button
            type="button"
            onClick={() => {
              logout();
              useCompanyStore.getState().reset();
              router.push('/');
            }}
            className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-700"
            title="Log out"
          >
            <LogOut className="h-5 w-5" />
          </button>
        </header>

        <main className="p-4 lg:p-6">{children}</main>
      </div>
    </div>
  );
}
