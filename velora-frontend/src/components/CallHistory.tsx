import React, { useCallback, useEffect, useRef, useState } from 'react';
import { callService } from '../services/callService';
import type { Call, PaginatedResult } from '../types/call';
import { Phone, PhoneIncoming, PhoneOutgoing, PhoneMissed, ArrowUpDown, ChevronLeft, ChevronRight, X } from 'lucide-react';
import { clsx } from 'clsx';
import { useAuthStore } from '../store/useAuthStore';
import { getInitials } from '../utils/getInitials';

interface CallHistoryProps {
    onClose?: () => void;
    /** When true, render as tab content (no overlay, no close button). */
    embedded?: boolean;
}

const SKELETON_ROWS = 8;
const PAGE_SIZE_DESKTOP = 8;
const PAGE_SIZE_MOBILE = 6;

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

function SkeletonRow() {
    return (
        <div className="p-3 sm:p-4 border-b border-[var(--volera-border)] flex items-center gap-3 min-h-[56px] animate-pulse">
            <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-full bg-[var(--volera-surface-muted)] shrink-0" />
            <div className="flex-1 min-w-0 space-y-2">
                <div className="h-4 bg-[var(--volera-surface-muted)] rounded w-2/3 max-w-[200px]" />
                <div className="h-3 bg-[var(--volera-surface-muted)] rounded w-1/2 max-w-[140px]" />
            </div>
        </div>
    );
}

