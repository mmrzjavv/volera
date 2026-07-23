import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { adminApi, type UserUsageDto, type PaginatedResult } from '../../services/adminApi';
import { ChevronLeft, ChevronRight } from 'lucide-react';

export const AdminUserUsage: React.FC = () => {
  const [result, setResult] = useState<PaginatedResult<UserUsageDto> | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState('messageCount');
  const [sortDesc, setSortDesc] = useState(true);

  useEffect(() => {
    setLoading(true);
    adminApi.getUserUsage(page, 20, sortBy, sortDesc).then(setResult).finally(() => setLoading(false));
  }, [page, sortBy, sortDesc]);

  const toggleSort = (field: string) => {
    setSortBy(field);
    setSortDesc((d) => (sortBy === field ? !d : true));
    setPage(1);
  };

  return (
    <div>
      <h1 className="text-xl sm:text-2xl font-bold mb-4 sm:mb-6">User Usage</h1>
      <p className="text-slate-400 text-sm mb-4">Message and saved-message counts per user (paginated for large datasets).</p>
      <div className="bg-slate-900 rounded-xl border border-slate-800 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-slate-400">Loading...</div>
        ) : (
          <>
          <div className="hidden md:block overflow-x-auto">
            <table className="w-full min-w-[400px]">
            <thead>
              <tr className="border-b border-slate-800">
                <th className="text-left px-4 py-3 text-slate-400 font-medium">User</th>
                <th className="text-left px-4 py-3">
                  <button onClick={() => toggleSort('messageCount')} className="text-slate-400 hover:text-white">
                    Messages {sortBy === 'messageCount' && (sortDesc ? '↓' : '↑')}
                  </button>
                </th>
                <th className="text-left px-4 py-3">
                  <button onClick={() => toggleSort('savedCount')} className="text-slate-400 hover:text-white">
                    Saved {sortBy === 'savedCount' && (sortDesc ? '↓' : '↑')}
                  </button>
                </th>
              </tr>
            </thead>
            <tbody>
              {result?.items.map((u) => (
                <tr key={u.userId} className="border-b border-slate-800 hover:bg-slate-800/50">
                  <td className="px-4 py-3">
                    <Link to={`/admin/users/${u.userId}`} className="text-teal-400 hover:underline">{u.username}</Link>
                  </td>
                  <td className="px-4 py-3">{u.messageCount.toLocaleString()}</td>
                  <td className="px-4 py-3">{u.savedMessagesCount.toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
          <div className="md:hidden p-4 space-y-3">
            {result?.items.map((u) => (
              <Link
                key={u.userId}
                to={`/admin/users/${u.userId}`}
                className="block bg-slate-800/60 border border-slate-700 rounded-lg p-4 flex flex-col gap-2 text-sm"
              >
                <span className="text-teal-400 font-medium truncate">{u.username}</span>
                <div className="flex flex-wrap gap-3 text-slate-400">
                  <span>Messages: {u.messageCount.toLocaleString()}</span>
                  <span>Saved: {u.savedMessagesCount.toLocaleString()}</span>
                </div>
              </Link>
            ))}
          </div>
          </>
        )}
        {result && result.totalPages > 1 && (
          <div className="flex flex-wrap items-center justify-between gap-2 px-4 py-3 border-t border-slate-800">
            <span className="text-slate-400 text-sm">Page {result.page} of {result.totalPages}</span>
            <div className="flex gap-2">
              <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1} className="p-2.5 rounded-lg bg-slate-800 disabled:opacity-50 min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation"><ChevronLeft size={18} /></button>
              <button onClick={() => setPage((p) => Math.min(result.totalPages, p + 1))} disabled={page >= result.totalPages} className="p-2.5 rounded-lg bg-slate-800 disabled:opacity-50 min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation"><ChevronRight size={18} /></button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
