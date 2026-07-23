import { create } from 'zustand';

interface ConfirmationDialogState {
  isOpen: boolean;
  title: string;
  message: string;
  confirmText: string;
  cancelText: string;
  variant: 'danger' | 'warning' | 'info';
  onConfirm: () => void;
  onCancel: () => void;
  openDialog: (options: {
    title: string;
    message: string;
    confirmText?: string;
    cancelText?: string;
    variant?: 'danger' | 'warning' | 'info';
    onConfirm: () => void;
    onCancel?: () => void;
  }) => void;
  closeDialog: () => void;
}

export const useConfirmationStore = create<ConfirmationDialogState>((set) => ({
  isOpen: false,
  title: '',
  message: '',
  confirmText: 'Confirm',
  cancelText: 'Cancel',
  variant: 'danger',
  onConfirm: () => {},
  onCancel: () => {},
  openDialog: (options) => set({
    isOpen: true,
    title: options.title,
    message: options.message,
    confirmText: options.confirmText || 'Confirm',
    cancelText: options.cancelText || 'Cancel',
    variant: options.variant || 'danger',
    onConfirm: options.onConfirm,
    onCancel: options.onCancel || (() => {}),
  }),
  closeDialog: () => set({ isOpen: false }),
}));
