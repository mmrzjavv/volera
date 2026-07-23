import React, { useState, useRef, useEffect, useCallback } from 'react';
import { createPortal } from 'react-dom';
import { clsx } from 'clsx';
import type { Message } from '../../types';
import { MessageActions } from './MessageActions';
import { VoiceMessagePlayer } from './VoiceMessagePlayer';
import { ChatImage } from './ChatImage';
import { FileIcon, Check, CheckCheck, Download, XCircle, CheckSquare, Square } from 'lucide-react';
import { isRtl } from '../../utils/rtl';
import { getInitials } from '../../utils/getInitials';
import { useFileTransferStore } from '../../store/useFileTransferStore';
import { useToastStore } from '../../store/useToastStore';
import { fileService } from '../../services/api';
import { CircularProgress } from '../ui/CircularProgress';
import axios from 'axios';
import { ensureReachableMediaUrl } from '../../utils/ensureReachableMediaUrl';

/** Nearest ancestor that clips overflowing content (scroll/auto/hidden). */
function getOverflowParent(el: HTMLElement): HTMLElement | null {
    let node: HTMLElement | null = el.parentElement;
    while (node && node !== document.body) {
        const { overflowY, overflow } = getComputedStyle(node);
        if (
            overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'hidden' ||
            overflow === 'auto' || overflow === 'scroll' || overflow === 'hidden'
        ) {
            return node;
        }
        node = node.parentElement;
    }
    return null;
}

type DesktopActionsPos = {
    top: number;
    left?: number;
    right?: number;
    placeBelow: boolean;
};

/** Approx height of the desktop reactions/reply strip. */
const DESKTOP_ACTIONS_HEIGHT_PX = 44;
const DESKTOP_ACTIONS_GAP_PX = 8;

function useIsMobile(): boolean {
    const [isMobile, setIsMobile] = useState(() =>
        typeof window !== 'undefined' && window.matchMedia('(max-width: 767px)').matches
    );
    useEffect(() => {
        const mql = window.matchMedia('(max-width: 767px)');
        const handler = () => setIsMobile(mql.matches);
        mql.addEventListener('change', handler);
        return () => mql.removeEventListener('change', handler);
    }, []);
    return isMobile;
}

/** Renders content only when in view so opening a chat doesn't load every media. */
function useLazyInView(rootMargin = '200px') {
    const containerRef = useRef<HTMLDivElement>(null);
    const [inView, setInView] = useState(false);
    useEffect(() => {
        const el = containerRef.current;
        if (!el) return;
        const observer = new IntersectionObserver(
            (entries) => {
                for (const entry of entries) {
                    if (entry.isIntersecting) {
                        setInView(true);
                        break;
                    }
                }
            },
            { rootMargin, threshold: 0.01 }
        );
        observer.observe(el);
        return () => observer.disconnect();
    }, [rootMargin]);
    return { containerRef, inView };
}

/** Renders video only when in view so opening a chat doesn't load every video. */
function LazyVideo({ src, className }: { src: string; className?: string }) {
    const { containerRef, inView } = useLazyInView();
    const [resolvedSrc, setResolvedSrc] = useState(src);

    useEffect(() => {
        let cancelled = false;
        ensureReachableMediaUrl(src).then((url) => {
            if (!cancelled) setResolvedSrc(url);
        });
        return () => {
            cancelled = true;
        };
    }, [src]);

    if (!inView) {
        return (
            <div
                ref={containerRef}
                className={clsx('rounded-lg max-w-full min-h-[120px] bg-gray-200 dark:bg-gray-700 flex items-center justify-center', className)}
            >
                <span className="text-xs text-gray-500 dark:text-gray-400">Video</span>
            </div>
        );
    }
    return (
        <div ref={containerRef} className="relative">
            <video src={resolvedSrc} controls preload="metadata" className={clsx('rounded-lg max-w-full h-auto max-h-[300px]', className)} />
        </div>
    );
}

