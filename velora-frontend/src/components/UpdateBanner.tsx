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
    <div className="fixed top-0 left-0 right-0 z-[100] bg-blue-600 text-white px-4 py-2 flex items-center justify-center gap-3 shadow-md">
      <span className="text-sm font-medium">A new version is available.</span>
      <button
        type="button"
        onClick={handleReload}
        disabled={reloading}
        className="flex items-center gap-1.5 bg-white text-blue-600 px-3 py-1.5 rounded-lg text-sm font-medium hover:bg-blue-50 transition-colors disabled:opacity-70 disabled:cursor-not-allowed"
      >
        <RefreshCw size={14} className={reloading ? 'animate-spin' : ''} />
        {reloading ? 'Reloading…' : 'Reload to update'}
      </button>
      <button
        type="button"
        onClick={() => dismissUpdate()}
        className="p-1.5 text-white/90 hover:text-white hover:bg-white/20 rounded-lg transition-colors"
        title="Dismiss"
        aria-label="Dismiss"
      >
        <X size={18} />
      </button>
    </div>
  );
}
