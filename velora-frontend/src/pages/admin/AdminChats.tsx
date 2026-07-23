import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { adminApi, type AdminChatDto, type PaginatedResult } from '../../services/adminApi';
import { Search, ChevronLeft, ChevronRight } from 'lucide-react';

export const AdminChats: React.FC = () => {
  const [result, setResult] = useState<PaginatedResult<AdminChatDto> | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [searchTerm, setSearchTerm] = useState('');
  const [typeFilter, setTypeFilter] = useState('');

  useEffect(() => {
    setLoading(true);
    adminApi
      .getChats({ page, pageSize: 20, searchTerm: searchTerm || undefined, type: typeFilter || undefined })
      .then(setResult)
      .finally(() => setLoading(false));
  }, [page, searchTerm, typeFilter]);

  return (
    <div>
      <h1 className="text-xl sm:text-2xl font-bold mb-4 sm:mb-6">Chats</h1>
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
          value={typeFilter}
          onChange={(e) => { setTypeFilter(e.target.value); setPage(1); }}
          className="px-3 py-2.5 bg-slate-900 border border-slate-700 rounded-lg text-white touch-manipulation min-h-[44px]"
        >
          <option value="">All</option>
          <option value="Dm">Direct Messages</option>
          <option value="Group">Groups</option>
        </select>
      </div>
      <div className="bg-slate-900 rounded-xl border border-slate-800 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-slate-400">Loading...</div>
        ) : (
          <>
          <div className="hidden md:block overflow-x-auto">
            <table className="w-full min-w-[520px]">
            <thead>
              <tr className="border-b border-slate-800">
                <th className="text-left px-4 py-3 text-slate-400 font-medium">Type</th>
                <th className="text-left px-4 py-3 text-slate-400 font-medium">Name / Key</th>
                <th className="text-left px-4 py-3 text-slate-400 font-medium">Last Message</th>
              </tr>
            </thead>
            <tbody>
              {result?.items.map((c) => (
                <tr key={c.conversationKey} className="border-b border-slate-800 hover:bg-slate-800/50">
                  <td className="px-4 py-3">{c.type}</td>
                  <td className="px-4 py-3">
                    <Link to={`/admin/chats/${encodeURIComponent(c.conversationKey)}`} className="text-teal-400 hover:underline">
                      {c.type === 'Dm' && (c.userName1 || c.userName2)
                        ? `${c.userName1 ?? c.userId1 ?? '?'} ↔ ${c.userName2 ?? c.userId2 ?? '?'}`
                        : c.groupName ?? c.conversationKey}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-slate-400 text-sm truncate max-w-xs">
                    {c.lastMessageContent ?? '-'} {c.lastMessageAt ? `(${new Date(c.lastMessageAt).toLocaleString()})` : ''}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
          <div className="md:hidden p-4 space-y-3">
            {result?.items.map((c) => (
              <Link
                key={c.conversationKey}
                to={`/admin/chats/${encodeURIComponent(c.conversationKey)}`}
                className="block bg-slate-800/60 border border-slate-700 rounded-lg p-4 flex flex-col gap-2 text-sm"
              >
                <div className="flex items-center justify-between gap-2">
                  <span className="text-slate-500 text-xs">{c.type}</span>
                  {c.lastMessageAt && (
                    <span className="text-slate-500 text-xs">{new Date(c.lastMessageAt).toLocaleString()}</span>
                  )}
                </div>
                <span className="text-teal-400 font-medium line-clamp-2">
                  {c.type === 'Dm' && (c.userName1 || c.userName2)
                    ? `${c.userName1 ?? c.userId1 ?? '?'} ↔ ${c.userName2 ?? c.userId2 ?? '?'}`
                    : c.groupName ?? c.conversationKey}
                </span>
                <p className="text-slate-400 truncate">{c.lastMessageContent ?? '—'}</p>
              </Link>
            ))}
          </div>
          {result && result.totalPages > 1 && (
            <div className="flex flex-wrap items-center justify-between gap-2 px-4 py-3 border-t border-slate-800">
              <span className="text-slate-400 text-sm">Page {result.page} of {result.totalPages}</span>
              <div className="flex gap-2">
                <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1} className="p-2.5 rounded-lg bg-slate-800 disabled:opacity-50 min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation" aria-label="Previous page">
                  <ChevronLeft size={18} />
                </button>
                <button onClick={() => setPage((p) => Math.min(result.totalPages, p + 1))} disabled={page >= result.totalPages} className="p-2.5 rounded-lg bg-slate-800 disabled:opacity-50 min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation" aria-label="Next page">
                  <ChevronRight size={18} />
                </button>
              </div>
            </div>
          )}
          </>
        )}
      </div>
    </div>
  );
};
