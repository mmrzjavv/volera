import React, { useEffect, useState } from 'react';
import { adminApi, type SystemLimitDto } from '../../services/adminApi';
import { Save, Smartphone, MessageSquare, HardDrive } from 'lucide-react';

const FRIENDLY: Record<string, { label: string; hint: string; icon: React.ElementType; min?: number; max?: number; step?: number; section: string }> = {
  MaxSessionsPerUser: {
    label: 'Max sessions per user',
    hint: 'Maximum number of devices a user can be logged in on at once. When exceeded, the oldest session is logged out.',
    icon: Smartphone,
    min: 1,
    max: 100,
    step: 1,
    section: 'Sessions',
  },
  MaxPinnedMessages: {
    label: 'Max pinned messages per chat',
    hint: 'Maximum number of messages that can be pinned in a single chat.',
    icon: MessageSquare,
    min: 1,
    max: 100,
    step: 1,
    section: 'Chat',
  },
  MaxSavedMessagesCount: {
    label: 'Max saved messages count',
    hint: 'Maximum number of messages a user can save.',
    icon: MessageSquare,
    min: 0,
    max: 10000,
    step: 1,
    section: 'Chat',
  },
  MaxSavedMessagesSizeBytes: {
    label: 'Max saved messages storage (bytes)',
    hint: 'Maximum storage in bytes for saved message attachments (e.g. 52428800 = 50 MB).',
    icon: HardDrive,
    min: 0,
    max: 10 * 1024 * 1024 * 1024,
    step: 1024 * 1024,
    section: 'Storage',
  },
  MaxMessageLength: {
    label: 'Max message length (characters)',
    hint: 'Maximum number of characters allowed in a single message. Long messages will be automatically split into multiple messages.',
    icon: MessageSquare,
    min: 100,
    max: 100000,
    step: 100,
    section: 'Chat',
  },
};

function getFriendly(key: string) {
  return FRIENDLY[key] ?? { label: key, hint: '', icon: MessageSquare, section: 'Other' };
}

export const AdminLimits: React.FC = () => {
  const [limits, setLimits] = useState<SystemLimitDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState<string | null>(null);
  const [edits, setEdits] = useState<Record<string, number>>({});

  useEffect(() => {
    adminApi.getSystemLimits().then((l) => {
      const hasSessionLimit = l.some((x) => x.key === 'MaxSessionsPerUser');
      const list = hasSessionLimit ? l : [{ key: 'MaxSessionsPerUser', value: 4, description: 'Maximum active sessions (devices) per user' }, ...l];
      setLimits(list);
      setEdits(Object.fromEntries(list.map((x) => [x.key, Number(x.value)])));
    }).finally(() => setLoading(false));
  }, []);

  const handleSave = async (key: string) => {
    const value = edits[key];
    if (value === undefined) return;
    setSaving(key);
    try {
      await adminApi.setSystemLimit(key, value);
      setLimits((prev) => prev.map((l) => (l.key === key ? { ...l, value } : l)));
    } finally {
      setSaving(null);
    }
  };

  const sections = Array.from(new Set(limits.map((l) => getFriendly(l.key).section))).sort((a, b) => {
    const order = ['Sessions', 'Chat', 'Storage', 'Other'];
    return order.indexOf(a) - order.indexOf(b);
  });

  if (loading) return <div className="text-slate-400">Loading...</div>;

  return (
    <div>
      <h1 className="text-xl sm:text-2xl font-bold mb-2">Limits</h1>
      <p className="text-slate-400 text-sm mb-6">Configure system-wide limits. Changes apply immediately.</p>

      <div className="space-y-8">
        {sections.map((section) => (
          <div key={section}>
            <h2 className="text-lg font-semibold text-slate-200 mb-4">{section}</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {limits
                .filter((l) => getFriendly(l.key).section === section)
                .map((l) => {
                  const friendly = getFriendly(l.key);
                  const Icon = friendly.icon;
                  const val = edits[l.key] ?? Number(l.value);
                  const numVal = Number.isFinite(val) ? val : 0;
                  return (
                    <div
                      key={l.key}
                      className="bg-slate-900 rounded-xl border border-slate-800 p-4 sm:p-5 flex flex-col gap-3"
                    >
                      <div className="flex items-start gap-3">
                        <div className="p-2 rounded-lg bg-slate-800 text-slate-400">
                          <Icon size={20} />
                        </div>
                        <div className="flex-1 min-w-0">
                          <label className="block text-sm font-medium text-slate-200 mb-0.5">
                            {friendly.label}
                          </label>
                          {friendly.hint && (
                            <p className="text-xs text-slate-500 mb-3">{friendly.hint}</p>
                          )}
                          <div className="flex flex-wrap items-center gap-2">
                            <input
                              type="number"
                              min={friendly.min}
                              max={friendly.max}
                              step={friendly.step ?? 1}
                              value={numVal}
                              onChange={(e) =>
                                setEdits((p) => ({ ...p, [l.key]: parseFloat(e.target.value) || 0 }))
                              }
                              className="w-28 px-3 py-2.5 bg-slate-800 border border-slate-700 rounded-lg text-white focus:border-teal-500 focus:ring-1 focus:ring-teal-500 min-h-[44px] touch-manipulation"
                            />
                            <button
                              type="button"
                              onClick={() => handleSave(l.key)}
                              disabled={saving === l.key}
                              className="inline-flex items-center gap-2 px-4 py-2.5 bg-teal-600 text-white rounded-lg text-sm font-medium hover:bg-teal-700 disabled:opacity-50 min-h-[44px] touch-manipulation"
                            >
                              <Save size={16} />
                              {saving === l.key ? 'Saving…' : 'Save'}
                            </button>
                          </div>
                        </div>
                      </div>
                    </div>
                  );
                })}
            </div>
          </div>
        ))}
      </div>

      {limits.length === 0 && (
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-8 text-center text-slate-500">
          No system limits configured. They may be seeded on first run.
        </div>
      )}
    </div>
  );
};
