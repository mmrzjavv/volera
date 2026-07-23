'use client';

import { useState, useEffect } from 'react';
import { useCompanyStore } from '@/store/useCompanyStore';
import { useAuthStore } from '@/store/useAuthStore';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Modal, ConfirmModal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Plus, UserPlus, Trash2 } from 'lucide-react';
import type { Role } from '@/types';
import { ROLE_LABEL } from '@/lib/roles';
import type { SupportUserRoleBackend } from '@/api/company';

const ROLE_OPTIONS: { value: Role; label: string }[] = [
  { value: 'SupportUser', label: ROLE_LABEL.SupportUser },
  { value: 'BranchManager', label: ROLE_LABEL.BranchManager },
  { value: 'CompanyAdmin', label: ROLE_LABEL.CompanyAdmin },
];

function roleToBackend(r: Role): SupportUserRoleBackend {
  switch (r) {
    case 'CompanyAdmin': return 'CompanyAdmin';
    case 'BranchManager': return 'SupportManager';
    default: return 'SupportAgent';
  }
}

export default function SupportUsersPage() {
  const auth = useAuthStore((s) => s.auth);
  const token = auth?.token ?? null;
  const {
    branches,
    supportUsers,
    plan,
    loading,
    error,
    clearError,
    addSupportUser,
    deleteSupportUser,
    assignUserToBranch,
    loadSupportUserBranches,
  } = useCompanyStore();

  const [addOpen, setAddOpen] = useState(false);
  const [assignUserId, setAssignUserId] = useState<string | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [limitError, setLimitError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<Role>('SupportUser');
  const [assignBranchId, setAssignBranchId] = useState('');

  useEffect(() => {
    if (assignUserId && token) {
      loadSupportUserBranches(token, assignUserId);
    }
  }, [assignUserId, token, loadSupportUserBranches]);

  const openAdd = () => {
    if (plan && supportUsers.length >= plan.maxSupportUsers) {
      setLimitError(`Plan limit: max ${plan.maxSupportUsers} support users. Upgrade to add more.`);
      return;
    }
    setLimitError('');
    setUsername('');
    setPassword('');
    setFirstName('');
    setLastName('');
    setEmail('');
    setRole('SupportUser');
    setAddOpen(true);
  };

  const handleAdd = async () => {
    if (!token || !username.trim() || !password || !firstName.trim() || !lastName.trim()) return;
    setSubmitting(true);
    const result = await addSupportUser(token, {
      username: username.trim(),
      password,
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      role: roleToBackend(role),
      email: email.trim() || undefined,
    });
    setSubmitting(false);
    if (result.success) {
      setAddOpen(false);
    } else {
      setLimitError(result.error ?? '');
    }
  };

  const handleAssignBranch = async () => {
    if (!assignUserId || !assignBranchId || !token) return;
    setSubmitting(true);
    await assignUserToBranch(token, assignUserId, assignBranchId);
    setSubmitting(false);
    setAssignUserId(null);
    setAssignBranchId('');
  };

  const handleDelete = async (id: string) => {
    if (!token) return;
    setSubmitting(true);
    await deleteSupportUser(token, id);
    setSubmitting(false);
    setDeleteId(null);
  };

  if (!auth) return null;

  const branchOptions = branches.map((b) => ({ value: b.id, label: b.name }));

  return (
    <div>
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold text-slate-900">Support users</h1>
        <Button onClick={openAdd} disabled={loading}>
          <Plus className="mr-2 h-4 w-4" />
          Add user
        </Button>
      </div>

      {(error || limitError) && (
        <div className="mt-4 flex items-center justify-between rounded-lg bg-amber-50 p-4 text-sm text-amber-800">
          <span>{error || limitError}</span>
          {error && (
            <button type="button" onClick={clearError} className="underline">
              Dismiss
            </button>
          )}
        </div>
      )}

      <Card className="mt-6">
        <CardHeader>
          <CardTitle>All support users</CardTitle>
        </CardHeader>
        <CardContent>
          {loading && supportUsers.length === 0 ? (
            <p className="text-slate-500">Loading…</p>
          ) : supportUsers.length === 0 ? (
            <p className="text-slate-500">No support users yet.</p>
          ) : (
            <ul className="divide-y divide-slate-200">
              {supportUsers.map((u) => (
                <li
                  key={u.id}
                  className="flex flex-wrap items-center justify-between gap-2 py-4 first:pt-0 last:pb-0"
                >
                  <div>
                    <p className="font-medium text-slate-900">
                      {u.firstName} {u.lastName}
                    </p>
                    <p className="text-sm text-slate-500">{u.username}</p>
                    <p className="text-xs text-slate-400">
                      {ROLE_LABEL[u.role]} · Branches:{' '}
                      {u.branchIds.length === 0
                        ? 'None'
                        : u.branchIds
                            .map((id) => branches.find((b) => b.id === id)?.name ?? id)
                            .join(', ')}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => {
                        setAssignUserId(u.id);
                        setAssignBranchId('');
                      }}
                      disabled={submitting}
                    >
                      <UserPlus className="mr-1 h-4 w-4" />
                      Assign branch
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setDeleteId(u.id)}
                      disabled={submitting}
                      className="text-red-600 hover:bg-red-50 hover:text-red-700"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Modal
        open={addOpen}
        onClose={() => setAddOpen(false)}
        title="Add support user"
        footer={
          <>
            <Button variant="secondary" onClick={() => setAddOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleAdd}
              disabled={
                !username.trim() ||
                !password ||
                !firstName.trim() ||
                !lastName.trim() ||
                submitting
              }
            >
              {submitting ? 'Adding…' : 'Add'}
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          <Input
            label="Username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
          />
          <Input
            label="Password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
          />
          <Input
            label="First name"
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
          />
          <Input
            label="Last name"
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
          />
          <Input
            label="Email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
          <Select
            label="Role"
            options={ROLE_OPTIONS}
            value={role}
            onChange={(e) => setRole(e.target.value as Role)}
          />
        </div>
      </Modal>

      <Modal
        open={!!assignUserId}
        onClose={() => {
          setAssignUserId(null);
          setAssignBranchId('');
        }}
        title="Assign to branch"
        footer={
          <>
            <Button variant="secondary" onClick={() => setAssignUserId(null)}>
              Cancel
            </Button>
            <Button
              onClick={handleAssignBranch}
              disabled={!assignBranchId || submitting}
            >
              {submitting ? 'Assigning…' : 'Assign'}
            </Button>
          </>
        }
      >
        <Select
          label="Branch"
          options={branchOptions}
          value={assignBranchId}
          onChange={(e) => setAssignBranchId(e.target.value)}
        />
      </Modal>

      <ConfirmModal
        open={!!deleteId}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteId && handleDelete(deleteId)}
        title="Delete support user"
        message="Are you sure? This cannot be undone."
        confirmLabel="Delete"
      />
    </div>
  );
}
