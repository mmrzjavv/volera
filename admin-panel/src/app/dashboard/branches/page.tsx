'use client';

import { useState } from 'react';
import { useCompanyStore } from '@/store/useCompanyStore';
import { useAuthStore } from '@/store/useAuthStore';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Modal, ConfirmModal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import { Plus, Pencil, Trash2 } from 'lucide-react';
import type { Branch } from '@/types';

export default function BranchesPage() {
  const auth = useAuthStore((s) => s.auth);
  const token = auth?.token ?? null;
  const {
    branches,
    plan,
    loading,
    error,
    clearError,
    addBranch,
    updateBranch,
    deleteBranch,
  } = useCompanyStore();

  const [addOpen, setAddOpen] = useState(false);
  const [editBranch, setEditBranch] = useState<Branch | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [limitError, setLimitError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const [formName, setFormName] = useState('');
  const [formAddress, setFormAddress] = useState('');
  const [formPhone, setFormPhone] = useState('');
  const [formEmail, setFormEmail] = useState('');

  const resetForm = () => {
    setFormName('');
    setFormAddress('');
    setFormPhone('');
    setFormEmail('');
    setEditBranch(null);
    setLimitError('');
  };

  const openAdd = () => {
    if (plan && branches.length >= plan.maxBranches) {
      setLimitError(`Plan limit: max ${plan.maxBranches} branches. Upgrade to add more.`);
      return;
    }
    setLimitError('');
    resetForm();
    setAddOpen(true);
  };

  const handleAdd = async () => {
    if (!token) return;
    setSubmitting(true);
    const result = await addBranch(token, {
      name: formName,
      address: formAddress || null,
      phoneNumber: formPhone || null,
      email: formEmail || null,
    });
    setSubmitting(false);
    if (result.success) {
      setAddOpen(false);
      resetForm();
    } else {
      setLimitError(result.error ?? '');
    }
  };

  const openEdit = (b: Branch) => {
    setEditBranch(b);
    setFormName(b.name);
    setFormAddress(b.address ?? '');
    setFormPhone(b.phone ?? '');
    setFormEmail(b.email ?? '');
  };

  const handleEdit = async () => {
    if (!editBranch || !token) return;
    setSubmitting(true);
    const result = await updateBranch(token, editBranch.id, {
      name: formName,
      address: formAddress || null,
      phoneNumber: formPhone || null,
      email: formEmail || null,
    });
    setSubmitting(false);
    if (result.success) {
      setEditBranch(null);
      resetForm();
    }
  };

  const handleDelete = async (id: string) => {
    if (!token) return;
    setSubmitting(true);
    await deleteBranch(token, id);
    setSubmitting(false);
    setDeleteId(null);
  };

  if (!auth) return null;

  return (
    <div>
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold text-slate-900">Branches</h1>
        <Button onClick={openAdd} disabled={loading}>
          <Plus className="mr-2 h-4 w-4" />
          Add branch
        </Button>
      </div>

      {(error || limitError) && (
        <div
          className="mt-4 flex items-center justify-between rounded-lg bg-amber-50 p-4 text-sm text-amber-800"
        >
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
          <CardTitle>All branches</CardTitle>
        </CardHeader>
        <CardContent>
          {loading && branches.length === 0 ? (
            <p className="text-slate-500">Loading…</p>
          ) : branches.length === 0 ? (
            <p className="text-slate-500">No branches yet. Add one to get started.</p>
          ) : (
            <ul className="divide-y divide-slate-200">
              {branches.map((b) => (
                <li
                  key={b.id}
                  className="flex flex-wrap items-center justify-between gap-2 py-4 first:pt-0 last:pb-0"
                >
                  <div>
                    <p className="font-medium text-slate-900">{b.name}</p>
                    {b.address && (
                      <p className="text-sm text-slate-500">{b.address}</p>
                    )}
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => openEdit(b)}
                      disabled={submitting}
                    >
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setDeleteId(b.id)}
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
        onClose={() => {
          setAddOpen(false);
          resetForm();
        }}
        title="Add branch"
        footer={
          <>
            <Button variant="secondary" onClick={() => setAddOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleAdd} disabled={!formName.trim() || submitting}>
              {submitting ? 'Adding…' : 'Add'}
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          <Input label="Name" value={formName} onChange={(e) => setFormName(e.target.value)} required />
          <Input label="Address" value={formAddress} onChange={(e) => setFormAddress(e.target.value)} />
          <Input label="Phone" value={formPhone} onChange={(e) => setFormPhone(e.target.value)} />
          <Input label="Email" value={formEmail} onChange={(e) => setFormEmail(e.target.value)} />
        </div>
      </Modal>

      <Modal
        open={!!editBranch}
        onClose={() => {
          setEditBranch(null);
          resetForm();
        }}
        title="Edit branch"
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditBranch(null)}>
              Cancel
            </Button>
            <Button onClick={handleEdit} disabled={!formName.trim() || submitting}>
              {submitting ? 'Saving…' : 'Save'}
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          <Input label="Name" value={formName} onChange={(e) => setFormName(e.target.value)} />
          <Input label="Address" value={formAddress} onChange={(e) => setFormAddress(e.target.value)} />
          <Input label="Phone" value={formPhone} onChange={(e) => setFormPhone(e.target.value)} />
          <Input label="Email" value={formEmail} onChange={(e) => setFormEmail(e.target.value)} />
        </div>
      </Modal>

      <ConfirmModal
        open={!!deleteId}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteId && handleDelete(deleteId)}
        title="Delete branch"
        message="Are you sure? This cannot be undone."
        confirmLabel="Delete"
      />
    </div>
  );
}
