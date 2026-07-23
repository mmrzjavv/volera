import { useOnlineStatus } from '../hooks/useOnlineStatus';
import { WifiOff } from 'lucide-react';

export function OfflineBanner() {
  const isOnline = useOnlineStatus();

  if (isOnline) return null;

  return (
    <div className="fixed top-0 left-0 right-0 z-[99] bg-amber-600 text-white px-4 py-2 flex items-center justify-center gap-2 shadow-md">
      <WifiOff size={18} className="shrink-0" />
      <span className="text-sm font-medium">You're offline. Some features may not work.</span>
    </div>
  );
}
