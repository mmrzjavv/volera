import React, { useEffect, useRef, useState } from 'react';
import { useConfirmationStore } from '../store/useConfirmationStore';
import { Button } from './ui/Button';
import { AlertTriangle, Info, AlertCircle } from 'lucide-react';
import { createPortal } from 'react-dom';
import { clsx } from 'clsx';

export const ConfirmationDialog: React.FC = () => {
  const { isOpen, title, message, confirmText, cancelText, variant, onConfirm, onCancel, closeDialog } = useConfirmationStore();
  const cancelButtonRef = useRef<HTMLButtonElement>(null);
  const overlayRef = useRef<HTMLDivElement>(null);
  const [isConfirming, setIsConfirming] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setIsConfirming(false);
      // Focus cancel button when dialog opens for safety
      setTimeout(() => {
        cancelButtonRef.current?.focus();
      }, 50);

      // Lock body scroll
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'unset';
    }

    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isOpen]);

  // Handle escape key (only when not confirming)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen && !isConfirming) {
        handleCancel();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, isConfirming]);

  const handleConfirm = async () => {
    if (isConfirming) return;
    setIsConfirming(true);
    try {
      await Promise.resolve(onConfirm());
      closeDialog();
    } catch (err) {
      console.error('Confirmation action failed:', err);
      // Keep dialog open on error; error feedback comes from caller (toast etc.)
    } finally {
      setIsConfirming(false);
    }
  };

  const handleCancel = () => {
    if (isConfirming) return;
    onCancel();
    closeDialog();
  };

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (e.target === overlayRef.current && !isConfirming) {
      handleCancel();
    }
  };

  if (!isOpen) return null;

  const getIcon = () => {
    switch (variant) {
      case 'danger':
        return <AlertTriangle className="w-6 h-6 text-red-600 dark:text-red-400" />;
      case 'warning':
        return <AlertCircle className="w-6 h-6 text-yellow-600 dark:text-yellow-400" />;
      case 'info':
        return <Info className="w-6 h-6 text-[var(--volera-accent)]" />;
    }
  };

  const getVariantStyles = () => {
      switch (variant) {
          case 'danger': return 'bg-red-50 dark:bg-red-900/20';
          case 'warning': return 'bg-yellow-50 dark:bg-yellow-900/20';
          case 'info': return 'bg-[var(--volera-accent)]/10';
      }
  };

  return createPortal(
    <div 
      className="fixed inset-0 z-[100] flex items-end sm:items-center justify-center bg-black/50 backdrop-blur-sm pt-[env(safe-area-inset-top,0px)] pl-[env(safe-area-inset-left,0px)] pr-[env(safe-area-inset-right,0px)] pb-[env(safe-area-inset-bottom,0px)] sm:p-4"
      ref={overlayRef}
      onClick={handleOverlayClick}
      role="dialog"
      aria-modal="true"
      aria-labelledby="dialog-title"
      aria-describedby="dialog-description"
    >
      <div className="w-full max-w-md max-h-[min(92dvh,100%)] sm:max-h-[90vh] flex flex-col bg-[var(--volera-surface)] rounded-t-[var(--volera-radius-lg)] sm:rounded-[var(--volera-radius-md)] shadow-2xl border border-[var(--volera-border)] overflow-hidden volera-fade-up">
        <div className="sm:hidden flex justify-center pt-2 shrink-0" aria-hidden>
          <div className="w-10 h-1 rounded-full bg-[var(--volera-border)]" />
        </div>
        <div className="p-5 sm:p-6 overflow-y-auto overflow-x-hidden min-h-0 flex-1 overscroll-contain">
            <div className="flex items-start gap-3 sm:gap-4 min-w-0">
                <div className={clsx("p-3 rounded-full flex-shrink-0", getVariantStyles())}>
                    {getIcon()}
                </div>
                <div className="flex-1 min-w-0">
                    <h3 id="dialog-title" className="text-lg font-semibold text-[var(--volera-text)] leading-6 line-clamp-2 break-words">
                        {title}
                    </h3>
                    <div className="mt-2 min-w-0">
                        <p id="dialog-description" className="text-sm text-[var(--volera-text-muted)] break-words">
                            {message}
                        </p>
                    </div>
                </div>
            </div>
        </div>

        <div className="bg-[var(--volera-surface-muted)] px-4 sm:px-6 py-4 flex flex-col-reverse sm:flex-row-reverse gap-3 border-t border-[var(--volera-border)] shrink-0 pb-[max(1rem,env(safe-area-inset-bottom,0px))] sm:pb-4">
          <Button
            variant={variant === 'danger' ? 'danger' : 'primary'}
            onClick={handleConfirm}
            disabled={isConfirming}
            isLoading={isConfirming}
            className="w-full sm:w-auto shadow-sm"
          >
            {confirmText}
          </Button>
          <Button
            type="button"
            variant="secondary"
            onClick={handleCancel}
            ref={cancelButtonRef}
            disabled={isConfirming}
            className="w-full sm:w-auto"
          >
            {cancelText}
          </Button>
        </div>
      </div>
    </div>,
    document.body
  );
};
