import React, { useCallback, useEffect, useRef, useState } from 'react';
import { callService } from '../services/callService';
import type { Call, PaginatedResult } from '../types/call';
import { Phone, PhoneIncoming, PhoneOutgoing, PhoneMissed, ArrowUpDown, ChevronLeft, ChevronRight, X } from 'lucide-react';
import { clsx } from 'clsx';
import { useAuthStore } from '../store/useAuthStore';

interface CallHistoryProps {
    onClose?: () => void;
    /** When true, render as tab content (no overlay, no close button). */
    embedded?: boolean;
}

const SKELETON_ROWS = 8;
const PAGE_SIZE_DESKTOP = 5;
const PAGE_SIZE_MOBILE = 3;

function usePageSize(): number {
    const [pageSize, setPageSize] = useState(PAGE_SIZE_DESKTOP);
    useEffect(() => {
        const mql = window.matchMedia('(min-width: 640px)');
        const update = () => setPageSize(mql.matches ? PAGE_SIZE_DESKTOP : PAGE_SIZE_MOBILE);
        update();
        mql.addEventListener('change', update);
        return () => mql.removeEventListener('change', update);
    }, []);
    return pageSize;
}

function SkeletonRow({ isCard }: { isCard?: boolean }) {
    if (isCard) {
        return (
            <div className="p-4 rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 animate-pulse">
                <div className="flex items-center gap-3 mb-2">
                    <div className="w-10 h-10 rounded-full bg-gray-200 dark:bg-gray-600" />
                    <div className="flex-1 min-w-0">
                        <div className="h-4 bg-gray-200 dark:bg-gray-600 rounded w-3/4 mb-2" />
                        <div className="h-3 bg-gray-200 dark:bg-gray-600 rounded w-1/2" />
                    </div>
                </div>
                <div className="flex justify-between text-sm">
                    <div className="h-3 bg-gray-200 dark:bg-gray-600 rounded w-20" />
                    <div className="h-3 bg-gray-200 dark:bg-gray-600 rounded w-12" />
                </div>
            </div>
        );
    }
    return (
        <tr className="animate-pulse">
            {[1, 2, 3, 4, 5].map((i) => (
                <td key={i} className="px-3 sm:px-4 py-3">
                    <div className="h-4 bg-gray-200 dark:bg-gray-600 rounded w-full max-w-[120px]" />
                </td>
            ))}
        </tr>
    );
}

