'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { MOCK_PLANS } from '@/data/plans';
import { useAuthStore } from '@/store/useAuthStore';
import { useCompanyStore } from '@/store/useCompanyStore';
import { companyApi } from '@/api/company';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';

type Step = 'plan' | 'company' | 'admin';

interface RegistrationFlowProps {
  initialPlanId?: string;
}

export function RegistrationFlow({ initialPlanId }: RegistrationFlowProps) {
  const router = useRouter();
  const setSession = useAuthStore((s) => s.setSession);
  const setPlan = useCompanyStore((s) => s.setPlan);

  const [step, setStep] = useState<Step>(initialPlanId ? 'company' : 'plan');
  const [planId, setPlanId] = useState(initialPlanId ?? '');

  // Sync step/plan when URL has ?plan= (e.g. after hydration or landing from pricing)
  useEffect(() => {
    if (!initialPlanId || initialPlanId === planId) return;
    const validPlan = MOCK_PLANS.some((p) => p.id === initialPlanId);
    if (!validPlan) return;
    setPlanId(initialPlanId);
    setPlan(initialPlanId);
    if (step === 'plan') setStep('company');
  }, [initialPlanId, step, planId, setPlan]);
  const [companyName, setCompanyName] = useState('');
  const [companyEmail, setCompanyEmail] = useState('');
  const [companyMobile, setCompanyMobile] = useState('');
  const [companyAddress, setCompanyAddress] = useState('');
  const [adminUsername, setAdminUsername] = useState('');
  const [adminFirstName, setAdminFirstName] = useState('');
  const [adminLastName, setAdminLastName] = useState('');
  const [adminEmail, setAdminEmail] = useState('');
  const [adminPassword, setAdminPassword] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handlePlanNext = (e?: React.MouseEvent) => {
    e?.preventDefault();
    e?.stopPropagation();
    if (!planId || planId === '') {
      setError('Please select a plan.');
      return;
    }
    try {
      setPlan(planId);
      setError('');
      setStep('company');
    } catch (err) {
      console.error('Error in handlePlanNext:', err);
      setError('An error occurred. Please try again.');
    }
  };

  const handleCompanyNext = (e?: React.MouseEvent, form?: HTMLFormElement) => {
    e?.preventDefault();
    e?.stopPropagation();
    if (!planId || !planId.trim()) {
      setError('Please select a plan.');
      return;
    }
    const formEl = form ?? (typeof document !== 'undefined' ? document.querySelector('form') : null);
    const name = (formEl?.elements.namedItem('companyName') as HTMLInputElement)?.value?.trim() ?? companyName.trim();
    const mobile = (formEl?.elements.namedItem('companyMobile') as HTMLInputElement)?.value?.trim() ?? companyMobile.trim();
    if (!name || !mobile) {
      setError('Company name and mobile are required.');
      return;
    }
    setCompanyName(name);
    setCompanyMobile(mobile);
    setCompanyEmail(((formEl?.elements.namedItem('companyEmail') as HTMLInputElement)?.value)?.trim() ?? companyEmail);
    setCompanyAddress(((formEl?.elements.namedItem('companyAddress') as HTMLInputElement)?.value)?.trim() ?? companyAddress);
    setError('');
    setStep('admin');
  };

  const handleRegisterOnly = async () => {
    setError('');
    setSubmitting(true);
    try {
      const res = await companyApi.register({
        name: companyName.trim(),
        mobileNumber: companyMobile.trim(),
        email: companyEmail.trim() || undefined,
        address: companyAddress.trim() || undefined,
      });
      if (res.success && res.data) {
        setSession(res.data.companyId, res.data.token, res.data.expiresAt);
        router.replace('/dashboard');
        return;
      }
      const errorMsg = Array.isArray(res.message) ? res.message.join(' ') : res.message?.[0] ?? 'Registration failed. Please try again.';
      setError(errorMsg);
    } catch (e) {
      const errorMsg = e instanceof Error ? e.message : 'Registration failed.';
      console.error('Registration error:', e);
      if (errorMsg.includes('fetch') || errorMsg.includes('Failed to fetch')) {
        setError('Cannot connect to backend. Make sure the API server is running at ' + (process.env.NEXT_PUBLIC_API_URL || '(set NEXT_PUBLIC_API_URL)'));
      } else {
        setError(errorMsg);
      }
    } finally {
      setSubmitting(false);
    }
  };

  const handleSubmit = async () => {
    setError('');
    setSubmitting(true);
    try {
      const registerRes = await companyApi.register({
        name: companyName.trim(),
        mobileNumber: companyMobile.trim(),
        email: companyEmail.trim() || undefined,
        address: companyAddress.trim() || undefined,
      });
      if (!registerRes.success || !registerRes.data) {
        setError(registerRes.message?.[0] ?? 'Registration failed.');
        setSubmitting(false);
        return;
      }
      const { companyId, token, expiresAt } = registerRes.data;
      setSession(companyId, token, expiresAt);

      if (adminUsername.trim() && adminPassword) {
        try {
          await companyApi.createSupportUser(token, {
            username: adminUsername.trim(),
            password: adminPassword,
            firstName: adminFirstName.trim() || 'Admin',
            lastName: adminLastName.trim() || 'User',
            role: 'CompanyAdmin',
            email: adminEmail.trim() || undefined,
          });
        } catch {
          // Company created; admin user creation failed (e.g. username taken). Still go to dashboard.
        }
      }
      router.replace('/dashboard');
    } catch (e) {
      const errorMsg = e instanceof Error ? e.message : 'Registration failed.';
      console.error('Registration error:', e);
      if (errorMsg.includes('fetch') || errorMsg.includes('Failed to fetch')) {
        setError('Cannot connect to backend. Make sure the API server is running at ' + (process.env.NEXT_PUBLIC_API_URL || '(set NEXT_PUBLIC_API_URL)'));
      } else {
        setError(errorMsg);
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>
          {step === 'plan' && '1. Choose plan'}
          {step === 'company' && '2. Company details'}
          {step === 'admin' && '3. Create account'}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {error && (
          <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{error}</div>
        )}

        {step === 'plan' && (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              const form = e.currentTarget;
              const planSelect = form.querySelector<HTMLSelectElement>('select[name="plan"]');
              const selectedPlanId = planSelect?.value?.trim() ?? '';
              if (!selectedPlanId) {
                setError('Please select a plan.');
                return;
              }
              setPlanId(selectedPlanId);
              setPlan(selectedPlanId);
              setError('');
              setStep('company');
            }}
          >
            <Select
              label="Plan"
              name="plan"
              options={MOCK_PLANS.map((p) => ({ value: p.id, label: `${p.name} — $${p.priceMonthly}/mo` }))}
              value={planId}
              onChange={(e) => {
                setPlanId(e.target.value);
                if (e.target.value) setError('');
              }}
            />
            <Button type="submit" className="w-full mt-4">
              Next
            </Button>
          </form>
        )}

        {step === 'company' && (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              handleCompanyNext(undefined, e.currentTarget);
            }}
          >
            {!initialPlanId && (
              <Select
                label="Plan"
                options={MOCK_PLANS.map((p) => ({ value: p.id, label: p.name }))}
                value={planId}
                onChange={(e) => setPlanId(e.target.value)}
              />
            )}
            <Input
              name="companyName"
              label="Company name"
              value={companyName}
              onChange={(e) => setCompanyName(e.target.value)}
              placeholder="Acme Corp"
            />
            <Input
              name="companyMobile"
              label="Mobile (required)"
              value={companyMobile}
              onChange={(e) => setCompanyMobile(e.target.value)}
              placeholder="+1234567890"
            />
            <Input
              name="companyEmail"
              label="Email"
              type="email"
              value={companyEmail}
              onChange={(e) => setCompanyEmail(e.target.value)}
              placeholder="contact@acme.com"
            />
            <Input
              name="companyAddress"
              label="Address"
              value={companyAddress}
              onChange={(e) => setCompanyAddress(e.target.value)}
              placeholder="Optional"
            />
            <div className="flex gap-2 mt-4">
              <Button
                variant="secondary"
                type="button"
                onClick={() => setStep('plan')}
              >
                Back
              </Button>
              <Button type="submit" className="flex-1">
                Next
              </Button>
            </div>
          </form>
        )}

        {step === 'admin' && (
          <>
            <p className="text-sm text-slate-600">
              Optionally create your first support user (Company Admin). You can skip and add users later from the dashboard.
            </p>
            <Input
              label="Username"
              value={adminUsername}
              onChange={(e) => setAdminUsername(e.target.value)}
              placeholder="admin.acme"
            />
            <Input
              label="First name"
              value={adminFirstName}
              onChange={(e) => setAdminFirstName(e.target.value)}
            />
            <Input
              label="Last name"
              value={adminLastName}
              onChange={(e) => setAdminLastName(e.target.value)}
            />
            <Input
              label="Email"
              type="email"
              value={adminEmail}
              onChange={(e) => setAdminEmail(e.target.value)}
            />
            <Input
              label="Password"
              type="password"
              value={adminPassword}
              onChange={(e) => setAdminPassword(e.target.value)}
              placeholder="••••••••"
            />
            <div className="flex gap-2 mt-4">
              <Button
                variant="secondary"
                type="button"
                onClick={() => setStep('company')}
                disabled={submitting}
              >
                Back
              </Button>
              <Button
                variant="secondary"
                type="button"
                onClick={handleRegisterOnly}
                disabled={submitting}
              >
                Skip & finish
              </Button>
              <Button
                type="button"
                onClick={handleSubmit}
                className="flex-1"
                disabled={submitting}
              >
                {submitting ? 'Creating…' : 'Create account'}
              </Button>
            </div>
          </>
        )}
        <p className="mt-4 text-center text-sm text-slate-600">
          Already have an account?{' '}
          <Link href="/login" className="font-medium text-primary-600 hover:text-primary-700">
            Log in
          </Link>
        </p>
      </CardContent>
    </Card>
  );
}
