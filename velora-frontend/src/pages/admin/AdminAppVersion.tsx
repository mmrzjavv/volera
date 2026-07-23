import React, { useEffect, useState } from 'react';
import { adminApi } from '../../services/adminApi';
import { Save, Tag } from 'lucide-react';

export const AdminAppVersion: React.FC = () => {
  const [version, setVersion] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<'success' | 'error' | null>(null);

  useEffect(() => {
    adminApi.getVersion().then((v) => {
      setVersion(v);
    }).catch(() => setVersion('1.0.0')).finally(() => setLoading(false));
  }, []);

  const handleSave = async () => {
    const v = version.trim();
    if (!v) return;
    setSaving(true);
    setMessage(null);
    try {
      await adminApi.setVersion(v);
      setMessage('success');
      setTimeout(() => setMessage(null), 3000);
    } catch {
      setMessage('error');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="text-slate-400">Loading...</div>;

  return (
    <div>
      <h1 className="text-xl sm:text-2xl font-bold mb-4 sm:mb-6">App Version</h1>
      <p className="text-slate-400 text-sm mb-6">
        Set the current deployed app version. Users with an older client version will see &quot;A new version is available&quot; until they click &quot;Reload to update&quot;.
      </p>
      <div className="bg-slate-900 rounded-xl border border-slate-800 p-4 sm:p-6 max-w-md w-full">
        <label className="block text-slate-400 text-sm font-medium mb-2">Current version</label>
        <div className="flex flex-col sm:flex-row gap-2 items-stretch sm:items-center">
          <div className="flex-1 min-w-0 relative">
            <Tag className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" size={18} />
            <input
              type="text"
              value={version}
              onChange={(e) => setVersion(e.target.value)}
              placeholder="e.g. 1.0.0"
              className="w-full pl-9 pr-4 py-2.5 bg-slate-800 border border-slate-700 rounded-lg text-white placeholder-slate-500 focus:border-teal-500 focus:ring-1 focus:ring-teal-500"
            />
          </div>
          <button
            onClick={handleSave}
            disabled={saving}
            className="flex items-center justify-center gap-2 px-4 py-2.5 bg-teal-600 text-white rounded-lg hover:bg-teal-700 disabled:opacity-50 font-medium min-h-[44px] touch-manipulation"
          >
            <Save size={18} />
            {saving ? 'Saving…' : 'Save'}
          </button>
        </div>
        {message === 'success' && (
          <p className="mt-3 text-green-400 text-sm">Version updated. Users will see the update banner if their client is older.</p>
        )}
        {message === 'error' && (
          <p className="mt-3 text-red-400 text-sm">Failed to save. Try again.</p>
        )}
      </div>
    </div>
  );
};
