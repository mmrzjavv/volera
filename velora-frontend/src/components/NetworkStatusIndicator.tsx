import { useOnlineStatus } from '../hooks/useOnlineStatus';
import { clsx } from 'clsx';

/**
 * Minimal network status indicator that updates in real-time via browser
 * online/offline events (like WhatsApp/Telegram), not per-request.
 */
export function NetworkStatusIndicator() {
  const isOnline = useOnlineStatus();

  return (
    <div
      className={clsx(
        "flex items-center gap-1.5 px-1.5 sm:px-2 py-1 rounded-md text-xs font-medium transition-colors",
        isOnline
          ? "text-emerald-600 dark:text-emerald-400 bg-emerald-50/80 dark:bg-emerald-900/20"
          : "text-amber-600 dark:text-amber-400 bg-amber-50/80 dark:bg-amber-900/20"
      )}
      title={isOnline ? 'Online' : 'Offline'}
      role="status"
      aria-live="polite"
    >
      <span
        className={clsx(
          "w-1.5 h-1.5 rounded-full shrink-0",
          isOnline ? "bg-emerald-500 dark:bg-emerald-400" : "bg-amber-500 dark:bg-amber-400"
        )}
      />
      <span className="truncate hidden sm:inline">{isOnline ? 'Online' : 'Offline'}</span>
    </div>
  );
}