export const CallHistory: React.FC<CallHistoryProps> = ({ onClose, embedded = false }) => {
    const { user: currentUser } = useAuthStore();
    const currentUserId = currentUser?.id?.toLowerCase?.();
    const pageSize = usePageSize();

    const [data, setData] = useState<PaginatedResult<Call> | null>(null);
    const [loading, setLoading] = useState(false);
    const [isInitialLoad, setIsInitialLoad] = useState(true);

    const [page, setPage] = useState(1);
    const [sortBy, setSortBy] = useState('startTime');
    const [sortDescending, setSortDescending] = useState(true);

    const abortRef = useRef<AbortController | null>(null);

    const fetchData = useCallback(async (opts?: { resetPage?: boolean }) => {
        if (abortRef.current) abortRef.current.abort();
        const nextPage = opts?.resetPage ? 1 : page;
        if (opts?.resetPage) setPage(1);

        const controller = new AbortController();
        abortRef.current = controller;

        setLoading(true);
        if (!data && isInitialLoad) setIsInitialLoad(true);

        try {
            const result = await callService.getHistory({
                page: nextPage,
                pageSize,
                sortBy,
                sortDescending
            });
            if (abortRef.current !== controller) return;
            setData(result);
            if (opts?.resetPage) setPage(1);
        } catch (error) {
            if (abortRef.current !== controller) return;
            console.error("Failed to fetch call history", error);
            setData(prev => prev ?? { items: [], totalCount: 0, page: 1, pageSize, totalPages: 0 });
        } finally {
            if (abortRef.current === controller) {
                setLoading(false);
                setIsInitialLoad(false);
            }
        }
    }, [page, pageSize, sortBy, sortDescending]);

    useEffect(() => {
        fetchData();
        return () => {
            if (abortRef.current) abortRef.current.abort();
        };
    }, [page, pageSize, sortBy, sortDescending]);

    const formatDuration = (seconds?: number) => {
        if (!seconds) return '0s';
        const mins = Math.floor(seconds / 60);
        const secs = seconds % 60;
        return `${mins}m ${secs}s`;
    };

    const formatDateTime = (dateStr: string) => {
        return new Date(dateStr).toLocaleString(undefined, {
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            ...(window.matchMedia('(min-width: 640px)').matches ? { year: 'numeric' } : {})
        });
    };

    const formatDateShort = (dateStr: string) => {
        return new Date(dateStr).toLocaleDateString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
    };

    const getStatusIcon = (status: string, isCaller: boolean) => {
        switch (status) {
            case 'Missed':
                return <PhoneMissed size={18} className="text-red-500 flex-shrink-0" />;
            case 'Rejected':
                return <PhoneMissed size={18} className="text-orange-500 flex-shrink-0" />;
            default:
                return isCaller ? <PhoneOutgoing size={18} className="text-[var(--volera-accent)] flex-shrink-0" /> : <PhoneIncoming size={18} className="text-green-500 flex-shrink-0" />;
        }
    };

    const isCaller = (call: Call): boolean => !!(currentUserId && call.callerId?.toLowerCase() === currentUserId);
    const otherPartyName = (call: Call) => isCaller(call) ? call.receiverName : call.callerName;

    const showSkeleton = loading && (isInitialLoad || !data?.items.length);
    const showTableLoading = loading && data?.items.length && !isInitialLoad;

    const content = (
        <div className={clsx(
            "flex flex-col overflow-hidden min-h-0",
            embedded ? "h-full bg-gray-100 dark:bg-gray-900" : "bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-4xl max-h-[90vh]"
        )}>
            {/* Header */}
            <div className={clsx(
                "p-3 sm:p-4 border-b flex justify-between items-center flex-shrink-0",
                embedded ? "border-gray-300 dark:border-gray-700 bg-gray-100 dark:bg-gray-900" : "border-gray-200 bg-gray-50 dark:bg-gray-800"
            )}>
                <h2 className="text-lg sm:text-xl font-bold text-gray-800 dark:text-gray-200 flex items-center gap-2">
                    <Phone size={22} className="text-[var(--volera-accent)] flex-shrink-0" />
                    Calls
                </h2>
                {!embedded && onClose && (
                    <button onClick={onClose} className="p-2 text-gray-500 hover:text-red-600 rounded-full hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors touch-manipulation">
                        <X size={22} />
                    </button>
                )}
            </div>

            {/* List / Table area - scrollable */}
            <div className={clsx(
                "flex-1 overflow-auto min-h-0 p-3 sm:p-4 relative",
                embedded ? "bg-gray-100 dark:bg-gray-900" : "bg-gray-50 dark:bg-gray-800/50"
            )}>
                {showTableLoading && (
                    <div className="absolute inset-0 bg-white/60 dark:bg-gray-900/60 flex items-center justify-center z-10 rounded-lg">
                        <div className="animate-spin rounded-full h-8 w-8 border-2 border-[var(--volera-accent)] border-t-transparent" />
                    </div>
                )}

                {/* Mobile: card list */}
                <div className="sm:hidden space-y-2">
                    {showSkeleton && Array.from({ length: Math.min(SKELETON_ROWS, pageSize) }).map((_, i) => (
                        <SkeletonRow key={i} isCard />
                    ))}
                    {!showSkeleton && data?.items.map((call) => (
                        <div
                            key={call.id}
                            className="p-4 rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 shadow-sm"
                        >
                            <div className="flex items-start gap-3">
                                <div className="flex-shrink-0 mt-0.5">
                                    {getStatusIcon(call.status, isCaller(call))}
                                </div>
                                <div className="flex-1 min-w-0">
                                    <p className="font-medium text-gray-900 dark:text-gray-100 truncate">
                                        {otherPartyName(call) || 'Unknown'}
                                    </p>
                                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                                        {isCaller(call) ? 'Outgoing' : 'Incoming'} · {formatDateShort(call.startTime)}
                                    </p>
                                    <div className="flex items-center gap-2 mt-2 flex-wrap">
                                        <span className={clsx(
                                            "px-2 py-0.5 text-xs font-medium rounded-full",
                                            call.status === 'Connected' || call.status === 'Ended' ? "bg-green-100 dark:bg-green-900/40 text-green-800 dark:text-green-300" :
                                            call.status === 'Missed' ? "bg-red-100 dark:bg-red-900/40 text-red-800 dark:text-red-300" :
                                            "bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-300"
                                        )}>
                                            {call.status}
                                        </span>
                                        <span className="text-xs text-gray-500 dark:text-gray-400">
                                            {formatDuration(call.duration)}
                                        </span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    ))}
                    {!showSkeleton && data?.items.length === 0 && (
                        <div className="py-12 text-center text-gray-500 dark:text-gray-400 text-sm">
                            No call history found.
                        </div>
                    )}
                </div>

                {/* Desktop: table */}
                <div className="hidden sm:block rounded-lg overflow-hidden border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm">
                    <table className="w-full border-collapse">
                        <thead className="bg-gray-50 dark:bg-gray-700/80 border-b border-gray-200 dark:border-gray-600 sticky top-0 z-[1]">
                            <tr>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Contact / Direction</th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-600" onClick={() => { setSortBy('startTime'); setSortDescending(!sortDescending); setPage(1); }}>
                                    <div className="flex items-center gap-1">Date {sortBy === 'startTime' && <ArrowUpDown size={12} />}</div>
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-600" onClick={() => { setSortBy('duration'); setSortDescending(!sortDescending); setPage(1); }}>
                                    <div className="flex items-center gap-1">Duration {sortBy === 'duration' && <ArrowUpDown size={12} />}</div>
                                </th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Status</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-200 dark:divide-gray-600">
                            {showSkeleton && Array.from({ length: Math.min(SKELETON_ROWS, pageSize) }).map((_, i) => (
                                <SkeletonRow key={i} />
                            ))}
                            {!showSkeleton && data?.items.map((call) => (
                                <tr key={call.id} className="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
                                    <td className="px-4 py-3">
                                        <div className="flex items-center gap-2">
                                            {getStatusIcon(call.status, isCaller(call))}
                                            <span className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate max-w-[200px]" title={otherPartyName(call) || ''}>
                                                {otherPartyName(call) || 'Unknown'}
                                            </span>
                                            <span className="text-xs text-gray-500 dark:text-gray-400">({isCaller(call) ? 'Outgoing' : 'Incoming'})</span>
                                        </div>
                                    </td>
                                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400 whitespace-nowrap">{formatDateTime(call.startTime)}</td>
                                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400 whitespace-nowrap">{formatDuration(call.duration)}</td>
                                    <td className="px-4 py-3">
                                        <span className={clsx(
                                            "px-2 py-1 text-xs font-medium rounded-full",
                                            call.status === 'Connected' || call.status === 'Ended' ? "bg-green-100 dark:bg-green-900/40 text-green-800 dark:text-green-300" :
                                            call.status === 'Missed' ? "bg-red-100 dark:bg-red-900/40 text-red-800 dark:text-red-300" :
                                            "bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-300"
                                        )}>
                                            {call.status}
                                        </span>
                                    </td>
                                </tr>
                            ))}
                            {!showSkeleton && data?.items.length === 0 && (
                                <tr>
                                    <td colSpan={4} className="px-4 py-12 text-center text-gray-500 dark:text-gray-400 text-sm">
                                        No call history found.
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Pagination - compact on mobile */}
            {data && data.totalPages > 0 && (
                <div className="p-3 sm:p-4 border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 flex-shrink-0 flex flex-wrap items-center justify-between gap-2">
                    <span className="text-xs sm:text-sm text-gray-600 dark:text-gray-400">
                        <span className="font-medium text-gray-800 dark:text-gray-200">{data.page}</span>
                        <span className="mx-1">/</span>
                        <span className="font-medium">{data.totalPages}</span>
                        <span className="ml-1 sm:ml-2 text-gray-500">({data.totalCount})</span>
                    </span>
                    <div className="flex items-center gap-1">
                        <button
                            type="button"
                            onClick={() => setPage(p => Math.max(1, p - 1))}
                            disabled={page <= 1 || loading}
                            className="p-2 sm:px-3 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed text-gray-700 dark:text-gray-300 touch-manipulation"
                            aria-label="Previous page"
                        >
                            <ChevronLeft size={20} />
                        </button>
                        <button
                            type="button"
                            onClick={() => setPage(p => Math.min(data.totalPages, p + 1))}
                            disabled={page >= data.totalPages || loading}
                            className="p-2 sm:px-3 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed text-gray-700 dark:text-gray-300 touch-manipulation"
                            aria-label="Next page"
                        >
                            <ChevronRight size={20} />
                        </button>
                    </div>
                </div>
            )}
        </div>
    );

    if (embedded) {
        return <div className="flex-1 flex flex-col overflow-hidden min-h-0">{content}</div>;
    }
    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
            <div className="animate-in fade-in zoom-in duration-200 w-full max-w-4xl max-h-[90vh] flex justify-center min-h-0">
                {content}
            </div>
        </div>
    );
};
