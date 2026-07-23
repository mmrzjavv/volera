import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { createPortal } from 'react-dom';
import { adminApi, type AdminUserDetailDto } from '../../services/adminApi';
import { ArrowLeft, Ban, Clock, CheckCircle, UserCog } from 'lucide-react';
import { useConfirmationStore } from '../../store/useConfirmationStore';
import { useToastStore } from '../../store/useToastStore';

const ROLES = ['User', 'Moderator', 'Admin', 'SuperAdmin'] as const;

export const AdminUserDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const addToast = useToastStore((s) => s.addToast);
  const [user, setUser] = useState<AdminUserDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [setRoleModalOpen, setSetRoleModalOpen] = useState(false);
  const [selectedRole, setSelectedRole] = useState<string>('User');
  const confirm = (title: string, message: string): Promise<boolean> =>
    new Promise((resolve) => {
      useConfirmationStore.getState().openDialog({ title, message, variant: 'warning', onConfirm: () => resolve(true), onCancel: () => resolve(false) });
    });

  useEffect(() => {
    if (id) adminApi.getUserDetail(id).then(setUser).finally(() => setLoading(false));
  }, [id]);

  const refresh = () => id && adminApi.getUserDetail(id).then(setUser);

  const handleDisable = async () => {
    if (!id) return;
    const ok = await confirm('Disable user', 'Are you sure you want to disable this user? They will not be able to log in.');
    if (!ok) return;
    setSaving(true);
    try {
      await adminApi.disableUser(id);
      refresh();
    } finally {
      setSaving(false);
    }
  };

  const handleSuspend = async () => {
    if (!id) return;
    const until = prompt('Enter suspension end date (ISO format):', new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString());
    if (!until) return;
    setSaving(true);
    try {
      await adminApi.suspendUser(id, until);
      refresh();
    } finally {
      setSaving(false);
    }
  };

  const handleReactivate = async () => {
    if (!id) return;
    const ok = await confirm('Reactivate user', 'Are you sure you want to reactivate this user?');
    if (!ok) return;
    setSaving(true);
    try {
      await adminApi.reactivateUser(id);
      refresh();
    } finally {
      setSaving(false);
    }
  };

  const openSetRoleModal = () => {
    setSelectedRole(user?.role ?? 'User');
    setSetRoleModalOpen(true);
  };

  const handleSetRoleConfirm = async () => {
    if (!id) return;
    setSaving(true);
    try {
      await adminApi.setUserRole(id, selectedRole);
      addToast('Role updated successfully.', 'success');
      setSetRoleModalOpen(false);
      refresh();
    } catch {
      // Error toast is shown by api interceptor
    } finally {
      setSaving(false);
    }
  };

  if (loading || !user) {
    return <div className="text-slate-400">Loading...</div>;
  }

  return (
    <div>
      <button onClick={() => navigate(-1)} className="flex items-center gap-2 text-slate-400 hover:text-white mb-4">
        <ArrowLeft size={18} /> Back
      </button>
      <h1 className="text-xl sm:text-2xl font-bold mb-4 sm:mb-6 break-words">{user.username}</h1>
      <div className="grid gap-4 sm:gap-6 md:grid-cols-2">
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-6">
          <h2 className="font-semibold mb-4">Profile</h2>
          <dl className="space-y-2 text-sm">
            <div><dt className="text-slate-500">Name</dt><dd>{user.firstName} {user.lastName}</dd></div>
            <div><dt className="text-slate-500">Phone</dt><dd>{user.phoneNumber}</dd></div>
            <div><dt className="text-slate-500">Email</dt><dd>{user.email || '-'}</dd></div>
            <div><dt className="text-slate-500">Role</dt><dd>{user.role}</dd></div>
            <div><dt className="text-slate-500">Status</dt><dd>{user.isDisabled ? 'Disabled' : user.suspendedUntil ? 'Suspended' : 'Active'}</dd></div>
            {user.suspendedUntil && <div><dt className="text-slate-500">Suspended Until</dt><dd>{new Date(user.suspendedUntil).toLocaleString()}</dd></div>}
          </dl>
        </div>
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-6">
          <h2 className="font-semibold mb-4">Stats</h2>
          <dl className="space-y-2 text-sm">
            <div><dt className="text-slate-500">Messages</dt><dd>{user.messageCount}</dd></div>
            <div><dt className="text-slate-500">Chats</dt><dd>{user.chatCount}</dd></div>
            <div><dt className="text-slate-500">Saved Messages</dt><dd>{user.savedMessagesCount}</dd></div>
          </dl>
        </div>
      </div>
      <div className="mt-6 flex flex-wrap gap-2">
        {!user.isDisabled && !user.suspendedUntil ? (
          <>
            <button onClick={handleDisable} disabled={saving} className="flex items-center gap-2 px-4 py-2.5 bg-red-600/20 text-red-400 rounded-lg hover:bg-red-600/30 disabled:opacity-50 min-h-[44px] touch-manipulation">
              <Ban size={16} /> Disable
            </button>
            <button onClick={handleSuspend} disabled={saving} className="flex items-center gap-2 px-4 py-2.5 bg-amber-600/20 text-amber-400 rounded-lg hover:bg-amber-600/30 disabled:opacity-50 min-h-[44px] touch-manipulation">
              <Clock size={16} /> Suspend
            </button>
          </>
        ) : (
          <button onClick={handleReactivate} disabled={saving} className="flex items-center gap-2 px-4 py-2.5 bg-emerald-600/20 text-emerald-400 rounded-lg hover:bg-emerald-600/30 disabled:opacity-50 min-h-[44px] touch-manipulation">
            <CheckCircle size={16} /> Reactivate
          </button>
        )}
        <button onClick={openSetRoleModal} disabled={saving} className="flex items-center gap-2 px-4 py-2.5 bg-teal-600/20 text-teal-400 rounded-lg hover:bg-teal-600/30 disabled:opacity-50 min-h-[44px] touch-manipulation">
          <UserCog size={16} /> Set Role
        </button>
      </div>
      {setRoleModalOpen &&
        createPortal(
          <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center pt-[env(safe-area-inset-top,0px)] pl-[env(safe-area-inset-left,0px)] pr-[env(safe-area-inset-right,0px)] pb-[env(safe-area-inset-bottom,0px)] sm:p-4" role="dialog" aria-modal="true" aria-labelledby="set-role-title">
            <div className="absolute inset-0 bg-black/60" onClick={() => !saving && setSetRoleModalOpen(false)} />
            <div className="relative bg-slate-900 border border-slate-700 rounded-t-2xl sm:rounded-xl shadow-xl max-w-sm w-full p-6 pb-[max(1.5rem,env(safe-area-inset-bottom,0px))] sm:pb-6">
              <div className="sm:hidden flex justify-center -mt-2 mb-3" aria-hidden>
                <div className="w-10 h-1 rounded-full bg-slate-600" />
              </div>
              <h2 id="set-role-title" className="text-lg font-semibold mb-4">Set Role</h2>
              <label className="block text-sm text-slate-400 mb-2">New role</label>
              <select
                value={selectedRole}
                onChange={(e) => setSelectedRole(e.target.value)}
                className="w-full bg-slate-800 border border-slate-600 rounded-lg px-3 py-2 text-white focus:ring-2 focus:ring-teal-500 focus:border-transparent"
              >
                {ROLES.map((r) => (
                  <option key={r} value={r}>{r}</option>
                ))}
              </select>
              <div className="mt-6 flex gap-2 justify-end">
                <button
                  type="button"
                  onClick={() => !saving && setSetRoleModalOpen(false)}
                  disabled={saving}
                  className="px-4 py-2 rounded-lg border border-slate-600 text-slate-300 hover:bg-slate-800 disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={handleSetRoleConfirm}
                  disabled={saving}
                  className="px-4 py-2 rounded-lg bg-teal-600 text-white hover:bg-teal-700 disabled:opacity-50"
                >
                  {saving ? 'Saving…' : 'Confirm'}
                </button>
              </div>
            </div>
          </div>,
          document.body
        )}
      {user.limitOverrides.length > 0 && (
        <div className="mt-6 bg-slate-900 rounded-xl border border-slate-800 p-6">
          <h2 className="font-semibold mb-4">Limit Overrides</h2>
          <ul className="space-y-2 text-sm">
            {user.limitOverrides.map((o) => (
              <li key={o.limitKey}>{o.limitKey}: {o.value}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};
