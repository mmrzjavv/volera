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
    } catch (err) {
      // Error handled in store
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-[var(--volera-bg)] p-4">
      <div className="max-w-md w-full bg-[var(--volera-surface)] p-6 sm:p-8 rounded-[var(--volera-radius-md)] shadow-lg border border-[var(--volera-border)]">
        <div className="flex flex-col items-center mb-6">
          <img src="/icon.svg" alt="Volera" className="w-14 h-14 rounded-2xl mb-3" />
          <h2 className="text-2xl font-bold text-center text-[var(--volera-text)]">Login to Volera</h2>
        </div>
        {error && (
          <div className="bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800 p-3 rounded-lg mb-4 text-sm">
            {error}
          </div>
        )}
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required
          />
          <Input
            label="Password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
          <Button type="submit" className="w-full" disabled={isLoading}>
            {isLoading ? 'Logging in...' : 'Login'}
          </Button>
        </form>
        <p className="mt-4 text-center text-sm text-[var(--volera-text-muted)]">
          Don't have an account?{' '}
          <Link to="/register" className="text-[var(--volera-accent)] hover:underline font-medium">
            Register
          </Link>
        </p>
        <p className="mt-3 text-center">
          <button
            type="button"
            onClick={handleUpdateApp}
            disabled={updating}
            className="inline-flex items-center gap-1.5 text-sm text-[var(--volera-text-muted)] hover:text-[var(--volera-accent)] transition-colors disabled:opacity-50"
            title="Clear cache and reload to get the latest version"
          >
            <RefreshCw size={14} />
            {updating ? 'Updating…' : 'Update app'}
          </button>
        </p>
      </div>
    </div>
  );
}
