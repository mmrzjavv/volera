import React, { useEffect } from 'react';
import { useChatStore } from '../store/useChatStore';
import { Users, Plus, Megaphone } from 'lucide-react';
import { clsx } from 'clsx';

interface GroupListProps {
  onCreateGroup: () => void;
  onCreateChannel?: () => void;
}

export const GroupList: React.FC<GroupListProps> = ({ onCreateGroup, onCreateChannel }) => {
  const { groups, channels, fetchGroups, fetchChannels, selectGroup, selectedGroup } = useChatStore();

  useEffect(() => {
    fetchGroups();
    fetchChannels();
  }, [fetchGroups, fetchChannels]);

  const regularGroups = groups.filter((g) => !g.isChannel);

  return (
    <div className="flex-1 flex flex-col overflow-hidden">
      <div className="p-4 border-b border-gray-300 dark:border-gray-700 flex justify-between items-center bg-gray-100 dark:bg-gray-900 gap-2">
        <h2 className="font-bold text-gray-700 dark:text-gray-200">Groups & Channels</h2>
        <div className="flex items-center gap-1">
          {onCreateChannel && (
            <button
              type="button"
              onClick={onCreateChannel}
              className="p-2 text-[var(--volera-accent)] hover:bg-[var(--volera-accent)]/10 rounded-full transition-colors"
              title="Create Channel"
            >
              <Megaphone size={20} />
            </button>
          )}
          <button
            type="button"
            onClick={onCreateGroup}
            className="p-2 text-[var(--volera-accent)] hover:bg-[var(--volera-accent)]/10 rounded-full transition-colors"
            title="Create Group"
          >
            <Plus size={20} />
          </button>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto">
        {channels.length > 0 && (
          <div className="px-4 pt-3 pb-1 text-xs font-semibold uppercase tracking-wide text-gray-500">Channels</div>
        )}
        {channels.map((channel) => (
          <div
            key={channel.id}
            onClick={() => selectGroup(channel)}
            className={clsx(
              'p-4 border-b border-gray-100 dark:border-gray-700 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors flex items-center gap-3',
              selectedGroup?.id === channel.id && 'bg-[var(--volera-accent)]/10 border-l-4 border-l-[var(--volera-accent)]'
            )}
          >
            <div className="w-10 h-10 rounded-full bg-[var(--volera-accent)]/15 flex items-center justify-center text-[var(--volera-accent)] overflow-hidden font-bold">
              {channel.profilePictureUrl ? (
                <img src={channel.profilePictureUrl} alt="" className="w-full h-full object-cover" />
              ) : (
                <Megaphone size={18} />
              )}
            </div>
            <div className="flex-1 min-w-0">
              <div className="font-medium text-gray-900 dark:text-white truncate">{channel.name}</div>
              <div className="text-xs text-gray-500 dark:text-gray-400 truncate">
                {channel.publicUsername ? `@${channel.publicUsername}` : 'Channel'}
              </div>
            </div>
          </div>
        ))}

        {regularGroups.length > 0 && (
          <div className="px-4 pt-3 pb-1 text-xs font-semibold uppercase tracking-wide text-gray-500">Groups</div>
        )}
        {regularGroups.length === 0 && channels.length === 0 ? (
          <div className="p-8 text-center text-gray-400 dark:text-gray-500 flex flex-col items-center">
            <Users size={48} className="mb-2 opacity-20" />
            <p>No groups or channels yet.</p>
            <div className="flex flex-col gap-1 mt-2">
              <button type="button" onClick={onCreateGroup} className="text-[var(--volera-accent)] hover:underline">
                Create a group
              </button>
              {onCreateChannel && (
                <button type="button" onClick={onCreateChannel} className="text-[var(--volera-accent)] hover:underline">
                  Create a channel
                </button>
              )}
            </div>
          </div>
        ) : (
          regularGroups.map((group) => (
            <div
              key={group.id}
              onClick={() => selectGroup(group)}
              className={clsx(
                'p-4 border-b border-gray-100 dark:border-gray-700 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors flex items-center gap-3',
                selectedGroup?.id === group.id && 'bg-[var(--volera-accent)]/10 border-l-4 border-l-[var(--volera-accent)]'
              )}
            >
              <div className="w-10 h-10 rounded-full bg-[var(--volera-accent)]/15 flex items-center justify-center text-[var(--volera-accent)] overflow-hidden font-bold">
                {group.name[0].toUpperCase()}
              </div>
              <div className="flex-1 min-w-0">
                <div className="font-medium text-gray-900 dark:text-white truncate">{group.name}</div>
                <div className="text-xs text-gray-500 dark:text-gray-400 truncate">Group</div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
