'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useSupportAuthStore } from '@/store/useSupportAuthStore';
import { supportApi } from '@/api/support';
import { Button } from '@/components/ui/Button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';

export default function SupportLoginPage() {
  const router = useRouter();
  const setSession = useSupportAuthStore((s) => s.setSession);
  const isAuthenticated = useSupportAuthStore((s) => s.isAuthenticated);
  const rehydrate = useSupportAuthStore((s) => s.rehydrate);
  const [hydrated, setHydrated] = useState(false);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    rehydrate();
    setHydrated(true);
  }, [rehydrate]);

  useEffect(() => {
    if (hydrated && isAuthenticated) router.replace('/support');
  }, [hydrated, isAuthenticated, router]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!username.trim() || !password) {
      setError('Username and password are required.');
      return;
    }
    setError('');
    setSubmitting(true);
    try {
      const res = await supportApi.login({
        username: username.trim(),
        password,
      });
      if (res.success && res.data && res.data.supportUser) {
        setSession({
          token: res.data.token,
          expiresAt: res.data.expiresAt,
          supportUser: res.data.supportUser,
        });
        router.replace('/support');
        return;
      }
      setError(
        Array.isArray(res.message) ? res.message[0] : res.message?.[0] ?? 'Login failed.'
      );
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Login failed.';
      setError(
        msg.includes('fetch') || msg.includes('Failed to fetch')
          ? 'Cannot connect to backend. Is the API server running?'
          : msg
      );
    } finally {
      setSubmitting(false);
    }
  };

  if (!hydrated || isAuthenticated) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <div className="text-slate-500">
          {!hydrated ? 'Loading...' : 'Redirecting...'}
        </div>
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
          <Link
            href="/login"
            className="text-sm font-medium text-slate-600 hover:text-slate-900"
          >
            Company login
          </Link>
        </div>
      </header>
      <main className="mx-auto max-w-md px-4 py-12">
        <Card>
          <CardHeader>
            <CardTitle>Support agent login</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="mb-4 text-sm text-slate-500">
              Log in with your support user account to view client messages from
              the widget.
            </p>
            <form onSubmit={handleSubmit} className="space-y-4">
              {error && (
                <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
                  {error}
                </div>
              )}
              <Input
                label="Username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="Your support username"
                required
              />
              <Input
                label="Password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
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
