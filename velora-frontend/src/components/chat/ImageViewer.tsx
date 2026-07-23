import React, { useCallback, useEffect, useRef, useState } from 'react';
import { X, Download, Maximize2, Minimize2 } from 'lucide-react';

export interface ImageViewerProps {
  isOpen: boolean;
  src: string | null;
  alt?: string;
  /** Suggested filename for download (e.g. "photo.jpg") */
  downloadFilename?: string;
  onClose: () => void;
  /** Called when user clicks Download. If not provided, a simple link download is used. */
  onDownload?: () => void | Promise<void>;
}

/**
 * Full-screen image viewer for chat images: maximize view, download, responsive (mobile + desktop).
 */
export const ImageViewer: React.FC<ImageViewerProps> = ({
  isOpen,
  src,
  alt = 'Attachment',
  downloadFilename = 'image',
  onClose,
  onDownload,
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const imageRef = useRef<HTMLImageElement>(null);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [isFitMode, setIsFitMode] = useState(true);
  const [downloading, setDownloading] = useState(false);

  const handleEscape = useCallback(
    (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        if (isFullscreen && document.fullscreenElement) {
          document.exitFullscreen().catch(() => {});
          setIsFullscreen(false);
        } else {
          onClose();
        }
      }
    },
    [onClose, isFullscreen]
  );

  useEffect(() => {
    if (!isOpen) return;
    document.addEventListener('keydown', handleEscape);
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', handleEscape);
      document.body.style.overflow = '';
      if (document.fullscreenElement) {
        document.exitFullscreen().catch(() => {});
      }
    };
  }, [isOpen, handleEscape]);

  const toggleFullscreen = useCallback(async () => {
    if (!containerRef.current) return;
    try {
      if (document.fullscreenElement) {
        await document.exitFullscreen();
        setIsFullscreen(false);
      } else {
        await containerRef.current.requestFullscreen();
        setIsFullscreen(true);
      }
    } catch {
      // Fallback: just close or ignore
    }
  }, []);

  const handleDownloadClick = useCallback(async () => {
    if (onDownload) {
      setDownloading(true);
      try {
        await onDownload();
      } finally {
        setDownloading(false);
      }
      return;
    }
    if (!src) return;
    setDownloading(true);
    try {
      const link = document.createElement('a');
      link.href = src;
      link.download = downloadFilename;
      link.rel = 'noopener noreferrer';
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch {
      window.open(src, '_blank');
    } finally {
      setDownloading(false);
    }
  }, [src, downloadFilename, onDownload]);

  const handleBackdropClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) onClose();
  };

  if (!isOpen) return null;

  return (
    <div
      ref={containerRef}
      className="fixed inset-0 z-[60] flex flex-col bg-black/95 backdrop-blur-sm animate-in fade-in duration-200 pt-[env(safe-area-inset-top,0px)] pb-[env(safe-area-inset-bottom,0px)]"
      role="dialog"
      aria-modal="true"
      aria-label="Image viewer"
    >
      {/* Top bar: close + actions */}
      <div className="absolute top-0 left-0 right-0 z-10 flex items-center justify-between p-3 sm:p-4 gap-2 min-h-[56px] pt-[max(0.75rem,env(safe-area-inset-top,0px))] bg-gradient-to-b from-black/60 to-transparent">
        <button
          type="button"
          onClick={onClose}
          className="p-2.5 rounded-full bg-white/10 hover:bg-white/20 text-white transition-colors touch-manipulation"
          aria-label="Close"
        >
          <X size={24} className="sm:w-6 sm:h-6" />
        </button>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={toggleFullscreen}
            className="p-2.5 rounded-full bg-white/10 hover:bg-white/20 text-white transition-colors touch-manipulation hidden sm:flex"
            aria-label={isFullscreen ? 'Exit fullscreen' : 'Fullscreen'}
          >
            {isFullscreen ? <Minimize2 size={22} /> : <Maximize2 size={22} />}
          </button>
          <button
            type="button"
            onClick={handleDownloadClick}
            disabled={downloading}
            className="flex items-center gap-2 px-3 py-2.5 sm:px-4 sm:py-2.5 rounded-full bg-white/10 hover:bg-white/20 text-white transition-colors touch-manipulation disabled:opacity-60"
            aria-label="Download"
          >
            <Download size={20} className="sm:w-5 sm:h-5" />
            <span className="text-sm font-medium hidden sm:inline">Download</span>
          </button>
        </div>
      </div>

      {/* Fit / 1:1 toggle - desktop only, subtle */}
      <div className="absolute top-16 left-1/2 -translate-x-1/2 z-10 hidden sm:block">
        <button
          type="button"
          onClick={() => setIsFitMode((v) => !v)}
          className="px-3 py-1.5 rounded-full bg-white/10 hover:bg-white/20 text-white/90 text-xs font-medium transition-colors"
        >
          {isFitMode ? 'Fit' : '1:1'}
        </button>
      </div>

      {/* Image area - click backdrop to close */}
      <div
        className="flex-1 flex items-center justify-center min-h-0 p-4 pt-16 sm:pt-20 pb-8 sm:pb-10 overflow-auto"
        onClick={handleBackdropClick}
      >
        {src && (
          <img
            ref={imageRef}
            src={src}
            alt={alt}
            className={`select-none ${
              isFitMode
                ? 'object-contain w-auto h-auto max-w-full'
                : 'object-none w-auto h-auto'
            }`}
            style={
              isFitMode
                ? { maxHeight: 'calc(100vh - 8rem)' }
                : undefined
            }
            onClick={(e) => e.stopPropagation()}
            draggable={false}
          />
        )}
      </div>
    </div>
  );
};
