import React, { useEffect } from 'react';
import { Plus } from 'lucide-react';
import { clsx } from 'clsx';
import { useStoryStore } from '../store/useStoryStore';
import { getInitials } from '../utils/getInitials';
import { StoryComposer } from './StoryComposer';
import { StoryViewer } from './StoryViewer';

export const StoriesStrip: React.FC = () => {
  const { rings, fetchFeed, openViewer, openComposer, composerOpen, viewerUserId } = useStoryStore();

  useEffect(() => {
    void fetchFeed();
  }, [fetchFeed]);

  return (
    <>
      <div className="shrink-0 border-b border-[var(--volera-border)] bg-[var(--volera-surface)] px-3 py-2.5">
        <div className="flex gap-3 overflow-x-auto overflow-y-hidden pb-1 message-input-scrollbar">
          {rings.map((ring) => (
            <div
              key={ring.userId}
              className="relative flex flex-col items-center gap-1 min-w-[64px] max-w-[72px] shrink-0"
            >
              <button
                type="button"
                onClick={() => {
                  if (ring.isOwn && ring.stories.length === 0) {
                    openComposer();
                    return;
                  }
                  openViewer(ring.userId);
                }}
                className="flex flex-col items-center gap-1 w-full touch-manipulation"
              >
                <div
                  className={clsx(
                    'w-14 h-14 rounded-full p-[2px]',
                    ring.hasUnseen || (ring.isOwn && ring.stories.length > 0)
                      ? 'bg-gradient-to-tr from-[var(--volera-accent)] to-teal-300'
                      : 'bg-[var(--volera-border)]'
                  )}
                >
                  <div className="w-full h-full rounded-full bg-[var(--volera-surface)] p-[2px] overflow-hidden">
                    {ring.profilePicture ? (
                      <img
                        src={ring.profilePicture}
                        alt=""
                        className="w-full h-full rounded-full object-cover"
                      />
                    ) : (
                      <div className="w-full h-full rounded-full bg-[var(--volera-surface-muted)] flex items-center justify-center text-xs font-bold text-[var(--volera-text-muted)]">
                        {getInitials(ring.displayName)}
                      </div>
                    )}
                  </div>
                </div>
                <span className="text-[11px] text-[var(--volera-text-muted)] truncate w-full text-center">
                  {ring.isOwn ? 'Your story' : ring.displayName.split(' ')[0]}
                </span>
              </button>
              {ring.isOwn && (
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    openComposer();
                  }}
                  className="absolute top-9 right-0 w-6 h-6 rounded-full bg-[var(--volera-accent)] text-white flex items-center justify-center shadow border-2 border-[var(--volera-surface)]"
                  aria-label="Add story"
                >
                  <Plus size={12} />
                </button>
              )}
            </div>
          ))}
        </div>
      </div>
      {composerOpen && <StoryComposer />}
      {viewerUserId && <StoryViewer userId={viewerUserId} />}
    </>
  );
};
