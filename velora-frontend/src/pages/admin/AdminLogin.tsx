import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../../services/api';
import { useAuthStore } from '../../store/useAuthStore';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';

const ADMIN_ROLES = ['Admin', 'Moderator', 'SuperAdmin'];

export const AdminLogin: React.FC = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const { setUser } = useAuthStore();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const response = await authService.login({ username, password });
      if (response?.user) {
        setUser(response.user);
        const role = (response.user as { role?: string }).role ?? response.user.role;
        if (role && ADMIN_ROLES.includes(role)) {
          navigate('/admin');
        } else {
          authService.logout();
          setError('You do not have admin access.');
        }
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(Array.isArray(msg) ? msg.join(' ') : msg ?? 'Login failed.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center p-4 sm:p-6">
      <div className="w-full max-w-md bg-slate-900 rounded-xl border border-slate-800 p-4 sm:p-6 md:p-8">
        <h1 className="text-xl sm:text-2xl font-bold text-white mb-2">Volera Admin</h1>
        <p className="text-slate-400 text-sm mb-6">Sign in with your admin account.</p>
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Username"
            type="text"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            className="bg-slate-800 border-slate-700 text-white focus:ring-teal-500"
            required
          />
          <Input
            label="Password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="bg-slate-800 border-slate-700 text-white focus:ring-teal-500"
            required
          />
          {error && <p className="text-red-400 text-sm">{error}</p>}
          <Button type="submit" isLoading={loading} className="w-full">
            {loading ? 'Signing in...' : 'Sign in'}
          </Button>
        </form>
        <p className="mt-4 text-center text-slate-500 text-sm">
          <a href="/login" className="text-teal-400 hover:underline">Back to main app</a>
        </p>
      </div>
    </div>
  );
};
