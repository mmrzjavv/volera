import React, { useRef, useState } from 'react';
import { X, ImagePlus } from 'lucide-react';
import { createPortal } from 'react-dom';
import { useStoryStore } from '../store/useStoryStore';
import { fileService } from '../services/api';
import { Button } from './ui/Button';
import { useToastStore } from '../store/useToastStore';
import type { CreateStoryItemPayload } from '../types';

type DraftItem = {
  file: File;
  previewUrl: string;
  mediaType: 'Image' | 'Video';
  overlayText: string;
  overlayColor: string;
};

export const StoryComposer: React.FC = () => {
  const { closeComposer, createStory } = useStoryStore();
  const addToast = useToastStore((s) => s.addToast);
  const inputRef = useRef<HTMLInputElement>(null);
  const [drafts, setDrafts] = useState<DraftItem[]>([]);
  const [activeIdx, setActiveIdx] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const active = drafts[activeIdx];

  const onFiles = (files: FileList | null) => {
    if (!files?.length) return;
    const next: DraftItem[] = [];
    Array.from(files).slice(0, 10 - drafts.length).forEach((file) => {
      const isVideo = file.type.startsWith('video/');
      const isImage = file.type.startsWith('image/');
      if (!isVideo && !isImage) return;
      next.push({
        file,
        previewUrl: URL.createObjectURL(file),
        mediaType: isVideo ? 'Video' : 'Image',
        overlayText: '',
        overlayColor: '#ffffff',
      });
    });
    setDrafts((prev) => {
      const merged = [...prev, ...next].slice(0, 10);
      setActiveIdx(merged.length - 1);
      return merged;
    });
  };

  const updateActive = (patch: Partial<DraftItem>) => {
    setDrafts((prev) => prev.map((d, i) => (i === activeIdx ? { ...d, ...patch } : d)));
  };

  const handlePublish = async () => {
    if (drafts.length === 0 || !navigator.onLine) {
      addToast(navigator.onLine ? 'Add at least one photo or video' : 'You are offline', 'error');
      return;
    }
    setIsSubmitting(true);
    try {
      const items: CreateStoryItemPayload[] = [];
      for (const draft of drafts) {
        const uploaded = await fileService.upload(draft.file);
        const overlay =
          draft.overlayText.trim().length > 0
            ? JSON.stringify({
                text: draft.overlayText.trim().slice(0, 120),
                color: draft.overlayColor,
                x: 0.5,
                y: 0.7,
                fontScale: 1,
              })
            : undefined;
        items.push({
          objectKey: uploaded.objectKey || uploaded.attachmentRef,
          mediaType: draft.mediaType,
          durationMs: draft.mediaType === 'Video' ? 10000 : 5000,
          textOverlayJson: overlay,
        });
      }
      await createStory(items);
      addToast('Story posted', 'success');
    } catch (err: any) {
      addToast(err?.response?.data?.message || 'Failed to post story', 'error');
    } finally {
      setIsSubmitting(false);
    }
  };

  return createPortal(
    <div className="fixed inset-0 z-[60] bg-black/90 flex flex-col text-white">
      <div className="flex items-center justify-between p-3 shrink-0">
        <button
          type="button"
          onClick={closeComposer}
          disabled={isSubmitting}
          className="p-2 min-h-[44px] min-w-[44px] flex items-center justify-center"
          aria-label="Close"
        >
          <X size={22} />
        </button>
        <span className="font-semibold">New story</span>
        <Button
          size="sm"
          onClick={handlePublish}
          isLoading={isSubmitting}
          disabled={drafts.length === 0}
          className="min-h-[40px]"
        >
          Share
        </Button>
      </div>

      <div className="flex-1 min-h-0 relative flex items-center justify-center bg-black">
        {active ? (
          <>
            {active.mediaType === 'Video' ? (
              <video src={active.previewUrl} className="max-h-full max-w-full object-contain" controls />
            ) : (
              <img src={active.previewUrl} alt="" className="max-h-full max-w-full object-contain" />
            )}
            {active.overlayText && (
              <div
                className="absolute left-1/2 top-[70%] -translate-x-1/2 -translate-y-1/2 px-3 py-1 text-center text-lg font-semibold drop-shadow-lg max-w-[80%] break-words"
                style={{ color: active.overlayColor }}
              >
                {active.overlayText}
              </div>
            )}
          </>
        ) : (
          <button
            type="button"
            onClick={() => inputRef.current?.click()}
            className="flex flex-col items-center gap-2 text-white/80 hover:text-white"
          >
            <ImagePlus size={40} />
            <span className="text-sm">Add photos or videos</span>
          </button>
        )}
      </div>

      {active && (
        <div className="shrink-0 p-3 space-y-2 bg-black/80 border-t border-white/10">
          <input
            type="text"
            value={active.overlayText}
            onChange={(e) => updateActive({ overlayText: e.target.value })}
            placeholder="Add text overlay…"
            maxLength={120}
            className="w-full min-h-[44px] px-3 rounded-lg bg-white/10 border border-white/20 text-white placeholder:text-white/50 focus:outline-none focus:ring-2 focus:ring-[var(--volera-accent)]"
          />
          <div className="flex items-center gap-2">
            {['#ffffff', '#fef08a', '#fda4af', '#67e8f9'].map((c) => (
              <button
                key={c}
                type="button"
                onClick={() => updateActive({ overlayColor: c })}
                className="w-8 h-8 rounded-full border-2 border-white/40"
                style={{ backgroundColor: c }}
                aria-label={`Color ${c}`}
              />
            ))}
          </div>
        </div>
      )}

      <div className="shrink-0 flex gap-2 p-3 overflow-x-auto border-t border-white/10">
        {drafts.map((d, i) => (
          <button
            key={d.previewUrl}
            type="button"
            onClick={() => setActiveIdx(i)}
            className={`w-14 h-14 rounded-lg overflow-hidden shrink-0 border-2 ${
              i === activeIdx ? 'border-[var(--volera-accent)]' : 'border-transparent'
            }`}
          >
            {d.mediaType === 'Video' ? (
              <video src={d.previewUrl} className="w-full h-full object-cover" muted />
            ) : (
              <img src={d.previewUrl} alt="" className="w-full h-full object-cover" />
            )}
          </button>
        ))}
        {drafts.length < 10 && (
          <button
            type="button"
            onClick={() => inputRef.current?.click()}
            className="w-14 h-14 rounded-lg border border-dashed border-white/40 flex items-center justify-center shrink-0"
            aria-label="Add more"
          >
            <ImagePlus size={20} />
          </button>
        )}
      </div>

      <input
        ref={inputRef}
        type="file"
        accept="image/*,video/*"
        multiple
        className="hidden"
        onChange={(e) => {
          onFiles(e.target.files);
          e.target.value = '';
        }}
      />
    </div>,
    document.body
  );
};
