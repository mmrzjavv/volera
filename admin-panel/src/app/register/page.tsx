'use client';

import { useSearchParams, useRouter } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { useAuthStore } from '@/store/useAuthStore';
import { RegistrationFlow } from '@/components/registration/RegistrationFlow';

function RegisterContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const planFromQuery = searchParams.get('plan');
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const rehydrate = useAuthStore((s) => s.rehydrate);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    rehydrate();
    setHydrated(true);
  }, [rehydrate]);

  useEffect(() => {
    if (hydrated && isAuthenticated) router.replace('/dashboard');
  }, [hydrated, isAuthenticated, router]);

  if (!hydrated || isAuthenticated) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <div className="text-slate-500">{!hydrated ? 'Loading...' : 'Redirecting...'}</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex h-14 max-w-2xl items-center px-4">
          <a href="/" className="text-lg font-semibold text-primary-600">
            Widget Admin
          </a>
        </div>
      </header>
      <main className="mx-auto max-w-2xl px-4 py-10">
        <RegistrationFlow initialPlanId={planFromQuery ?? undefined} />
      </main>
    </div>
  );
}

export default function RegisterPage() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-screen items-center justify-center">
          <div className="text-slate-500">Loading...</div>
        </div>
      }
    >
      <RegisterContent />
    </Suspense>
  );
}
