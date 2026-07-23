import { WifiOff } from 'lucide-react';
import { useOnlineStatus } from '../hooks/useOnlineStatus';

export function OfflineBanner() {
  const isOnline = useOnlineStatus();

  if (isOnline) return null;

  return (
    <div className="w-full bg-amber-600 text-white px-4 py-2.5 flex items-center justify-center gap-2 shadow-sm">
      <WifiOff size={18} className="shrink-0" />
      <span className="text-sm font-medium">You&apos;re offline. Messages will queue until you reconnect.</span>
    </div>
  );
}
