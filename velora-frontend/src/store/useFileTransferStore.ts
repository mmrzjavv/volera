import { create } from 'zustand';

export interface TransferStatus {
    id: string; // Message ID or File ID/Name
    type: 'upload' | 'download';
    progress: number; // 0-100
    status: 'pending' | 'uploading' | 'downloading' | 'completed' | 'error';
    error?: string;
    cancel?: () => void;
}

interface FileTransferState {
    transfers: Record<string, TransferStatus>;
    addTransfer: (id: string, type: 'upload' | 'download', cancel?: () => void) => void;
    updateProgress: (id: string, progress: number) => void;
    completeTransfer: (id: string) => void;
    failTransfer: (id: string, error: string) => void;
    removeTransfer: (id: string) => void;
    getTransfer: (id: string) => TransferStatus | undefined;
}

export const useFileTransferStore = create<FileTransferState>((set, get) => ({
    transfers: {},
    addTransfer: (id, type, cancel) => set((state) => ({
        transfers: {
            ...state.transfers,
            [id]: {
                id,
                type,
                progress: 0,
                status: type === 'upload' ? 'uploading' : 'downloading',
                cancel
            }
        }
    })),
    updateProgress: (id, progress) => set((state) => {
        const transfer = state.transfers[id];
        if (!transfer) return state;
        return {
            transfers: {
                ...state.transfers,
                [id]: { ...transfer, progress }
            }
        };
    }),
    completeTransfer: (id) => set((state) => {
        const transfer = state.transfers[id];
        if (!transfer) return state;
        return {
            transfers: {
                ...state.transfers,
                [id]: { ...transfer, status: 'completed', progress: 100 }
            }
        };
    }),
    failTransfer: (id, error) => set((state) => {
        const transfer = state.transfers[id];
        if (!transfer) return state;
        return {
            transfers: {
                ...state.transfers,
                [id]: { ...transfer, status: 'error', error }
            }
        };
    }),
    removeTransfer: (id) => set((state) => {
        const { [id]: _, ...rest } = state.transfers;
        return { transfers: rest };
    }),
    getTransfer: (id) => get().transfers[id]
}));
