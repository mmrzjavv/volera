import React, { useEffect, useState } from 'react';
import { adminApi, type AdminMessageDto, type PaginatedResult } from '../../services/adminApi';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useConfirmationStore } from '../../store/useConfirmationStore';

function askConfirm(title: string, message: string): Promise<boolean> {
  return new Promise((resolve) => {
    useConfirmationStore.getState().openDialog({
      title,
      message,
      variant: 'danger',
      onConfirm: () => resolve(true),
      onCancel: () => resolve(false),
    });
  });
}

export const AdminMessageSearch: React.FC = () => {
  const [result, setResult] = useState<PaginatedResult<AdminMessageDto> | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [content, setContent] = useState('');
  const [senderId, setSenderId] = useState('');
  const [groupId, setGroupId] = useState('');

  useEffect(() => {
    setLoading(true);
    adminApi
      .searchMessages({ page, pageSize: 20, content: content || undefined, senderId: senderId || undefined, groupId: groupId || undefined })
      .then(setResult)
      .finally(() => setLoading(false));
  }, [page, content, senderId, groupId]);

  const handleDelete = async (id: string, hardDelete = false) => {
    const ok = await askConfirm('Delete message', hardDelete ? 'Are you sure? This will permanently delete the message.' : 'Are you sure? This will soft-delete the message.');
    if (!ok) return;
    try {
      await adminApi.deleteMessage(id, hardDelete);
      setResult((r) => r ? { ...r, items: r.items.filter((m) => m.id !== id), totalCount: r.totalCount - 1 } : null);
    } catch (e) {
      console.error(e);
    }
  };

  return (
    <div>
      <h1 className="text-xl sm:text-2xl font-bold mb-4 sm:mb-6">Message Search</h1>
      <div className="flex flex-wrap gap-3 sm:gap-4 mb-4">
        <input
          type="text"
          placeholder="Search content..."
          value={content}
          onChange={(e) => { setContent(e.target.value); setPage(1); }}
          className="flex-1 min-w-0 sm:flex-initial px-3 py-2.5 bg-slate-900 border border-slate-700 rounded-lg text-white placeholder-slate-500 touch-manipulation"
        />
        <input
          type="text"
          placeholder="Sender ID"
          value={senderId}
          onChange={(e) => { setSenderId(e.target.value); setPage(1); }}
          className="flex-1 min-w-0 sm:flex-initial px-3 py-2.5 bg-slate-900 border border-slate-700 rounded-lg text-white placeholder-slate-500 touch-manipulation"
        />
        <input
          type="text"
          placeholder="Group ID"
          value={groupId}
          onChange={(e) => { setGroupId(e.target.value); setPage(1); }}
          className="flex-1 min-w-0 sm:flex-initial px-3 py-2.5 bg-slate-900 border border-slate-700 rounded-lg text-white placeholder-slate-500 touch-manipulation"
        />
      </div>
      <div className="bg-slate-900 rounded-xl border border-slate-800 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-slate-400">Loading...</div>
        ) : (
          <>
          <div className="hidden md:block overflow-x-auto">
            <table className="w-full min-w-[600px]">
            <thead>
              <tr className="border-b border-slate-800">
                <th className="text-left px-4 py-3 text-slate-400 font-medium">Sender</th>
                <th className="text-left px-4 py-3 text-slate-400 font-medium">Content</th>
                <th className="text-left px-4 py-3 text-slate-400 font-medium">Sent At</th>
                <th className="text-left px-4 py-3 text-slate-400 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {result?.items.map((m) => (
                <tr key={m.id} className="border-b border-slate-800 hover:bg-slate-800/50">
                  <td className="px-4 py-3 text-sm">{m.senderUsername ?? m.senderId}</td>
                  <td className="px-4 py-3 text-sm truncate max-w-xs">{m.content}</td>
                  <td className="px-4 py-3 text-sm text-slate-400">{new Date(m.sentAt).toLocaleString()}</td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-2">
                      <button type="button" onClick={() => handleDelete(m.id, false)} className="text-amber-400 hover:underline py-1.5 touch-manipulation min-h-[44px]">Soft Delete</button>
                      <button type="button" onClick={() => handleDelete(m.id, true)} className="text-red-400 hover:underline py-1.5 touch-manipulation min-h-[44px]">Hard Delete</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
          <div className="md:hidden p-4 space-y-3">
            {result?.items.map((m) => (
              <div key={m.id} className="bg-slate-800/60 border border-slate-700 rounded-lg p-4 flex flex-col gap-2 text-sm">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <span className="font-medium text-slate-200">{m.senderUsername ?? m.senderId}</span>
                  <span className="text-slate-500 text-xs">{new Date(m.sentAt).toLocaleString()}</span>
                </div>
                <p className="text-slate-400 line-clamp-2 break-words">{m.content}</p>
                <div className="flex flex-wrap gap-2">
                  <button type="button" onClick={() => handleDelete(m.id, false)} className="text-amber-400 hover:underline py-2 touch-manipulation min-h-[44px]">Soft Delete</button>
                  <button type="button" onClick={() => handleDelete(m.id, true)} className="text-red-400 hover:underline py-2 touch-manipulation min-h-[44px]">Hard Delete</button>
                </div>
              </div>
            ))}
          </div>
          </>
        )}
        {result && result.totalPages > 1 && (
          <div className="flex flex-wrap items-center justify-between gap-2 px-4 py-3 border-t border-slate-800">
            <span className="text-slate-400 text-sm">Page {result.page} of {result.totalPages}</span>
            <div className="flex gap-2">
              <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1} className="p-2.5 rounded-lg bg-slate-800 disabled:opacity-50 min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation"> <ChevronLeft size={18} /> </button>
              <button onClick={() => setPage((p) => Math.min(result.totalPages, p + 1))} disabled={page >= result.totalPages} className="p-2.5 rounded-lg bg-slate-800 disabled:opacity-50 min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation"> <ChevronRight size={18} /> </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