/** Renders voice player only when in view so opening a chat doesn't load every audio. */
function LazyVoiceMessagePlayer({ src, isMyMessage }: { src: string; isMyMessage: boolean }) {
    const { containerRef, inView } = useLazyInView();
    const [resolvedSrc, setResolvedSrc] = useState(src);

    useEffect(() => {
        let cancelled = false;
        ensureReachableMediaUrl(src).then((url) => {
            if (!cancelled) setResolvedSrc(url);
        });
        return () => {
            cancelled = true;
        };
    }, [src]);

    if (!inView) {
        return (
            <div
                ref={containerRef}
                className="rounded-lg max-w-full min-h-[52px] bg-gray-200 dark:bg-gray-700 flex items-center justify-center -mx-2 -my-1"
            >
                <span className="text-xs text-gray-500 dark:text-gray-400">Voice message</span>
            </div>
        );
    }
    return (
        <div ref={containerRef} className="-mx-2 -my-1">
            <VoiceMessagePlayer src={resolvedSrc} isMyMessage={isMyMessage} />
        </div>
    );
}

interface MessageBubbleProps {
    message: Message;
    isMyMessage: boolean;
    senderProfilePicture?: string;
    senderName?: string;
    onEdit: (message: Message) => void;
    onDelete: (messageId: string) => void;
    onSave: (messageId: string, isSaved: boolean) => void;
    onReply?: (message: Message) => void;
    onReact?: (message: Message, emoji: string) => void;
    onForward?: (message: Message) => void;
    onTogglePin?: (message: Message) => void;
    onCopyImage?: (message: Message) => void;
    /** When user clicks the image (opens full-screen viewer). If not set, click triggers download. */
    onImageClick?: (message: Message) => void;
    onSelect?: (message: Message) => void;
    /** Hide Save button (e.g. in user-to-user chat) */
    showSave?: boolean;
    /** Selection mode: show checkbox, clicking toggles selection */
    selectionMode?: boolean;
    isSelected?: boolean;
    onToggleSelect?: (messageId: string) => void;
    isReactionPending?: boolean;
    isSavePending?: boolean;
    isPinPending?: boolean;
}

