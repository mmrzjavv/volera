import React, { useEffect, useState } from 'react';
import { adminApi, type AdminAuditLogDto, type PaginatedResult } from '../../services/adminApi';
import { ChevronLeft, ChevronRight } from 'lucide-react';

export const AdminAudit: React.FC = () => {
  const [result, setResult] = useState<PaginatedResult<AdminAuditLogDto> | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);

  useEffect(() => {
    setLoading(true);
    adminApi.getAuditLog({ page, pageSize: 20 }).then(setResult).finally(() => setLoading(false));
  }, [page]);

  return (
    <div>
      <h1 className="text-xl sm:text-2xl font-bold mb-4 sm:mb-6">Audit Log</h1>
      <div className="bg-slate-900 rounded-xl border border-slate-800 overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-slate-400">Loading...</div>
        ) : (
          <>
          <div className="hidden md:block overflow-x-auto">
            <table className="w-full min-w-[600px]">
            <thead>
              <tr className="border-b border-slate-800">
                <th className="text-left px-4 py-3 text-slate-400 font-medium">Admin</th>
                <th className="text-left px-4 py-3 text-slate-400 font-medium">Action</th>
                <th className="text-left px-4 py-3 text-slate-400 font-medium">Resource</th>
                <th className="text-left px-4 py-3 text-slate-400 font-medium">Details</th>
                <th className="text-left px-4 py-3 text-slate-400 font-medium">Time</th>
              </tr>
            </thead>
            <tbody>
              {result?.items.map((a) => (
                <tr key={a.id} className="border-b border-slate-800 hover:bg-slate-800/50">
                  <td className="px-4 py-3 text-sm">{a.adminUsername ?? a.adminUserId}</td>
                  <td className="px-4 py-3 text-sm">{a.action}</td>
                  <td className="px-4 py-3 text-sm">{a.resourceType} {a.resourceId ?? ''}</td>
                  <td className="px-4 py-3 text-sm text-slate-400 truncate max-w-xs">{a.details ?? '-'}</td>
                  <td className="px-4 py-3 text-sm text-slate-400">{new Date(a.createdAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
          <div className="md:hidden p-4 space-y-3">
            {result?.items.map((a) => (
              <div key={a.id} className="bg-slate-800/60 border border-slate-700 rounded-lg p-4 flex flex-col gap-2 text-sm">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <span className="font-medium text-slate-200">{a.adminUsername ?? a.adminUserId}</span>
                  <span className="text-slate-500 text-xs">{new Date(a.createdAt).toLocaleString()}</span>
                </div>
                <div className="text-slate-400">{a.action} · {a.resourceType} {a.resourceId ?? ''}</div>
                {a.details && <p className="text-slate-500 truncate" title={a.details}>{a.details}</p>}
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
