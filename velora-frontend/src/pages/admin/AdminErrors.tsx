import React, { useEffect, useState } from 'react';
import { adminApi } from '../../services/adminApi';
import { AlertTriangle, ExternalLink } from 'lucide-react';

export const AdminErrors: React.FC = () => {
  const [uiUrl, setUiUrl] = useState('http://localhost:5341');
  const [message, setMessage] = useState('Application errors are shipped to Seq via Serilog.');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    adminApi.getErrorLoggingInfo()
      .then((info) => {
        if (info.uiUrl) setUiUrl(info.uiUrl);
        if (info.message) setMessage(info.message);
      })
      .catch(() => { /* keep defaults */ })
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="p-4 sm:p-6 max-w-2xl">
      <div className="flex items-center gap-3 mb-6">
        <AlertTriangle className="text-amber-400 shrink-0" size={28} />
        <div>
          <h1 className="text-xl font-semibold text-slate-100">Error logs</h1>
          <p className="text-sm text-slate-400 mt-1">Errors are ingested by Serilog into Seq.</p>
        </div>
      </div>

      <div className="rounded-xl border border-slate-700 bg-slate-900/60 p-5 space-y-4">
        {loading ? (
          <p className="text-slate-400 text-sm">Loading…</p>
        ) : (
          <>
            <p className="text-slate-300 text-sm leading-relaxed">{message}</p>
            <a
              href={uiUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 px-4 py-2.5 rounded-lg bg-teal-600 hover:bg-teal-500 text-white text-sm font-medium transition-colors"
            >
              Open Seq
              <ExternalLink size={16} />
            </a>
            <p className="text-xs text-slate-500 break-all">{uiUrl}</p>
          </>
        )}
      </div>
    </div>
  );
};