export const MessageBubble: React.FC<MessageBubbleProps> = ({ message, isMyMessage, senderProfilePicture, senderName, onEdit, onDelete, onSave, onReply, onReact, onForward, onTogglePin, onCopyImage, onImageClick, onSelect, showSave = true, selectionMode = false, isSelected = false, onToggleSelect, isReactionPending, isSavePending, isPinPending }) => {
    const isDeleted = !!message.deletedAt;
    const isMessageRtl = isRtl(message.content || "");
    const [showActions, setShowActions] = useState(false);
    const [desktopActionsPos, setDesktopActionsPos] = useState<DesktopActionsPos | null>(null);
    const rowRef = useRef<HTMLDivElement>(null);
    const closeActionsTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
    const longPressTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
    const touchStartRef = useRef<{ x: number; y: number } | null>(null);
    const isMobile = useIsMobile();

    const LONG_PRESS_MS = 550;
    const MOVE_THRESHOLD_PX = 10;

    const clearCloseActionsTimer = useCallback(() => {
        if (closeActionsTimerRef.current) {
            clearTimeout(closeActionsTimerRef.current);
            closeActionsTimerRef.current = null;
        }
    }, []);

    const scheduleCloseDesktopActions = useCallback(() => {
        clearCloseActionsTimer();
        closeActionsTimerRef.current = setTimeout(() => {
            setShowActions(false);
            setDesktopActionsPos(null);
        }, 120);
    }, [clearCloseActionsTimer]);

    const placeDesktopActions = useCallback(() => {
        const el = rowRef.current;
        if (!el) return;

        const rect = el.getBoundingClientRect();
        const clipParent = getOverflowParent(el);
        const clipTop = clipParent?.getBoundingClientRect().top ?? 0;
        const needed = DESKTOP_ACTIONS_HEIGHT_PX + DESKTOP_ACTIONS_GAP_PX;
        const spaceAbove = rect.top - clipTop;
        // Prefer above; flip below near the scroll/header edge so the strip stays fully usable.
        const placeBelow = spaceAbove < needed;

        setDesktopActionsPos({
            top: placeBelow
                ? rect.bottom + DESKTOP_ACTIONS_GAP_PX
                : rect.top - DESKTOP_ACTIONS_GAP_PX,
            left: isMyMessage ? undefined : rect.left,
            right: isMyMessage ? Math.max(0, window.innerWidth - rect.right) : undefined,
            placeBelow,
        });
    }, [isMyMessage]);

    const openDesktopActions = useCallback(() => {
        if (isDeleted || isMobile || selectionMode) return;
        clearCloseActionsTimer();
        placeDesktopActions();
        setShowActions(true);
    }, [isDeleted, isMobile, selectionMode, clearCloseActionsTimer, placeDesktopActions]);

    useEffect(() => {
        if (!showActions || isMobile || isDeleted || selectionMode) return;

        const reposition = () => placeDesktopActions();
        window.addEventListener('resize', reposition);
        const clipParent = rowRef.current ? getOverflowParent(rowRef.current) : null;
        clipParent?.addEventListener('scroll', reposition, { passive: true });

        return () => {
            window.removeEventListener('resize', reposition);
            clipParent?.removeEventListener('scroll', reposition);
        };
    }, [showActions, isMobile, isDeleted, selectionMode, placeDesktopActions]);

    useEffect(() => () => clearCloseActionsTimer(), [clearCloseActionsTimer]);

    const { addTransfer, updateProgress, completeTransfer, failTransfer, getTransfer, removeTransfer } = useFileTransferStore();
    const addToast = useToastStore((s) => s.addToast);
    const transfer = getTransfer(message.attachmentUrl || '');
    const isDownloading = transfer?.status === 'downloading';

    const handleDownload = async (e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        
        if (!message.attachmentUrl) return;
        const url = await ensureReachableMediaUrl(message.attachmentUrl);
        
        if (isDownloading) return;

        // Check file size
        const size = await fileService.checkFileSize(url);
        const isHeavy = size > 1024 * 1024; // 1MB
        
        if (!isHeavy) {
            window.open(url, '_blank');
            return;
        }
        
        const cancelTokenSource = axios.CancelToken.source();
        addTransfer(url, 'download', () => cancelTokenSource.cancel());
        
        try {
            const blob = await fileService.downloadFile(url, (progress) => {
                updateProgress(url, progress);
            }, cancelTokenSource.token);
            
            completeTransfer(url);
            
            // Create object URL and download
            const objectUrl = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = objectUrl;
            link.download = url.split('/').pop()?.split('_').slice(1).join('_') || 'download';
            document.body.appendChild(link);
            link.click();
            link.remove();
            window.URL.revokeObjectURL(objectUrl);
            
            // Clear transfer status after a delay
            setTimeout(() => {
                removeTransfer(url);
            }, 2000);

        } catch (error) {
            if (axios.isCancel(error)) {
                console.log('Download canceled');
                removeTransfer(url);
            } else {
                console.error('Download failed', error);
                failTransfer(url, 'Download failed');
            }
        }
    };

    const handleCancelDownload = (e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        if (transfer && transfer.cancel) {
            transfer.cancel();
        }
    };

    const clearLongPressTimer = () => {
        if (longPressTimerRef.current) {
            clearTimeout(longPressTimerRef.current);
            longPressTimerRef.current = null;
        }
        touchStartRef.current = null;
    };

    const handleTouchStart = (e: React.TouchEvent) => {
        if (isDeleted || selectionMode) return;
        const t = e.targetTouches[0];
        if (t) touchStartRef.current = { x: t.clientX, y: t.clientY };
        longPressTimerRef.current = setTimeout(() => {
            longPressTimerRef.current = null;
            touchStartRef.current = null;
            setShowActions(true);
        }, LONG_PRESS_MS);
    };

    const handleTouchMove = (e: React.TouchEvent) => {
        const start = touchStartRef.current;
        if (!start || !e.targetTouches[0]) return;
        const dx = Math.abs(e.targetTouches[0].clientX - start.x);
        const dy = Math.abs(e.targetTouches[0].clientY - start.y);
        if (dx > MOVE_THRESHOLD_PX || dy > MOVE_THRESHOLD_PX) {
            clearLongPressTimer();
        }
    };

    const handleTouchEnd = clearLongPressTimer;
    const handleTouchCancel = clearLongPressTimer;

    const handleContextMenu = (e: React.MouseEvent) => {
        if (isMobile) {
            e.preventDefault();
            return;
        }
        e.preventDefault();
        if (!isDeleted) {
            clearCloseActionsTimer();
            placeDesktopActions();
            setShowActions(true);
        }
    };

    const hasImageAttachment = (message.attachmentType?.startsWith('image/') ?? false);
    const textContent = (message.content || '').trim();
    const hasTextContent = textContent.length > 0;
    const urlRegex = /https?:\/\/[^\s<>"{}|\\^`[\]]+/gi;
    const urls = textContent.match(urlRegex) ?? [];
    const hasLinks = urls.length > 0;

    const handleCopyText = () => {
        if (!hasTextContent) return;
        navigator.clipboard.writeText(textContent)
            .then(() => addToast('Text copied to clipboard', 'success'))
            .catch(() => addToast('Could not copy text', 'error'));
    };
    const handleCopyLink = () => {
        if (!hasLinks) return;
        const toCopy = urls.length === 1 ? urls[0] : urls.join('\n');
        navigator.clipboard.writeText(toCopy)
            .then(() => addToast('Link copied to clipboard', 'success'))
            .catch(() => addToast('Could not copy link', 'error'));
    };
    const handleBubbleClick = (e: React.MouseEvent) => {
        if (selectionMode && onToggleSelect) {
            e.stopPropagation();
            onToggleSelect(message.id);
        }
    };

    return (
        <div 
            ref={rowRef}
            id={`message-${message.id}`}
            className={clsx(
                "flex w-full mb-4 relative group",
                isMyMessage ? "justify-end" : "justify-start"
            )}
            onMouseEnter={openDesktopActions}
            onMouseLeave={() => !isMobile && scheduleCloseDesktopActions()}
            onTouchStart={selectionMode ? undefined : handleTouchStart}
            onTouchMove={selectionMode ? undefined : handleTouchMove}
            onTouchEnd={selectionMode ? undefined : handleTouchEnd}
            onTouchCancel={selectionMode ? undefined : handleTouchCancel}
            onContextMenu={selectionMode ? undefined : handleContextMenu}
        >
            <div className={clsx("flex max-w-[85%] md:max-w-[70%] items-start gap-2", isMyMessage ? "flex-row-reverse" : "flex-row")}>
                {selectionMode && (
                    <button
                        type="button"
                        onClick={(e) => { e.stopPropagation(); onToggleSelect?.(message.id); }}
                        className="flex-shrink-0 self-center p-1 rounded text-[var(--volera-accent)] hover:bg-[var(--volera-accent)]/10 transition-colors"
                        aria-label={isSelected ? 'Deselect' : 'Select'}
                    >
                        {isSelected ? <CheckSquare size={24} className="fill-current" /> : <Square size={24} />}
                    </button>
                )}
                <div className="flex-shrink-0 w-8 h-8 rounded-full bg-gray-200 overflow-hidden self-end mb-1 shadow-sm">
                    {senderProfilePicture ? (
                        <img src={senderProfilePicture} alt={senderName || "User"} className="w-full h-full object-cover" />
                    ) : (
                        <div className="w-full h-full flex items-center justify-center text-gray-500 text-xs font-bold uppercase bg-gray-100">
                            {getInitials(senderName)}
                        </div>
                    )}
                </div>

                <div
                    role={selectionMode ? 'button' : undefined}
                    tabIndex={selectionMode ? 0 : undefined}
                    onClick={selectionMode ? handleBubbleClick : undefined}
                    className={clsx(
                        "px-4 py-2 rounded-2xl shadow-sm relative transition-all min-w-[80px]",
                        "md:px-4 md:py-2.5 md:shadow-md md:rounded-2xl",
                        isMyMessage ? "text-white rounded-tr-none" : "text-gray-800 dark:text-gray-200 rounded-tl-none border",
                        isDeleted && "italic text-gray-500 bg-gray-100 dark:bg-gray-700 border-gray-200 dark:border-gray-600",
                        selectionMode && "cursor-pointer ring-2 ring-offset-2 ring-transparent",
                        selectionMode && isSelected && "ring-[var(--volera-accent)]"
                    )}
                    style={isDeleted ? undefined : isMyMessage
                        ? { background: 'var(--chat-bubble-me)' }
                        : { background: 'var(--chat-bubble-other)', borderColor: 'var(--chat-bubble-other-border)' }}
                >
                    {isDeleted ? (
                        <p className="text-sm">This message was deleted</p>
                    ) : (
                        <>
                            {message.forwardedFromMessageId && (
                                <div className="mb-1 text-[10px] uppercase tracking-wide font-semibold text-gray-400 dark:text-gray-500">
                                    Forwarded
                                </div>
                            )}
                            {message.replyToMessagePreview && (
                                <div className="mb-2 min-w-0 overflow-hidden px-3 py-2 rounded-xl text-xs bg-black/5 dark:bg-white/10 border border-black/5 dark:border-white/10">
                                    <div className="font-semibold mb-0.5 text-gray-700 dark:text-gray-200 truncate">
                                        {message.replyToMessagePreview.senderName || 'Unknown'}
                                    </div>
                                    <div className="text-gray-500 dark:text-gray-400 line-clamp-2 min-w-0">
                                        {message.replyToMessagePreview.deletedAt
                                            ? 'Original message was deleted'
                                            : message.replyToMessagePreview.contentSnippet}
                                    </div>
                                </div>
                            )}
                            {message.replyToStoryItemId && (
                                <div className="mb-2 min-w-0 overflow-hidden px-3 py-2 rounded-xl text-xs bg-black/5 dark:bg-white/10 border border-black/5 dark:border-white/10 flex gap-2">
                                    {message.replyToStoryItemPreview?.mediaUrl && (
                                      <img
                                        src={message.replyToStoryItemPreview.mediaUrl}
                                        alt=""
                                        className="w-10 h-10 rounded object-cover shrink-0"
                                      />
                                    )}
                                    <div className="min-w-0">
                                      <div className="font-semibold mb-0.5 text-gray-700 dark:text-gray-200 truncate">
                                        Story
                                        {message.replyToStoryItemPreview?.ownerName
                                          ? ` · ${message.replyToStoryItemPreview.ownerName}`
                                          : ''}
                                      </div>
                                      <div className="text-gray-500 dark:text-gray-400 line-clamp-2">
                                        {message.replyToStoryItemPreview?.overlaySnippet || 'Replied to a story'}
                                      </div>
                                    </div>
                                </div>
                            )}
                                {message.attachmentUrl && (
                                <div className="mb-2 relative">
                                    {isDownloading && !message.attachmentType?.startsWith('image/') && (
                                        <div className="absolute inset-0 bg-black/50 z-10 flex items-center justify-center rounded-lg">
                                            <CircularProgress 
                                                progress={transfer.progress} 
                                                color="#fff" 
                                                trackColor="rgba(255,255,255,0.3)" 
                                                size={50}
                                                icon={
                                                    <button onClick={handleCancelDownload} className="p-1 hover:text-red-400">
                                                        <XCircle size={20} />
                                                    </button>
                                                }
                                            />
                                        </div>
                                    )}
                                    {message.attachmentType?.startsWith('image/') ? (
                                        <ChatImage
                                            src={message.attachmentUrl}
                                            alt="Attachment"
                                            onClick={onImageClick ? () => onImageClick(message) : (e) => handleDownload(e)}
                                            showDownloadButton={!isDownloading}
                                            onDownloadClick={handleDownload}
                                        />
                                    ) : message.attachmentType?.startsWith('video/') ? (
                                        <LazyVideo
                                            src={message.attachmentUrl}
                                            className="rounded-lg max-w-full h-auto max-h-[300px]"
                                        />
                                    ) : message.attachmentType?.startsWith('audio/') ? (
                                        <LazyVoiceMessagePlayer
                                            src={message.attachmentUrl}
                                            isMyMessage={isMyMessage}
                                        />
                                    ) : (
                                        <div 
                                            onClick={handleDownload}
                                            className={clsx(
                                                "flex items-center gap-2 p-3 rounded-lg transition-colors cursor-pointer relative overflow-hidden min-h-[52px]",
                                                isMyMessage ? "bg-white/20 hover:bg-white/30" : "bg-gray-100 dark:bg-gray-600 hover:bg-gray-200 dark:hover:bg-gray-500"
                                            )}
                                        >
                                            {isDownloading && (
                                                <div className="absolute inset-0 bg-black/50 z-10 flex items-center justify-center gap-2 rounded-lg">
                                                    <CircularProgress
                                                        progress={transfer.progress}
                                                        color="#fff"
                                                        trackColor="rgba(255,255,255,0.3)"
                                                        size={40}
                                                        icon={
                                                            <button type="button" onClick={handleCancelDownload} className="p-0.5 hover:text-red-300">
                                                                <XCircle size={18} />
                                                            </button>
                                                        }
                                                    />
                                                    <span className="text-white text-sm font-medium">{transfer.progress}%</span>
                                                </div>
                                            )}
                                            <div className="relative z-0 flex items-center gap-2 w-full">
                                                <FileIcon size={20} className="flex-shrink-0" />
                                                <span className="text-sm underline truncate max-w-[150px]">
                                                    {message.attachmentUrl.split('/').pop()?.split('_').slice(1).join('_') || "File"}
                                                </span>
                                                {!isDownloading && <Download size={16} className="ml-auto opacity-50 flex-shrink-0" />}
                                            </div>
                                        </div>
                                    )}
                                </div>
                            )}
                            <p 
                                className={clsx(
                                    "whitespace-pre-wrap break-words text-sm md:text-base leading-relaxed select-text",
                                    isMessageRtl ? "text-right" : "text-left"
                                )}
                                dir={isMessageRtl ? "rtl" : "ltr"}
                            >
                                {(() => {
                                    const text = message.content || '';
                                    const parts = text.split(/(https?:\/\/[^\s<>"{}|\\^`[\]]+)/gi);
                                    return parts.map((part, i) =>
                                        /^https?:\/\//i.test(part) ? (
                                            <a
                                                key={i}
                                                href={part}
                                                target="_blank"
                                                rel="noopener noreferrer"
                                                onClick={(e) => e.stopPropagation()}
                                                className={clsx(
                                                    "underline break-all hover:opacity-80 transition-opacity",
                                                    isMyMessage ? "text-white/80" : "text-[var(--volera-accent)]"
                                                )}
                                            >
                                                {part}
                                            </a>
                                        ) : (
                                            part
                                        )
                                    );
                                })()}
                            </p>
                            {message.isEdited && (
                                <span className={clsx("text-[10px] block text-right mt-0.5", isMyMessage ? "text-white/70" : "text-gray-400")}>
                                    (edited)
                                </span>
                            )}
                            {message.reactions && message.reactions.length > 0 && (
                                <div className="mt-1 flex flex-wrap gap-1">
                                    {message.reactions.map((r, idx) => (
                                        <span
                                            key={`${r.userId}-${idx}`}
                                            className={clsx(
                                                "px-2 py-0.5 rounded-full text-xs border flex items-center gap-1 bg-white/40",
                                                isMyMessage ? "border-white/40 text-white/90" : "border-gray-200 text-gray-600"
                                            )}
                                        >
                                            <span>{r.emoji}</span>
                                            {r.userName && (
                                                <span className="max-w-[80px] truncate opacity-80">
                                                    {r.userName}
                                                </span>
                                            )}
                                        </span>
                                    ))}
                                </div>
                            )}
                        </>
                    )}
                    <div className={clsx("text-[10px] mt-1 text-right opacity-80 flex items-center justify-end gap-1 flex-wrap", isMyMessage ? "text-white/90" : "text-gray-400")}>
                        {message.signatureDisplayName && (
                          <span className="mr-auto opacity-90 italic">{message.signatureDisplayName}</span>
                        )}
                        {typeof message.viewCount === 'number' && message.viewCount > 0 && (
                          <span className="opacity-80" title="Views">{message.viewCount} views</span>
                        )}
                        {message.sendAsChannelName && (
                          <span className="opacity-90">{message.sendAsChannelName}</span>
                        )}
                        {message.isPinned && <span className="mr-1">📌</span>}
                        {new Date(message.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        {isMyMessage && (
                            (message.deliveryStatus === 'queued' || message.deliveryStatus === 'sending' || message.deliveryStatus === 'retrying')
                              ? <span className="opacity-70" title={message.deliveryStatus}>…</span>
                              : message.deliveryStatus === 'permanently_failed'
                              ? <span title="Failed to send"><XCircle size={14} className="text-red-200" /></span>
                              : message.isRead
                              ? <CheckCheck size={14} className="text-white/70" />
                              : <span title={message.deliveryStatus === 'accepted' ? 'Sent' : undefined}><Check size={14} /></span>
                        )}
                    </div>
                </div>

                {/* Actions Menu: overlay on mobile (long-press), inline strip on desktop */}
                {showActions && !isDeleted && !selectionMode && isMobile && (
                    <div
                        className="fixed inset-0 z-50 flex items-end sm:items-center justify-center pt-[env(safe-area-inset-top,0px)] pl-[env(safe-area-inset-left,0px)] pr-[env(safe-area-inset-right,0px)] pb-[env(safe-area-inset-bottom,0px)] sm:p-4 bg-black/50"
                        onClick={(e) => { e.stopPropagation(); setShowActions(false); }}
                        role="presentation"
                    >
                        <div
                            className="w-full sm:w-auto max-w-md animate-in fade-in slide-in-from-bottom-4 sm:zoom-in-95 duration-200 pb-[max(0.5rem,env(safe-area-inset-bottom,0px))] sm:pb-0"
                            onClick={(e) => e.stopPropagation()}
                        >
                            <div className="sm:hidden flex justify-center pb-2" aria-hidden>
                                <div className="w-10 h-1 rounded-full bg-white/40" />
                            </div>
                            <MessageActions
                                onEdit={() => { onEdit(message); setShowActions(false); }}
                                onDelete={() => { onDelete(message.id); setShowActions(false); }}
                                onSave={() => { onSave(message.id, !!message.isSaved); setShowActions(false); }}
                                onReply={onReply ? () => { onReply(message); setShowActions(false); } : undefined}
                                onReact={onReact ? (emoji) => { onReact(message, emoji); setShowActions(false); } : undefined}
                                onForward={onForward ? () => { onForward(message); setShowActions(false); } : undefined}
                                onTogglePin={onTogglePin ? () => { onTogglePin(message); setShowActions(false); } : undefined}
                                onCopyImage={hasImageAttachment && onCopyImage ? () => { onCopyImage(message); setShowActions(false); } : undefined}
                                onCopyText={hasTextContent ? () => { handleCopyText(); setShowActions(false); } : undefined}
                                onCopyLink={hasLinks ? () => { handleCopyLink(); setShowActions(false); } : undefined}
                                onSelect={onSelect ? () => { onSelect(message); setShowActions(false); } : undefined}
                                showSave={showSave}
                                hasImageAttachment={hasImageAttachment}
                                isMyMessage={isMyMessage}
                                isSaved={message.isSaved}
                                mobileLayout
                                isReactionPending={isReactionPending}
                                isSavePending={isSavePending}
                                isPinPending={isPinPending}
                            />
                        </div>
                    </div>
                )}
                {showActions && !isDeleted && !selectionMode && !isMobile && desktopActionsPos && createPortal(
                    <div
                        className="fixed z-[80] animate-in fade-in duration-150"
                        style={{
                            top: desktopActionsPos.top,
                            left: desktopActionsPos.left,
                            right: desktopActionsPos.right,
                            transform: desktopActionsPos.placeBelow ? undefined : 'translateY(-100%)',
                        }}
                        onMouseEnter={() => {
                            clearCloseActionsTimer();
                            setShowActions(true);
                        }}
                        onMouseLeave={scheduleCloseDesktopActions}
                    >
                        <MessageActions
                            onEdit={() => onEdit(message)}
                            onDelete={() => onDelete(message.id)}
                            onSave={() => onSave(message.id, !!message.isSaved)}
                            onReply={onReply ? () => onReply(message) : undefined}
                            onReact={onReact ? (emoji) => onReact(message, emoji) : undefined}
                            onForward={onForward ? () => onForward(message) : undefined}
                            onTogglePin={onTogglePin ? () => onTogglePin(message) : undefined}
                            onCopyImage={hasImageAttachment && onCopyImage ? () => onCopyImage(message) : undefined}
                            onCopyText={hasTextContent ? handleCopyText : undefined}
                            onCopyLink={hasLinks ? handleCopyLink : undefined}
                            onSelect={onSelect ? () => onSelect(message) : undefined}
                            showSave={showSave}
                            hasImageAttachment={hasImageAttachment}
                            isMyMessage={isMyMessage}
                            isSaved={message.isSaved}
                            isReactionPending={isReactionPending}
                            isSavePending={isSavePending}
                            isPinPending={isPinPending}
                        />
                    </div>,
                    document.body
                )}
            </div>
        </div>
    );
};
