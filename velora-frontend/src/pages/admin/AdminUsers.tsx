import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { adminApi, type AdminUserListDto, type AdminSessionDto, type PaginatedResult } from '../../services/adminApi';
import { Search, ChevronLeft, ChevronRight, Monitor, X, LogOut } from 'lucide-react';

export const AdminUsers: React.FC = () => {
  const [result, setResult] = useState<PaginatedResult<AdminUserListDto> | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [searchTerm, setSearchTerm] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [isDisabledFilter, setIsDisabledFilter] = useState<boolean | undefined>();
  const [sessionsModal, setSessionsModal] = useState<{ userId: string; username: string } | null>(null);
  const [sessions, setSessions] = useState<AdminSessionDto[]>([]);
  const [sessionsLoading, setSessionsLoading] = useState(false);
  const [revokingSessionId, setRevokingSessionId] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    adminApi
      .getUsers({ page, pageSize: 20, searchTerm: searchTerm || undefined, roleFilter: roleFilter || undefined, isDisabled: isDisabledFilter })
      .then(setResult)
      .finally(() => setLoading(false));
  }, [page, searchTerm, roleFilter, isDisabledFilter]);

  useEffect(() => {
    if (!sessionsModal) return;
    setSessionsLoading(true);
    setSessions([]);
    adminApi
      .getUserSessions(sessionsModal.userId)
      .then(setSessions)
      .finally(() => setSessionsLoading(false));
  }, [sessionsModal]);

  const openSessionsModal = (userId: string, username: string) => setSessionsModal({ userId, username });
  const closeSessionsModal = () => setSessionsModal(null);

  const revokeSession = async (sessionId: string) => {
    if (!sessionsModal) return;
    setRevokingSessionId(sessionId);
    try {
      await adminApi.revokeUserSession(sessionsModal.userId, sessionId);
      setSessions((prev) => prev.filter((s) => s.id !== sessionId));
    } finally {
      setRevokingSessionId(null);
    }
  };

  const formatDate = (s: string) => {
    try {
      const d = new Date(s);
      return d.toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' });
    } catch {
      return s;
    }
  };

  return (
    <div>
      <h1 className="text-xl sm:text-2xl font-bold mb-4 sm:mb-6">Users</h1>
      <div className="flex flex-wrap gap-3 sm:gap-4 mb-4">
        <div className="relative flex-1 min-w-0 w-full sm:min-w-[200px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" size={18} />
          <input
            type="text"
            placeholder="Search..."
            value={searchTerm}
            onChange={(e) => { setSearchTerm(e.target.value); setPage(1); }}
            className="w-full pl-9 pr-3 py-2.5 bg-slate-900 border border-slate-700 rounded-lg text-white placeholder-slate-500 touch-manipulation"
          />
        </div>
        <select
          value={roleFilter}
          onChange={(e) => { setRoleFilter(e.target.value); setPage(1); }}
          className="px-3 py-2.5 bg-slate-900 border border-slate-700 rounded-lg text-white touch-manipulation min-h-[44px]"
        >
          <option value="">All Roles</option>
          <option value="User">User</option>
          <option value="Moderator">Moderator</option>
          <option value="Admin">Admin</option>
          <option value="SuperAdmin">SuperAdmin</option>
        </select>
        <select
          value={isDisabledFilter === undefined ? '' : isDisabledFilter ? 'disabled' : 'active'}
          onChange={(e) => {
            const v = e.target.value;
            setIsDisabledFilter(v === '' ? undefined : v === 'disabled');
            setPage(1);
          }}
          className="px-3 py-2.5 bg-slate-900 border border-slate-700 rounded-lg text-white touch-manipulation min-h-[44px]"
        >
          <option value="">All Status</option>
          <option value="active">Active</option>
          <option value="disabled">Disabled</option>
        </select>
      </div>
      <div className="bg-slate-900 rounded-xl border border-slate-800 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-slate-400">Loading...</div>
        ) : (
          <>
          <div className="hidden md:block overflow-x-auto">
            <table className="w-full min-w-[640px]">
              <thead>
                <tr className="border-b border-slate-800">
                  <th className="text-left px-4 py-3 text-slate-400 font-medium">Username</th>
                  <th className="text-left px-4 py-3 text-slate-400 font-medium">Name</th>
                  <th className="text-left px-4 py-3 text-slate-400 font-medium">Role</th>
                  <th className="text-left px-4 py-3 text-slate-400 font-medium">Status</th>
                  <th className="text-left px-4 py-3 text-slate-400 font-medium">Messages</th>
                  <th className="text-left px-4 py-3 text-slate-400 font-medium">Chats</th>
                  <th className="text-left px-4 py-3 text-slate-400 font-medium text-center sm:text-left">Sessions</th>
                </tr>
              </thead>
              <tbody>
                {result?.items.map((u) => (
                  <tr key={u.id} className="border-b border-slate-800 hover:bg-slate-800/50">
                    <td className="px-4 py-3">
                      <Link to={`/admin/users/${u.id}`} className="text-teal-400 hover:underline">
                        {u.username}
                      </Link>
                    </td>
                    <td className="px-4 py-3">{u.firstName} {u.lastName}</td>
                    <td className="px-4 py-3">{u.role}</td>
                    <td className="px-4 py-3">
                      {u.isDisabled ? <span className="text-red-400">Disabled</span> : u.suspendedUntil ? <span className="text-amber-400">Suspended</span> : <span className="text-emerald-400">Active</span>}
                    </td>
                    <td className="px-4 py-3">{u.messageCount}</td>
                    <td className="px-4 py-3">{u.chatCount}</td>
                    <td className="px-4 py-3 text-center sm:text-left">
                      <button
                        type="button"
                        onClick={() => openSessionsModal(u.id, u.username)}
                        className="inline-flex items-center justify-center gap-1.5 px-3 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white border border-slate-700 min-h-[44px] touch-manipulation"
                        aria-label={`View sessions for ${u.username}`}
                      >
                        <Monitor size={18} />
                        <span className="hidden sm:inline">Sessions</span>
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="md:hidden p-4 space-y-3">
            {result?.items.map((u) => (
              <div key={u.id} className="bg-slate-800/60 border border-slate-700 rounded-lg p-4 flex flex-col gap-2 text-sm">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <Link to={`/admin/users/${u.id}`} className="text-teal-400 hover:underline font-medium truncate">
                    {u.username}
                  </Link>
                  <span className={u.isDisabled ? 'text-red-400' : u.suspendedUntil ? 'text-amber-400' : 'text-emerald-400'}>
                    {u.isDisabled ? 'Disabled' : u.suspendedUntil ? 'Suspended' : 'Active'}
                  </span>
                </div>
                <div className="text-slate-400">
                  {[u.firstName, u.lastName].filter(Boolean).join(' ') || '—'} · {u.role}
                </div>
                <div className="flex flex-wrap items-center justify-between gap-2 text-slate-500">
                  <span>Messages: {u.messageCount}</span>
                  <span>Chats: {u.chatCount}</span>
                  <button
                    type="button"
                    onClick={() => openSessionsModal(u.id, u.username)}
                    className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg bg-slate-700 hover:bg-slate-600 text-slate-300 min-h-[44px] touch-manipulation"
                    aria-label={`View sessions for ${u.username}`}
                  >
                    <Monitor size={18} /> Sessions
                  </button>
                </div>
              </div>
            ))}
          </div>
            {result && result.totalPages > 1 && (
              <div className="flex flex-wrap items-center justify-between gap-2 px-4 py-3 border-t border-slate-800">
                <span className="text-slate-400 text-sm">
                  Page {result.page} of {result.totalPages} ({result.totalCount} total)
                </span>
                <div className="flex gap-2">
                  <button
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    disabled={page <= 1}
                    className="p-2.5 rounded-lg bg-slate-800 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-slate-700 min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation"
                  >
                    <ChevronLeft size={18} />
                  </button>
                  <button
                    onClick={() => setPage((p) => Math.min(result.totalPages, p + 1))}
                    disabled={page >= result.totalPages}
                    className="p-2.5 rounded-lg bg-slate-800 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-slate-700 min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation"
                  >
                    <ChevronRight size={18} />
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {sessionsModal && (
        <div
          className="fixed inset-0 z-50 flex items-end sm:items-center justify-center pt-[env(safe-area-inset-top,0px)] pl-[env(safe-area-inset-left,0px)] pr-[env(safe-area-inset-right,0px)] pb-[env(safe-area-inset-bottom,0px)] sm:p-4 md:p-6 bg-black/60 backdrop-blur-sm"
          role="dialog"
          aria-modal="true"
          aria-labelledby="sessions-modal-title"
          onClick={closeSessionsModal}
        >
          <div
            className="bg-slate-900 border border-slate-700 rounded-t-2xl sm:rounded-xl shadow-xl w-full max-w-2xl max-h-[min(92dvh,100%)] h-[min(85dvh,100%)] sm:h-auto sm:max-h-[90vh] flex flex-col"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="sm:hidden flex justify-center pt-2 shrink-0" aria-hidden>
              <div className="w-10 h-1 rounded-full bg-slate-600" />
            </div>
            <div className="flex items-center justify-between gap-3 px-4 py-3 sm:px-6 border-b border-slate-800 shrink-0">
              <h2 id="sessions-modal-title" className="text-lg sm:text-xl font-semibold text-white truncate">
                Sessions — {sessionsModal.username}
              </h2>
              <button
                type="button"
                onClick={closeSessionsModal}
                className="p-2 rounded-lg hover:bg-slate-800 text-slate-400 hover:text-white min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation"
                aria-label="Close"
              >
                <X size={20} />
              </button>
            </div>
            <div className="overflow-y-auto flex-1 min-h-0 px-4 py-3 sm:px-6 sm:py-4 overscroll-contain">
              {sessionsLoading ? (
                <div className="py-8 text-center text-slate-400">Loading sessions...</div>
              ) : sessions.length === 0 ? (
                <div className="py-8 text-center text-slate-400">No active sessions.</div>
              ) : (
                <ul className="space-y-3 sm:space-y-4">
                  {sessions.map((s) => (
                    <li
                      key={s.id}
                      className="bg-slate-800/60 border border-slate-700 rounded-lg p-3 sm:p-4"
                    >
                      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 sm:gap-x-4 sm:gap-y-1 text-sm">
                        <div className="flex flex-wrap gap-x-2 gap-y-0.5">
                          <span className="text-slate-500">Device:</span>
                          <span className="text-white">{s.deviceType}</span>
                        </div>
                        <div className="flex flex-wrap gap-x-2 gap-y-0.5">
                          <span className="text-slate-500">Browser:</span>
                          <span className="text-white">{s.browser}</span>
                        </div>
                        <div className="flex flex-wrap gap-x-2 gap-y-0.5">
                          <span className="text-slate-500">OS:</span>
                          <span className="text-white">{s.os}</span>
                        </div>
                        <div className="flex flex-wrap gap-x-2 gap-y-0.5">
                          <span className="text-slate-500">Location:</span>
                          <span className="text-white truncate" title={s.location}>{s.location || '—'}</span>
                        </div>
                        <div className="sm:col-span-2 flex flex-wrap gap-x-4 gap-y-1 mt-1">
                          <span className="text-slate-500">Login:</span>
                          <span className="text-slate-300">{formatDate(s.loginAt)}</span>
                          <span className="text-slate-500 ml-2 sm:ml-0">Last activity:</span>
                          <span className="text-slate-300">{formatDate(s.lastActivityAt)}</span>
                        </div>
                        <div className="flex flex-wrap gap-x-2 gap-y-0.5 sm:col-span-2">
                          <span className="text-slate-500">App version:</span>
                          <span className="text-slate-300">{s.appVersion || '—'}</span>
                          {s.isRevoked && (
                            <span className="text-red-400 ml-2">Revoked</span>
                          )}
                        </div>
                      </div>
                      {!s.isRevoked && (
                        <div className="mt-3 pt-3 border-t border-slate-700 flex justify-end">
                          <button
                            type="button"
                            onClick={() => revokeSession(s.id)}
                            disabled={revokingSessionId === s.id}
                            className="inline-flex items-center gap-2 px-3 py-2 rounded-lg bg-red-900/50 hover:bg-red-900/70 text-red-300 hover:text-red-200 border border-red-800/50 min-h-[44px] touch-manipulation disabled:opacity-50"
                            title="Log out this device — user will be redirected to login"
                          >
                            <LogOut size={18} />
                            {revokingSessionId === s.id ? 'Revoking…' : 'Log out this device'}
                          </button>
                        </div>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
