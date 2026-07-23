import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuthStore } from '../store/useAuthStore';
import { Input } from '../components/ui/Input';
import { Button } from '../components/ui/Button';

export function Register() {
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    username: '',
    phoneNumber: '',
    password: '',
  });
  const { register, isLoading, error } = useAuthStore();
  const navigate = useNavigate();

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await register(formData);
      navigate('/login');
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
          <p className="mt-1 text-sm text-[var(--volera-text-muted)]">Create your account</p>
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
          <div className="grid grid-cols-2 gap-3 sm:gap-4">
            <Input label="First Name" name="firstName" value={formData.firstName} onChange={handleChange} autoComplete="given-name" required />
            <Input label="Last Name" name="lastName" value={formData.lastName} onChange={handleChange} autoComplete="family-name" required />
          </div>
          <Input label="Username" name="username" value={formData.username} onChange={handleChange} autoComplete="username" required />
          <Input label="Phone Number" name="phoneNumber" value={formData.phoneNumber} onChange={handleChange} autoComplete="tel" required />
          <Input label="Password" type="password" name="password" value={formData.password} onChange={handleChange} autoComplete="new-password" required />
          <Button type="submit" className="w-full" isLoading={isLoading}>
            {isLoading ? 'Creating…' : 'Create account'}
          </Button>
        </form>
        <p className="mt-5 text-center text-sm text-[var(--volera-text-muted)]">
          Already have an account?{' '}
          <Link to="/login" className="text-[var(--volera-accent)] hover:underline font-medium">
            Sign in
          </Link>
        </p>
      </div>
    </div>
  );
}
