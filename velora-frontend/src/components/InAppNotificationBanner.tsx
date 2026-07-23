import { useCallback } from 'react';
import { X } from 'lucide-react';
import { useInAppNotificationStore, type InAppNotification } from '../store/useInAppNotificationStore';
import { useChatStore } from '../store/useChatStore';
import { useCallStore } from '../store/useCallStore';
import { getInitials } from '../utils/getInitials';
import { clsx } from 'clsx';

function NotificationCard({
  item,
  onDismiss,
  onClick,
}: {
  item: InAppNotification;
  onDismiss: () => void;
  onClick: () => void;
}) {
  const isCall =
    item.type === 'call_initiated' || item.type === 'group_call_initiated';
  const displayName = item.type === 'group_message' ? item.groupName : item.senderName;
  const initials = getInitials(displayName ?? (isCall ? item.callerName : undefined));

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={(e) => {
        e.stopPropagation();
        onClick();
      }}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onClick();
        }
      }}
      className={clsx(
        'flex items-start gap-3 p-3 rounded-xl shadow-lg border cursor-pointer',
        'min-w-[280px] max-w-[360px] w-full',
        'bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700',
        'hover:bg-gray-50 dark:hover:bg-gray-700/80 transition-colors',
        'text-left'
      )}
    >
      <div
        className={clsx(
          'shrink-0 w-10 h-10 rounded-full flex items-center justify-center text-sm font-medium',
          isCall
            ? 'bg-amber-500/20 text-amber-600 dark:text-amber-400'
            : 'bg-[var(--volera-accent-soft)] text-[var(--volera-accent)]'
        )}
      >
        {initials}
      </div>
      <div className="flex-1 min-w-0">
        <p className="font-semibold text-gray-900 dark:text-gray-100 truncate">
          {item.title}
        </p>
        <p className="text-sm text-gray-600 dark:text-gray-400 line-clamp-2 mt-0.5">
          {item.body}
        </p>
      </div>
      <button
        type="button"
        onClick={(e) => {
          e.stopPropagation();
          onDismiss();
        }}
        className="shrink-0 p-1 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-200 dark:hover:text-gray-300 dark:hover:bg-gray-700 transition-colors"
        aria-label="Dismiss"
      >
        <X size={18} />
      </button>
    </div>
  );
}

/**
 * Renders in-app notification banners when the app is open (instead of OS notifications).
 * Shown at top of viewport; click opens the relevant chat or call UI.
 */
export function InAppNotificationBanner() {
  const { items, remove } = useInAppNotificationStore();
  const { selectUserById, selectGroupById } = useChatStore();
  const { setIncomingFromNotification } = useCallStore();

  const handleClick = useCallback(
    (item: InAppNotification) => {
      remove(item.id);
      if (item.type === 'call_initiated' || item.type === 'group_call_initiated') {
        setIncomingFromNotification({
          callId: item.callId ?? '',
          callerId: item.callerId ?? '',
          callerName: item.callerName ?? '',
          receiverId: item.receiverId ?? '',
          isVideo: item.isVideo ?? false,
        });
        return;
      }
      if (item.type === 'message' && item.senderId) {
        selectUserById(item.senderId);
        return;
      }
      if (item.type === 'group_message' && item.groupId) {
        selectGroupById(item.groupId);
      }
    },
    [remove, setIncomingFromNotification, selectUserById, selectGroupById]
  );

  if (items.length === 0) return null;

  return (
    <div className="fixed top-[max(1rem,calc(env(safe-area-inset-top)+0.75rem))] left-1/2 -translate-x-1/2 z-[200] flex flex-col gap-2 pointer-events-none w-full max-w-[360px] px-4">
      <div className="pointer-events-auto flex flex-col gap-2">
        {items.map((item) => (
          <NotificationCard
            key={item.id}
            item={item}
            onDismiss={() => remove(item.id)}
            onClick={() => handleClick(item)}
          />
        ))}
      </div>
    </div>
  );
}
