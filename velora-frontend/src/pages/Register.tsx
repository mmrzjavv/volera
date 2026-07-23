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
    } catch (err) {
      // Error handled in store
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-[var(--volera-bg)] p-4">
      <div className="max-w-md w-full bg-[var(--volera-surface)] p-6 sm:p-8 rounded-[var(--volera-radius-md)] shadow-lg border border-[var(--volera-border)]">
        <div className="flex flex-col items-center mb-6">
          <img src="/icon.svg" alt="Volera" className="w-14 h-14 rounded-2xl mb-3" />
          <h2 className="text-2xl font-bold text-center text-[var(--volera-text)]">Create Account</h2>
        </div>
        {error && (
          <div className="bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800 p-3 rounded-lg mb-4 text-sm">
            {error}
          </div>
        )}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <Input label="First Name" name="firstName" value={formData.firstName} onChange={handleChange} required />
            <Input label="Last Name" name="lastName" value={formData.lastName} onChange={handleChange} required />
          </div>
          <Input label="Username" name="username" value={formData.username} onChange={handleChange} required />
          <Input label="Phone Number" name="phoneNumber" value={formData.phoneNumber} onChange={handleChange} required />
          <Input label="Password" type="password" name="password" value={formData.password} onChange={handleChange} required />
          <Button type="submit" className="w-full" disabled={isLoading}>
            {isLoading ? 'Registering...' : 'Register'}
          </Button>
        </form>
        <p className="mt-4 text-center text-sm text-[var(--volera-text-muted)]">
          Already have an account?{' '}
          <Link to="/login" className="text-[var(--volera-accent)] hover:underline font-medium">
            Login
          </Link>
        </p>
      </div>
    </div>
  );
}
