import React, { useEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { X, Eye, Trash2, Send } from 'lucide-react';
import { useStoryStore } from '../store/useStoryStore';
import { storyService } from '../services/api';
import { useAuthStore } from '../store/useAuthStore';
import { useToastStore } from '../store/useToastStore';
import type { StoryItem, StoryViewer as StoryViewerDto } from '../types';
import { getInitials } from '../utils/getInitials';

function parseOverlay(json?: string | null): { text: string; color: string; x: number; y: number } | null {
  if (!json) return null;
  try {
    const o = JSON.parse(json);
    return {
      text: String(o.text || ''),
      color: String(o.color || '#ffffff'),
      x: typeof o.x === 'number' ? o.x : 0.5,
      y: typeof o.y === 'number' ? o.y : 0.7,
    };
  } catch {
    return null;
  }
}

interface StoryViewerProps {
  userId: string;
}

export const StoryViewer: React.FC<StoryViewerProps> = ({ userId }) => {
  const { rings, closeViewer, markViewed, deleteStory, replyToItem, fetchFeed } = useStoryStore();
  const { user } = useAuthStore();
  const addToast = useToastStore((s) => s.addToast);
  const ring = rings.find((r) => r.userId === userId);
  const flatItems = useMemo(() => {
    const items: { storyId: string; item: StoryItem }[] = [];
    ring?.stories.forEach((s) => {
      s.items
        .slice()
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .forEach((item) => items.push({ storyId: s.storyId, item }));
    });
    return items;
  }, [ring]);

  const [index, setIndex] = useState(0);
  const [paused, setPaused] = useState(false);
  const [reply, setReply] = useState('');
  const [sending, setSending] = useState(false);
  const [showViewers, setShowViewers] = useState(false);
  const [viewers, setViewers] = useState<StoryViewerDto[]>([]);
  const progressRef = useRef(0);
  const rafRef = useRef<number | null>(null);
  const lastTsRef = useRef<number | null>(null);
  const [progress, setProgress] = useState(0);

  const current = flatItems[index];
  const isOwn = ring?.isOwn || user?.id === userId;

  useEffect(() => {
    if (!current) return;
    void markViewed(current.storyId);
  }, [current?.storyId, markViewed]);

  useEffect(() => {
    if (!current || paused) {
      lastTsRef.current = null;
      return;
    }
    const duration = Math.max(1000, current.item.durationMs || 5000);
    const tick = (ts: number) => {
      if (lastTsRef.current == null) lastTsRef.current = ts;
      const delta = ts - lastTsRef.current;
      lastTsRef.current = ts;
      progressRef.current += delta / duration;
      if (progressRef.current >= 1) {
        progressRef.current = 0;
        setProgress(0);
        setIndex((i) => {
          if (i >= flatItems.length - 1) {
            closeViewer();
            return i;
          }
          return i + 1;
        });
        return;
      }
      setProgress(progressRef.current);
      rafRef.current = requestAnimationFrame(tick);
    };
    rafRef.current = requestAnimationFrame(tick);
    return () => {
      if (rafRef.current) cancelAnimationFrame(rafRef.current);
    };
  }, [current, paused, index, flatItems.length, closeViewer]);

  useEffect(() => {
    progressRef.current = 0;
    setProgress(0);
    lastTsRef.current = null;
  }, [index]);

  if (!ring || flatItems.length === 0) {
    return createPortal(
      <div className="fixed inset-0 z-[60] bg-black/90 flex items-center justify-center text-white">
        <div className="text-center space-y-3">
          <p>No stories to show</p>
          <button type="button" onClick={closeViewer} className="underline">
            Close
          </button>
        </div>
      </div>,
      document.body
    );
  }

  const overlay = parseOverlay(current.item.textOverlayJson);
  const storyCount = ring.stories.length;
  const storyIndex = ring.stories.findIndex((s) => s.storyId === current.storyId);

  const goNext = () => {
    if (index >= flatItems.length - 1) closeViewer();
    else setIndex((i) => i + 1);
  };
  const goPrev = () => {
    if (index <= 0) return;
    setIndex((i) => i - 1);
  };

  const handleReply = async () => {
    if (!reply.trim() || isOwn) return;
    setSending(true);
    try {
      await replyToItem(current.item.id, reply.trim());
      setReply('');
      addToast('Reply sent', 'success');
    } catch {
      addToast('Failed to send reply', 'error');
    } finally {
      setSending(false);
    }
  };

  const loadViewers = async () => {
    try {
      const list = await storyService.getViewers(current.storyId);
      setViewers(list);
      setShowViewers(true);
    } catch {
      addToast('Could not load viewers', 'error');
    }
  };

  const handleDelete = async () => {
    try {
      await deleteStory(current.storyId);
      addToast('Story deleted', 'success');
      closeViewer();
      void fetchFeed();
    } catch {
      addToast('Failed to delete', 'error');
    }
  };

  return createPortal(
    <div
      className="fixed inset-0 z-[60] bg-black flex flex-col text-white select-none"
      onPointerDown={() => setPaused(true)}
      onPointerUp={() => setPaused(false)}
      onPointerLeave={() => setPaused(false)}
    >
      <div className="absolute top-0 inset-x-0 z-10 flex gap-1 p-2 pt-[max(0.5rem,env(safe-area-inset-top))]">
        {flatItems.map((fi, i) => (
          <div key={fi.item.id} className="flex-1 h-0.5 rounded-full bg-white/30 overflow-hidden">
            <div
              className="h-full bg-white transition-[width] duration-75"
              style={{
                width: i < index ? '100%' : i === index ? `${progress * 100}%` : '0%',
              }}
            />
          </div>
        ))}
      </div>

      <div className="absolute top-4 inset-x-0 z-10 flex items-center justify-between px-3 pt-2">
        <div className="flex items-center gap-2 min-w-0">
          <div className="w-8 h-8 rounded-full bg-white/20 overflow-hidden shrink-0">
            {ring.profilePicture ? (
              <img src={ring.profilePicture} alt="" className="w-full h-full object-cover" />
            ) : (
              <div className="w-full h-full flex items-center justify-center text-[10px] font-bold">
                {getInitials(ring.displayName)}
              </div>
            )}
          </div>
          <div className="min-w-0">
            <div className="text-sm font-semibold truncate">{ring.displayName}</div>
            <div className="text-[10px] text-white/70">
              {storyIndex + 1}/{storyCount}
            </div>
          </div>
        </div>
        <div className="flex items-center gap-1">
          {isOwn && (
            <>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  void loadViewers();
                }}
                className="p-2 min-h-[44px] min-w-[44px] flex items-center justify-center"
                aria-label="Viewers"
              >
                <Eye size={20} />
              </button>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  void handleDelete();
                }}
                className="p-2 min-h-[44px] min-w-[44px] flex items-center justify-center"
                aria-label="Delete"
              >
                <Trash2 size={20} />
              </button>
            </>
          )}
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              closeViewer();
            }}
            className="p-2 min-h-[44px] min-w-[44px] flex items-center justify-center"
            aria-label="Close"
          >
            <X size={22} />
          </button>
        </div>
      </div>

      <div className="flex-1 relative flex items-center justify-center">
        <button
          type="button"
          className="absolute inset-y-0 left-0 w-1/3 z-[5]"
          onClick={(e) => {
            e.stopPropagation();
            goPrev();
          }}
          aria-label="Previous"
        />
        <button
          type="button"
          className="absolute inset-y-0 right-0 w-1/3 z-[5]"
          onClick={(e) => {
            e.stopPropagation();
            goNext();
          }}
          aria-label="Next"
        />
        {current.item.mediaType === 'Video' ? (
          <video
            key={current.item.id}
            src={current.item.mediaUrl}
            className="max-h-full max-w-full object-contain"
            autoPlay
            playsInline
            muted={false}
          />
        ) : (
          <img
            key={current.item.id}
            src={current.item.mediaUrl}
            alt=""
            className="max-h-full max-w-full object-contain"
          />
        )}
        {overlay?.text && (
          <div
            className="absolute px-3 py-1 text-center text-lg font-semibold drop-shadow-lg max-w-[80%] break-words pointer-events-none"
            style={{
              color: overlay.color,
              left: `${overlay.x * 100}%`,
              top: `${overlay.y * 100}%`,
              transform: 'translate(-50%, -50%)',
            }}
          >
            {overlay.text}
          </div>
        )}
      </div>

      {!isOwn && (
        <div
          className="shrink-0 flex gap-2 p-3 pb-[max(0.75rem,env(safe-area-inset-bottom))] bg-black/60"
          onPointerDown={(e) => e.stopPropagation()}
        >
          <input
            value={reply}
            onChange={(e) => setReply(e.target.value)}
            placeholder="Reply…"
            className="flex-1 min-h-[44px] px-3 rounded-full bg-white/10 border border-white/20 text-white placeholder:text-white/50 focus:outline-none"
          />
          <button
            type="button"
            disabled={sending || !reply.trim()}
            onClick={() => void handleReply()}
            className="p-3 min-h-[44px] min-w-[44px] rounded-full bg-[var(--volera-accent)] disabled:opacity-50 flex items-center justify-center"
            aria-label="Send reply"
          >
            <Send size={18} />
          </button>
        </div>
      )}

      {showViewers && (
        <div
          className="absolute inset-0 z-20 bg-black/70 flex items-end"
          onClick={() => setShowViewers(false)}
        >
          <div
            className="w-full max-h-[50vh] overflow-y-auto rounded-t-2xl bg-[var(--volera-surface)] text-[var(--volera-text)] p-4"
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className="font-semibold mb-3">Viewers</h3>
            {viewers.length === 0 ? (
              <p className="text-sm text-[var(--volera-text-muted)]">No views yet</p>
            ) : (
              <ul className="space-y-2">
                {viewers.map((v) => (
                  <li key={v.userId} className="flex items-center gap-3">
                    <div className="w-9 h-9 rounded-full bg-[var(--volera-surface-muted)] overflow-hidden">
                      {v.profilePicture ? (
                        <img src={v.profilePicture} alt="" className="w-full h-full object-cover" />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center text-xs font-bold">
                          {getInitials(v.displayName)}
                        </div>
                      )}
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="text-sm font-medium truncate">{v.displayName}</div>
                      <div className="text-xs text-[var(--volera-text-muted)]">
                        {new Date(v.viewedAt).toLocaleString()}
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}
    </div>,
    document.body
  );
};
