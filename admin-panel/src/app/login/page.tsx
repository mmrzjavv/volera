'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useAuthStore } from '@/store/useAuthStore';
import { companyApi } from '@/api/company';
import { Button } from '@/components/ui/Button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';

export default function LoginPage() {
  const router = useRouter();
  const setSession = useAuthStore((s) => s.setSession);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const rehydrate = useAuthStore((s) => s.rehydrate);
  const [hydrated, setHydrated] = useState(false);
  const [mobileNumber, setMobileNumber] = useState('');
  const [otp, setOtp] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    rehydrate();
    setHydrated(true);
  }, [rehydrate]);

  useEffect(() => {
    if (hydrated && isAuthenticated) router.replace('/dashboard');
  }, [hydrated, isAuthenticated, router]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!mobileNumber.trim() || !otp.trim()) {
      setError('Mobile number and OTP are required.');
      return;
    }
    setError('');
    setSubmitting(true);
    try {
      const res = await companyApi.login({
        mobileNumber: mobileNumber.trim(),
        token: otp.trim(),
      });
      if (res.success && res.data) {
        setSession(res.data.companyId, res.data.token, res.data.expiresAt);
        router.replace('/dashboard');
        return;
      }
      setError(Array.isArray(res.message) ? res.message[0] : res.message?.[0] ?? 'Login failed.');
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Login failed.';
      setError(msg.includes('fetch') || msg.includes('Failed to fetch')
        ? 'Cannot connect to backend. Is the API server running?'
        : msg);
    } finally {
      setSubmitting(false);
    }
  };

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
        <div className="mx-auto flex h-14 max-w-2xl items-center justify-between px-4">
          <Link href="/" className="text-lg font-semibold text-primary-600">
            Widget Admin
          </Link>
          <div className="flex items-center gap-3">
            <Link
              href="/support/login"
              className="text-sm font-medium text-slate-600 hover:text-slate-900"
            >
              Support login
            </Link>
            <Link
              href="/register"
              className="text-sm font-medium text-slate-600 hover:text-slate-900"
            >
              Create account
            </Link>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-md px-4 py-12">
        <Card>
          <CardHeader>
            <CardTitle>Log in</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="mb-4 text-sm text-slate-500">
              After registering, log in with your company mobile number and the OTP sent to you. For demo, use OTP <strong>1234</strong>.
            </p>
            <form onSubmit={handleSubmit} className="space-y-4">
              {error && (
                <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{error}</div>
              )}
              <Input
                label="Mobile number"
                value={mobileNumber}
                onChange={(e) => setMobileNumber(e.target.value)}
                placeholder="+1234567890"
                required
              />
              <Input
                label="OTP"
                type="text"
                inputMode="numeric"
                autoComplete="one-time-code"
                value={otp}
                onChange={(e) => setOtp(e.target.value)}
                placeholder="1234 (demo)"
                required
              />
              <Button type="submit" className="w-full" disabled={submitting}>
                {submitting ? 'Signing in…' : 'Log in'}
              </Button>
            </form>
          </CardContent>
        </Card>
      </main>
    </div>
  );
}
