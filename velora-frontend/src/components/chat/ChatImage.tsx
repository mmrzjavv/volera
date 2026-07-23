import React, { useState, useEffect, useRef } from 'react';
import axios from 'axios';
import { useFileTransferStore } from '../../store/useFileTransferStore';
import { fileService } from '../../services/api';
import { getCachedImageBlobUrl, setCachedImageBlobUrl } from '../../utils/imageCache';
import { ensureReachableMediaUrl } from '../../utils/ensureReachableMediaUrl';
import { CircularProgress } from '../ui/CircularProgress';
import { Download, XCircle, AlertCircle } from 'lucide-react';

const DISPLAY_LOAD_PREFIX = 'img:';
/** If download doesn't complete or fail within this time, we fail and let user retry */
const LOAD_TIMEOUT_MS = 30_000;
/** Preload images this many pixels before they enter the viewport */
const LAZY_ROOT_MARGIN = '200px';

interface ChatImageProps {
  src: string;
  alt?: string;
  className?: string;
  onClick?: (e: React.MouseEvent) => void;
  showDownloadButton?: boolean;
  onDownloadClick?: (e: React.MouseEvent) => void;
}

/**
 * Loads and displays a chat image with progress (WhatsApp-style).
 * Only downloads when the image is in or near the viewport (lazy load) to avoid heavy loading when opening a chat.
 * Uses cache so scrolling back doesn't re-fetch. Shows placeholder until in view, then progress until loaded.
 */
