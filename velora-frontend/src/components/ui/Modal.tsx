import React, { useEffect, useId, useRef } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';
import { twMerge } from 'tailwind-merge';

export interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: React.ReactNode;
  children: React.ReactNode;
  footer?: React.ReactNode;
  /** Max width Tailwind class, default max-w-md */
  maxWidth?: string;
  className?: string;
  /** Disable close via Escape / overlay while busy */
  closeDisabled?: boolean;
  /** Hide the default header close button */
  hideCloseButton?: boolean;
  headerClassName?: string;
  bodyClassName?: string;
  footerClassName?: string;
  /** Stretch nearly full viewport height on small screens (lists / long forms) */
  tallOnMobile?: boolean;
}

export function Modal({
  isOpen,
  onClose,
  title,
  children,
  footer,
  maxWidth = 'max-w-md',
  className,
  closeDisabled = false,
  hideCloseButton = false,
  headerClassName,
  bodyClassName,
  footerClassName,
  tallOnMobile = false,
}: ModalProps) {
  const overlayRef = useRef<HTMLDivElement>(null);
  const titleId = useId();

  useEffect(() => {
    if (!isOpen) return;
    const prev = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = prev;
    };
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !closeDisabled) onClose();
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [isOpen, closeDisabled, onClose]);

  if (!isOpen) return null;

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (e.target === overlayRef.current && !closeDisabled) onClose();
  };

  return createPortal(
    <div
      ref={overlayRef}
      className={twMerge(
        'fixed inset-0 z-[var(--volera-z-modal)] flex items-end sm:items-center justify-center',
        'bg-black/50 backdrop-blur-sm',
        'pt-[env(safe-area-inset-top,0px)] pl-[env(safe-area-inset-left,0px)] pr-[env(safe-area-inset-right,0px)]',
        'pb-[env(safe-area-inset-bottom,0px)] sm:p-4 sm:pt-4 sm:pb-4'
      )}
      onClick={handleOverlayClick}
      role="dialog"
      aria-modal="true"
      aria-labelledby={title ? titleId : undefined}
    >
      <div
        className={twMerge(
          'w-full flex flex-col overflow-hidden',
          'rounded-t-[var(--volera-radius-lg)] sm:rounded-[var(--volera-radius-md)]',
          'shadow-xl border border-[var(--volera-border)] bg-[var(--volera-surface)] text-[var(--volera-text)]',
          'volera-fade-up',
          'max-h-[min(92dvh,100%)] sm:max-h-[min(90vh,56rem)]',
          tallOnMobile && 'h-[min(92dvh,100%)] sm:h-auto',
          maxWidth,
          className
        )}
      >
        {/* Mobile sheet grab affordance */}
        <div className="sm:hidden flex justify-center pt-2 pb-0 shrink-0" aria-hidden>
          <div className="w-10 h-1 rounded-full bg-[var(--volera-border)]" />
        </div>

        {(title || !hideCloseButton) && (
          <div
            className={twMerge(
              'shrink-0 flex items-center justify-between gap-3 px-4 py-3 border-b border-[var(--volera-border)] bg-[var(--volera-surface-muted)]',
              headerClassName
            )}
          >
            <div id={titleId} className="min-w-0 flex-1 font-semibold text-base sm:text-lg truncate">
              {title}
            </div>
            {!hideCloseButton && (
              <button
                type="button"
                onClick={() => !closeDisabled && onClose()}
                disabled={closeDisabled}
                className="shrink-0 p-2 min-h-[44px] min-w-[44px] flex items-center justify-center rounded-full text-[var(--volera-text-muted)] hover:text-[var(--volera-text)] hover:bg-[var(--volera-border)]/40 transition-colors disabled:opacity-50 touch-manipulation"
                aria-label="Close"
              >
                <X size={20} />
              </button>
            )}
          </div>
        )}
        <div
          className={twMerge(
            'flex-1 min-h-0 overflow-y-auto overflow-x-hidden overscroll-contain [-webkit-overflow-scrolling:touch]',
            bodyClassName
          )}
        >
          {children}
        </div>
        {footer && (
          <div
            className={twMerge(
              'shrink-0 border-t border-[var(--volera-border)] bg-[var(--volera-surface-muted)] px-4 py-3',
              'pb-[max(0.75rem,env(safe-area-inset-bottom,0px))] sm:pb-3',
              footerClassName
            )}
          >
            {footer}
          </div>
        )}
      </div>
    </div>,
    document.body
  );
}