function statusBadgeClass(status: string): string {
    if (status === 'Connected' || status === 'Ended') {
        return 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-800 dark:text-emerald-300';
    }
    if (status === 'Missed') {
        return 'bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300';
    }
    if (status === 'Rejected') {
        return 'bg-orange-100 dark:bg-orange-900/30 text-orange-800 dark:text-orange-300';
    }
    return 'bg-[var(--volera-surface-muted)] text-[var(--volera-text-muted)]';
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
        if (mins === 0) return `${secs}s`;
        return `${mins}m ${secs}s`;
    };

    const formatTime = (dateStr: string) => {
        const date = new Date(dateStr);
        const now = new Date();
        const isToday =
            date.getDate() === now.getDate() &&
            date.getMonth() === now.getMonth() &&
            date.getFullYear() === now.getFullYear();

        if (isToday) {
            return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        }

        const diffDays = Math.ceil(Math.abs(now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));
        if (diffDays < 7) {
            return date.toLocaleDateString([], { weekday: 'short', hour: '2-digit', minute: '2-digit' });
        }
        return date.toLocaleDateString([], { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
    };

    const getStatusIcon = (status: string, caller: boolean) => {
        switch (status) {
            case 'Missed':
                return <PhoneMissed size={20} className="text-red-500" />;
            case 'Rejected':
                return <PhoneMissed size={20} className="text-orange-500" />;
            default:
                return caller
                    ? <PhoneOutgoing size={20} className="text-[var(--volera-accent)]" />
                    : <PhoneIncoming size={20} className="text-[var(--volera-accent)]" />;
        }
    };

    const isCaller = (call: Call): boolean => !!(currentUserId && call.callerId?.toLowerCase() === currentUserId);
    const otherPartyName = (call: Call) => isCaller(call) ? call.receiverName : call.callerName;

    const showSkeleton = loading && (isInitialLoad || !data?.items.length);
    const showListLoading = loading && !!data?.items.length && !isInitialLoad;

    const toggleSort = (field: string) => {
        if (sortBy === field) {
            setSortDescending((d) => !d);
        } else {
            setSortBy(field);
            setSortDescending(true);
        }
        setPage(1);
    };

    const listShell = (
        <div className={clsx(
            "flex-1 flex flex-col overflow-hidden min-h-0",
            embedded ? "bg-[var(--volera-bg)]" : ""
        )}>
            <div className="p-3 sm:p-4 border-b border-[var(--volera-border)] flex justify-between items-center bg-[var(--volera-surface)] shrink-0 gap-2">
                <h2 className="font-semibold text-base sm:text-lg text-[var(--volera-text)] truncate flex items-center gap-2">
                    {!embedded && <Phone size={20} className="text-[var(--volera-accent)] shrink-0" />}
                    Calls
                </h2>
                <div className="flex items-center gap-1 shrink-0">
                    <button
                        type="button"
                        onClick={() => toggleSort('startTime')}
                        className={clsx(
                            "volera-icon-btn text-xs sm:text-sm px-2 gap-1 min-h-[36px]",
                            sortBy === 'startTime' && "text-[var(--volera-accent)] bg-[var(--volera-accent-soft)]"
                        )}
                        title="Sort by date"
                    >
                        <ArrowUpDown size={16} />
                        <span className="hidden sm:inline">Date</span>
                    </button>
                    <button
                        type="button"
                        onClick={() => toggleSort('duration')}
                        className={clsx(
                            "volera-icon-btn text-xs sm:text-sm px-2 gap-1 min-h-[36px]",
                            sortBy === 'duration' && "text-[var(--volera-accent)] bg-[var(--volera-accent-soft)]"
                        )}
                        title="Sort by duration"
                    >
                        <ArrowUpDown size={16} />
                        <span className="hidden sm:inline">Duration</span>
                    </button>
                    {!embedded && onClose && (
                        <button
                            type="button"
                            onClick={onClose}
                            className="volera-icon-btn text-[var(--volera-text-muted)] hover:text-[var(--volera-danger)] min-h-[36px]"
                            aria-label="Close call history"
                        >
                            <X size={20} />
                        </button>
                    )}
                </div>
            </div>

            <div className="flex-1 overflow-y-auto overflow-x-hidden min-h-0 relative">
                {showListLoading && (
                    <div className="absolute inset-0 bg-[var(--volera-bg)]/70 flex items-center justify-center z-10">
                        <div className="animate-spin rounded-full h-8 w-8 border-2 border-[var(--volera-accent)] border-t-transparent" />
                    </div>
                )}

                {showSkeleton &&
                    Array.from({ length: Math.min(SKELETON_ROWS, pageSize) }).map((_, i) => (
                        <SkeletonRow key={i} />
                    ))}

                {!showSkeleton && data?.items.map((call) => {
                    const name = otherPartyName(call) || 'Unknown';
                    const caller = isCaller(call);
                    return (
                        <div
                            key={call.id}
                            className="p-3 sm:p-4 border-b border-[var(--volera-border)] hover:bg-[var(--volera-surface-muted)] active:bg-[var(--volera-surface-muted)] transition-colors flex items-center gap-2 sm:gap-3 min-h-[56px] touch-manipulation"
                        >
                            <div className="w-10 h-10 sm:w-12 sm:h-12 rounded-full bg-[var(--volera-surface-muted)] flex items-center justify-center shrink-0 text-sm sm:text-base font-semibold text-[var(--volera-text-muted)]">
                                {getInitials(name) || '?'}
                            </div>
                            <div className="flex-1 min-w-0 overflow-hidden">
                                <div className="flex items-baseline gap-2 mb-0.5">
                                    <span className="font-medium text-[var(--volera-text)] truncate text-sm sm:text-base">{name}</span>
                                    <span className="text-[11px] sm:text-xs text-[var(--volera-text-muted)] shrink-0 whitespace-nowrap">
                                        {formatTime(call.startTime)}
                                    </span>
                                </div>
                                <div className="flex items-center gap-2 min-w-0 flex-wrap">
                                    <span className="inline-flex items-center gap-1 text-xs text-[var(--volera-text-muted)]">
                                        {getStatusIcon(call.status, caller)}
                                        {caller ? 'Outgoing' : 'Incoming'}
                                    </span>
                                    <span className={clsx('px-2 py-0.5 text-[10px] sm:text-xs font-medium rounded-full', statusBadgeClass(call.status))}>
                                        {call.status}
                                    </span>
                                    <span className="text-xs text-[var(--volera-text-muted)] tabular-nums">
                                        {formatDuration(call.duration)}
                                    </span>
                                </div>
                            </div>
                        </div>
                    );
                })}

                {!showSkeleton && data?.items.length === 0 && (
                    <div className="p-8 text-center text-[var(--volera-text-muted)] flex flex-col items-center justify-center">
                        <div className="w-16 h-16 bg-[var(--volera-surface-muted)] rounded-full flex items-center justify-center mb-4">
                            <Phone size={32} className="opacity-40 text-[var(--volera-text-muted)]" />
                        </div>
                        <p className="font-medium text-[var(--volera-text)]">No calls yet</p>
                        <p className="text-sm mt-1 max-w-[16rem]">Your incoming and outgoing calls will appear here.</p>
                    </div>
                )}
            </div>

            {data && data.totalPages > 0 && (
                <div className="p-3 sm:p-4 border-t border-[var(--volera-border)] bg-[var(--volera-surface)] flex-shrink-0 flex flex-wrap items-center justify-between gap-2">
                    <span className="text-xs sm:text-sm text-[var(--volera-text-muted)] tabular-nums">
                        <span className="font-medium text-[var(--volera-text)]">{data.page}</span>
                        <span className="mx-1">/</span>
                        <span className="font-medium text-[var(--volera-text)]">{data.totalPages}</span>
                        <span className="ml-1 sm:ml-2">({data.totalCount})</span>
                    </span>
                    <div className="flex items-center gap-1">
                        <button
                            type="button"
                            onClick={() => setPage((p) => Math.max(1, p - 1))}
                            disabled={page <= 1 || loading}
                            className="volera-icon-btn min-w-[44px] min-h-[44px] disabled:opacity-50 disabled:cursor-not-allowed"
                            aria-label="Previous page"
                        >
                            <ChevronLeft size={20} />
                        </button>
                        <button
                            type="button"
                            onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
                            disabled={page >= data.totalPages || loading}
                            className="volera-icon-btn min-w-[44px] min-h-[44px] disabled:opacity-50 disabled:cursor-not-allowed"
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
        return listShell;
    }

    return (
        <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center bg-black/50 backdrop-blur-sm pt-[env(safe-area-inset-top,0px)] pl-[env(safe-area-inset-left,0px)] pr-[env(safe-area-inset-right,0px)] pb-[env(safe-area-inset-bottom,0px)] sm:p-4">
            <div className="w-full max-w-lg max-h-[min(92dvh,100%)] sm:max-h-[90vh] h-[min(92dvh,100%)] sm:h-auto flex flex-col min-h-0 bg-[var(--volera-surface)] rounded-t-[var(--volera-radius-lg)] sm:rounded-2xl shadow-2xl border border-[var(--volera-border)] overflow-hidden volera-fade-up">
                <div className="sm:hidden flex justify-center pt-2 shrink-0" aria-hidden>
                    <div className="w-10 h-1 rounded-full bg-[var(--volera-border)]" />
                </div>
                {listShell}
            </div>
        </div>
    );
};
