import { useEffect, useState } from 'react';
import { useInstallPrompt } from '../hooks/useInstallPrompt';
import { Smartphone, X } from 'lucide-react';

const STORAGE_KEY = 'install_banner_dismissed';
const BANNER_OFFSET = '4.75rem';

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

  const visible = !isInstalled && !dismissed;

  useEffect(() => {
    document.documentElement.style.setProperty(
      '--install-banner-offset',
      visible ? BANNER_OFFSET : '0px'
    );
    return () => {
      document.documentElement.style.setProperty('--install-banner-offset', '0px');
    };
  }, [visible]);

  if (!visible) return null;

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
    } catch {
      /* ignore */
    }
  };

  return (
    <div
      className="fixed bottom-0 left-0 right-0 z-[98] flex justify-center px-3 pointer-events-none"
      style={{ paddingBottom: 'max(0.75rem, env(safe-area-inset-bottom))' }}
    >
      <div
        className="pointer-events-auto w-full max-w-md flex items-center gap-3 rounded-[var(--volera-radius-md)] px-3 py-2.5 shadow-lg border border-[var(--volera-border)] bg-[var(--volera-surface)]/95 backdrop-blur-sm text-[var(--volera-text)]"
        role="banner"
      >
        <div className="flex shrink-0 size-9 rounded-lg bg-[var(--volera-accent-soft)] items-center justify-center">
          <Smartphone size={18} className="text-[var(--volera-accent)]" />
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium">Install Volera</p>
          <p className="text-xs text-[var(--volera-text-muted)]">
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
              className="px-3 py-2 min-h-[40px] rounded-lg text-sm font-medium bg-[var(--volera-accent)] hover:bg-[var(--volera-accent-hover)] disabled:opacity-70 text-white transition-colors"
            >
              {installing ? '…' : 'Install'}
            </button>
          )}
          <button
            type="button"
            onClick={handleDismiss}
            className="p-2 min-h-[44px] min-w-[44px] flex items-center justify-center rounded-lg text-[var(--volera-text-muted)] hover:text-[var(--volera-text)] hover:bg-[var(--volera-surface-muted)] transition-colors"
            aria-label="Dismiss"
          >
            <X size={16} />
          </button>
        </div>
      </div>
    </div>
  );
}
