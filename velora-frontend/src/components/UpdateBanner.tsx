import { useState } from 'react';
import { useVersionStore } from '../store/useVersionStore';
import { RefreshCw, X } from 'lucide-react';

export function UpdateBanner() {
  const { updateAvailable, clearCacheAndReload, dismissUpdate } = useVersionStore();
  const [reloading, setReloading] = useState(false);

  if (!updateAvailable) return null;

  const handleReload = async () => {
    if (reloading) return;
    setReloading(true);
    await clearCacheAndReload();
  };

  return (
    <div className="w-full bg-[var(--volera-accent)] text-white px-4 py-2.5 flex flex-wrap items-center justify-center gap-3 shadow-sm">
      <span className="text-sm font-medium">A new version is available.</span>
      <button
        type="button"
        onClick={handleReload}
        disabled={reloading}
        className="flex items-center gap-1.5 bg-white text-[var(--volera-accent)] px-3 py-1.5 min-h-[36px] rounded-lg text-sm font-medium hover:bg-teal-50 transition-colors disabled:opacity-70 disabled:cursor-not-allowed"
      >
        <RefreshCw size={14} className={reloading ? 'animate-spin' : ''} />
        {reloading ? 'Reloading…' : 'Reload to update'}
      </button>
      <button
        type="button"
        onClick={() => dismissUpdate()}
        className="p-2 min-h-[44px] min-w-[44px] flex items-center justify-center text-white/90 hover:text-white hover:bg-white/20 rounded-lg transition-colors"
        title="Dismiss"
        aria-label="Dismiss"
      >
        <X size={18} />
      </button>
    </div>
  );
}