export const ChatImage: React.FC<ChatImageProps> = ({
  src,
  alt = 'Attachment',
  className = '',
  onClick,
  showDownloadButton = true,
  onDownloadClick,
}) => {
  const [blobUrl, setBlobUrl] = useState<string | null>(() => getCachedImageBlobUrl(src) ?? null);
  const [error, setError] = useState(false);
  const [retryKey, setRetryKey] = useState(0);
  const [inView, setInView] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const cancelTokenRef = useRef<ReturnType<typeof axios.CancelToken.source> | null>(null);

  const {
    addTransfer,
    updateProgress,
    completeTransfer,
    failTransfer,
    getTransfer,
    removeTransfer,
  } = useFileTransferStore();

  const transferId = `${DISPLAY_LOAD_PREFIX}${src}`;
  const transfer = getTransfer(transferId);
  const progress = transfer?.progress ?? 0;

  // Lazy load: only consider "in view" when element is in or near viewport
  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;
    const cached = getCachedImageBlobUrl(src);
    if (cached) {
      setInView(true);
      return;
    }
    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            setInView(true);
            break;
          }
        }
      },
      { rootMargin: LAZY_ROOT_MARGIN, threshold: 0.01 }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, [src]);

  // Download only when in view (and not cached)
  useEffect(() => {
    const cached = getCachedImageBlobUrl(src);
    if (cached) {
      setBlobUrl(cached);
      setError(false);
      return;
    }
    if (!inView) return;

    // Clear any stale stuck transfer from a previous attempt so we don't show old progress
    const existing = getTransfer(transferId);
    if (existing && (existing.status === 'downloading' || existing.status === 'uploading')) {
      removeTransfer(transferId);
    }

    let mounted = true;
    const cancelTokenSource = axios.CancelToken.source();
    cancelTokenRef.current = cancelTokenSource;

    const timeoutId = setTimeout(() => {
      if (!mounted) return;
      cancelTokenSource.cancel('Load timeout');
    }, LOAD_TIMEOUT_MS);

    addTransfer(transferId, 'download', () => cancelTokenSource.cancel());

    const run = async () => {
      let fetchUrl = src;
      try {
        fetchUrl = await ensureReachableMediaUrl(src);
      } catch {
        fetchUrl = src;
      }

      let totalBytes: number | undefined;
      try {
        totalBytes = await fileService.checkFileSize(fetchUrl);
        if (totalBytes === 0) totalBytes = undefined;
      } catch {
        totalBytes = undefined;
      }

      return fileService.downloadFile(
        fetchUrl,
        (p) => {
          if (mounted) updateProgress(transferId, p);
        },
        cancelTokenSource.token,
        totalBytes
      )
      .then((blob) => {
        if (!mounted) return;
        clearTimeout(timeoutId);
        const objectUrl = window.URL.createObjectURL(blob);
        setCachedImageBlobUrl(src, objectUrl);
        setBlobUrl(objectUrl);
        setError(false);
        completeTransfer(transferId);
        setTimeout(() => removeTransfer(transferId), 500);
      })
      .catch((err) => {
        if (!mounted) return;
        clearTimeout(timeoutId);
        if (axios.isCancel(err)) {
          removeTransfer(transferId);
          setError(true); // Show error state so loading overlay doesn't stick (user cancel or timeout)
        } else {
          setError(true);
          failTransfer(transferId, 'Failed to load image');
          setTimeout(() => removeTransfer(transferId), 500);
        }
      });
    };

    run();

    return () => {
      mounted = false;
      clearTimeout(timeoutId);
      cancelTokenSource.cancel();
      removeTransfer(transferId);
      cancelTokenRef.current = null;
    };
  }, [src, retryKey, inView]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleCancelLoad = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (transfer?.cancel) transfer.cancel();
  };

  if (error) {
    return (
      <div ref={containerRef}>
        <button
          type="button"
          onClick={() => {
            removeTransfer(transferId);
            setError(false);
            setRetryKey((k) => k + 1);
          }}
          className={`flex flex-col items-center justify-center rounded-lg bg-gray-200 dark:bg-gray-700 min-h-[120px] w-full cursor-pointer hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors ${className}`}
        >
          <AlertCircle className="text-gray-500 dark:text-gray-400 mb-1" size={28} />
          <span className="text-xs text-gray-500 dark:text-gray-400">Failed to load image</span>
          <span className="text-[10px] text-gray-400 dark:text-gray-500 mt-0.5">Tap to retry</span>
        </button>
      </div>
    );
  }

  if (blobUrl) {
    return (
      <div ref={containerRef} className="relative group/image animate-in fade-in duration-500">
        <img
          src={blobUrl}
          alt={alt}
          className={`rounded-xl max-w-full h-auto max-h-[400px] object-cover cursor-pointer shadow-sm hover:shadow-md transition-shadow ${className}`}
          onClick={onClick}
          loading="lazy"
        />
        {showDownloadButton && onDownloadClick && (
          <button
            type="button"
            onClick={onDownloadClick}
            className="absolute bottom-3 right-3 p-2.5 bg-black/40 hover:bg-black/60 backdrop-blur-md text-white rounded-full opacity-0 group-hover/image:opacity-100 transition-all transform scale-90 group-hover/image:scale-100"
            aria-label="Download"
          >
            <Download size={18} />
          </button>
        )}
      </div>
    );
  }

  // Not in view yet: lightweight placeholder (no download)
  if (!inView) {
    return (
      <div
        ref={containerRef}
        className={`relative rounded-xl w-full max-w-full min-h-[200px] sm:min-w-[240px] sm:min-h-[240px] bg-gray-100 dark:bg-gray-800 flex items-center justify-center overflow-hidden shadow-sm border border-gray-100 dark:border-gray-700 ${className}`}
      >
        <div className="absolute inset-0 bg-gradient-to-tr from-gray-200 via-gray-100 to-gray-200 dark:from-gray-800 dark:via-gray-700 dark:to-gray-800 animate-pulse" />
        <span className="relative z-10 text-xs text-gray-400 dark:text-gray-500">Image</span>
      </div>
    );
  }

  // In view, loading: show progress
  return (
    <div
      ref={containerRef}
      className={`relative rounded-xl w-full max-w-full min-h-[200px] sm:min-w-[240px] sm:min-h-[240px] bg-gray-100 dark:bg-gray-800 flex items-center justify-center overflow-hidden shadow-sm border border-gray-100 dark:border-gray-700 ${className}`}
    >
      <div className="absolute inset-0 bg-gradient-to-tr from-gray-200 via-gray-100 to-gray-200 dark:from-gray-800 dark:via-gray-700 dark:to-gray-800 animate-pulse" />
      <div className="relative z-10 flex flex-col items-center justify-center gap-4 p-6 rounded-3xl bg-white/40 dark:bg-black/40 backdrop-blur-xl border border-white/30 dark:border-white/10 shadow-xl min-w-[140px]">
        <CircularProgress
          progress={progress}
          color="currentColor"
          trackColor="rgba(255,255,255,0.2)"
          size={56}
          className="text-[var(--volera-accent)]"
          strokeWidth={5}
          icon={
            <button
              type="button"
              onClick={handleCancelLoad}
              className="p-1.5 text-gray-700 dark:text-gray-200 hover:text-red-500 dark:hover:text-red-400 transition-colors rounded-full hover:bg-white/20"
              aria-label="Cancel load"
            >
              <XCircle size={24} />
            </button>
          }
        />
        <div className="flex flex-col items-center gap-1">
          <span className="text-sm font-bold tracking-widest text-gray-800 dark:text-gray-100 font-mono whitespace-nowrap">
            {progress > 0 ? `${Math.round(progress)}%` : '0%'}
          </span>
          <span className="text-[10px] uppercase tracking-wider text-gray-500 dark:text-gray-400 font-medium">
            Loading
          </span>
        </div>
      </div>
    </div>
  );
};
