'use client';

import { usePathname, useRouter } from 'next/navigation';
import Link from 'next/link';
import { useSupportAuthStore } from '@/store/useSupportAuthStore';
import { useEffect, useState } from 'react';
import { MessageSquare, LogOut, Menu, X } from 'lucide-react';
import { cn } from '@/lib/cn';

export default function SupportLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const auth = useSupportAuthStore((s) => s.auth);
  const logout = useSupportAuthStore((s) => s.logout);
  const rehydrate = useSupportAuthStore((s) => s.rehydrate);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [hydrated, setHydrated] = useState(false);

  const isLoginPage = pathname === '/support/login';

  useEffect(() => {
    rehydrate();
    setHydrated(true);
  }, [rehydrate]);

  useEffect(() => {
    if (!hydrated || isLoginPage) return;
    if (auth === null) {
      router.replace('/support/login');
    }
  }, [hydrated, isLoginPage, auth, router]);

  const handleLogout = () => {
    logout();
    router.replace('/support/login');
  };

  if (isLoginPage) {
    return <>{children}</>;
  }

  if (!hydrated || auth === null) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <div className="text-slate-500">
          {!hydrated ? 'Loading...' : 'Redirecting...'}
        </div>
      </div>
    );
  }

  const displayName =
    auth.supportUser.firstName || auth.supportUser.lastName
      ? `${auth.supportUser.firstName} ${auth.supportUser.lastName}`.trim()
      : auth.supportUser.username;

  return (
    <div className="min-h-screen bg-slate-50">
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-40 w-64 border-r border-slate-200 bg-white transition-transform lg:translate-x-0',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        <div className="flex h-14 items-center justify-between border-b border-slate-200 px-4 lg:justify-center">
          <Link href="/support" className="font-semibold text-primary-600">
            Support
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
          <Link
            href="/support"
            onClick={() => setSidebarOpen(false)}
            className={cn(
              'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition',
              pathname === '/support'
                ? 'bg-primary-50 text-primary-700'
                : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
            )}
          >
            <MessageSquare className="h-5 w-5 shrink-0" />
            Inbox
          </Link>
          <Link
            href="/support/conversations"
            onClick={() => setSidebarOpen(false)}
            className={cn(
              'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition',
              pathname === '/support/conversations'
                ? 'bg-primary-50 text-primary-700'
                : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
            )}
          >
            <MessageSquare className="h-5 w-5 shrink-0" />
            Conversations
          </Link>
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
          <span className="text-sm text-slate-600">{displayName}</span>
          <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-700">
            {auth.supportUser.role}
          </span>
          <button
            type="button"
            onClick={handleLogout}
            className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-700"
            title="Log out"
          >
            <LogOut className="h-5 w-5" />
          </button>
        </header>

        <main className={pathname === '/support/conversations' ? 'h-[calc(100vh-3.5rem)] overflow-hidden' : 'p-4 lg:p-6'}>{children}</main>
      </div>
    </div>
  );
}
