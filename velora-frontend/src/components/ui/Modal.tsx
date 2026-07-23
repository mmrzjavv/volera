import React, { useEffect, useRef } from 'react';
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
  /** Extra header content (e.g. icon + title already composed) */
  headerClassName?: string;
  bodyClassName?: string;
  footerClassName?: string;
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
}: ModalProps) {
  const overlayRef = useRef<HTMLDivElement>(null);

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
      className="fixed inset-0 z-[var(--volera-z-modal)] flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm"
      onClick={handleOverlayClick}
      role="dialog"
      aria-modal="true"
    >
      <div
        className={twMerge(
          'w-full max-h-[90vh] flex flex-col overflow-hidden rounded-[var(--volera-radius-md)] shadow-xl border border-[var(--volera-border)] bg-[var(--volera-surface)] text-[var(--volera-text)]',
          maxWidth,
          className
        )}
      >
        {(title || !hideCloseButton) && (
          <div
            className={twMerge(
              'shrink-0 flex items-center justify-between gap-3 px-4 py-3 border-b border-[var(--volera-border)] bg-[var(--volera-surface-muted)]',
              headerClassName
            )}
          >
            <div className="min-w-0 flex-1 font-semibold text-lg truncate">{title}</div>
            {!hideCloseButton && (
              <button
                type="button"
                onClick={() => !closeDisabled && onClose()}
                disabled={closeDisabled}
                className="shrink-0 p-2 min-h-[44px] min-w-[44px] flex items-center justify-center rounded-full text-[var(--volera-text-muted)] hover:text-[var(--volera-text)] hover:bg-[var(--volera-border)]/40 transition-colors disabled:opacity-50"
                aria-label="Close"
              >
                <X size={20} />
              </button>
            )}
          </div>
        )}
        <div className={twMerge('flex-1 min-h-0 overflow-y-auto overflow-x-hidden', bodyClassName)}>
          {children}
        </div>
        {footer && (
          <div
            className={twMerge(
              'shrink-0 border-t border-[var(--volera-border)] bg-[var(--volera-surface-muted)] px-4 py-3',
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
