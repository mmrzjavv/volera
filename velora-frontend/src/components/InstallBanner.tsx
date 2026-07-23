import { useState } from 'react';
import { useInstallPrompt } from '../hooks/useInstallPrompt';
import { Smartphone, X } from 'lucide-react';

const STORAGE_KEY = 'install_banner_dismissed';

export function InstallBanner() {
  const { canInstall, promptInstall, isInstalled, isIOS } = useInstallPrompt();
  const [dismissed, setDismissed] = useState(() => {
    try {
      return localStorage.getItem(STORAGE_KEY) === '1';
    } catch {
      return false;
    }
  });
  const [installing, setInstalling] = useState(false);

  if (isInstalled || dismissed) return null;

  const handleInstall = async () => {
    if (!canInstall) return;
    setInstalling(true);
    try {
      const installed = await promptInstall();
      if (installed) setDismissed(true);
    } finally {
      setInstalling(false);
    }
  };

  const handleDismiss = () => {
    setDismissed(true);
    try {
      localStorage.setItem(STORAGE_KEY, '1');
    } catch {}
  };

  return (
    <div
      className="fixed bottom-0 left-0 right-0 z-[98] flex justify-center px-3 pb-3 pointer-events-none"
      style={{ paddingBottom: 'max(0.75rem, env(safe-area-inset-bottom))' }}
    >
      <div
        className="pointer-events-auto w-full max-w-md flex items-center gap-3 rounded-xl px-3 py-2.5 shadow-lg ring-1 ring-black/5 dark:ring-white/10 bg-white/95 dark:bg-gray-800/95 backdrop-blur-sm text-gray-900 dark:text-gray-100"
        role="banner"
      >
        <div className="flex shrink-0 size-9 rounded-lg bg-teal-500/10 dark:bg-teal-400/10 flex items-center justify-center">
          <Smartphone size={18} className="text-teal-600 dark:text-teal-400" />
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium">Install Volera</p>
          <p className="text-xs text-gray-500 dark:text-gray-400">
            {canInstall
              ? 'Add to home screen for quick access'
              : isIOS
                ? 'Tap Share and choose Add to Home Screen'
                : 'Use your browser menu to install this app'}
          </p>
        </div>
        <div className="flex items-center gap-1.5 shrink-0">
          {canInstall && (
            <button
              type="button"
              onClick={handleInstall}
              disabled={installing}
              className="px-3 py-1.5 rounded-lg text-sm font-medium bg-teal-600 hover:bg-teal-700 disabled:opacity-70 text-white transition-colors"
            >
              {installing ? '...' : 'Install'}
            </button>
          )}
          <button
            type="button"
            onClick={handleDismiss}
            className="p-1.5 rounded-lg text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 hover:bg-gray-200/50 dark:hover:bg-gray-700/50 transition-colors"
            aria-label="Dismiss"
          >
            <X size={16} />
          </button>
        </div>
      </div>
    </div>
  );
}
