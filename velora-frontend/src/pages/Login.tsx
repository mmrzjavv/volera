import React, { useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuthStore } from '../store/useAuthStore';
import { useVersionStore } from '../store/useVersionStore';
import { Input } from '../components/ui/Input';
import { Button } from '../components/ui/Button';
import { RefreshCw } from 'lucide-react';

export function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [updating, setUpdating] = useState(false);
  const { login, isLoading, error } = useAuthStore();
  const { clearCacheAndReload } = useVersionStore();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string })?.from;

  const handleUpdateApp = async () => {
    setUpdating(true);
    try {
      await clearCacheAndReload();
    } finally {
      setUpdating(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await login({ username, password });
      navigate(from ?? '/', { replace: true });
    } catch {
      // Error handled in store
    }
  };

  return (
    <div className="volera-auth-shell">
      <div className="volera-panel volera-fade-up max-w-md w-full p-6 sm:p-8">
        <div className="flex flex-col items-center mb-8">
          <img src="/icon.svg" alt="" className="w-16 h-16 rounded-2xl mb-4 shadow-sm" />
          <p className="text-3xl font-bold tracking-tight text-[var(--volera-text)]">Volera</p>
          <p className="mt-1 text-sm text-[var(--volera-text-muted)]">Sign in to continue</p>
        </div>
        {error && (
          <div
            role="alert"
            className="bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800 p-3 rounded-[var(--volera-radius-sm)] mb-4 text-sm"
          >
            {error}
          </div>
        )}
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            autoComplete="username"
            required
          />
          <Input
            label="Password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
            required
          />
          <Button type="submit" className="w-full" isLoading={isLoading}>
            {isLoading ? 'Signing in…' : 'Sign in'}
          </Button>
        </form>
        <p className="mt-5 text-center text-sm text-[var(--volera-text-muted)]">
          Don&apos;t have an account?{' '}
          <Link to="/register" className="text-[var(--volera-accent)] hover:underline font-medium">
            Create one
          </Link>
        </p>
        <p className="mt-3 text-center">
          <button
            type="button"
            onClick={handleUpdateApp}
            disabled={updating}
            className="inline-flex items-center gap-1.5 text-sm text-[var(--volera-text-muted)] hover:text-[var(--volera-accent)] transition-colors disabled:opacity-50 min-h-[44px] px-2"
            title="Clear cache and reload to get the latest version"
          >
            <RefreshCw size={14} className={updating ? 'animate-spin' : ''} />
            {updating ? 'Updating…' : 'Update app'}
          </button>
        </p>
      </div>
    </div>
  );
}
