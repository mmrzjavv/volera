import React from 'react';
import { Pencil, Trash2, Bookmark, BookmarkCheck, Reply, CornerDownRight, Pin, Copy, CheckCircle, Link2 } from 'lucide-react';

interface MessageActionsProps {
    onEdit: () => void;
    onDelete: () => void;
    onSave: () => void;
    onReply?: () => void;
    onReact?: (emoji: string) => void;
    onForward?: () => void;
    onTogglePin?: () => void;
    onCopyImage?: () => void;
    onCopyText?: () => void;
    onCopyLink?: () => void;
    onSelect?: () => void;
    isMyMessage: boolean;
    isSaved?: boolean;
    /** Hide Save button (e.g. in user-to-user chat) */
    showSave?: boolean;
    /** When true, use column layout and larger touch targets (for mobile overlay) */
    mobileLayout?: boolean;
    isReactionPending?: boolean;
    isSavePending?: boolean;
    isPinPending?: boolean;
    hasImageAttachment?: boolean;
}

const QUICK_EMOJIS = ['👍', '❤️', '😂', '😮', '😢'];

const iconSize = 20;
const iconSizeCompact = 14;

export const MessageActions: React.FC<MessageActionsProps> = ({
    onEdit, onDelete, onSave, onReply, onReact, onForward, onTogglePin, onCopyImage, onCopyText, onCopyLink, onSelect,
    isMyMessage, isSaved, showSave = true, mobileLayout = false, isReactionPending, isSavePending, isPinPending,
    hasImageAttachment = false,
}) => {
    const isCol = mobileLayout;
    const containerClass = isCol
        ? 'flex flex-col gap-0.5 bg-white dark:bg-gray-800 shadow-lg border border-gray-200 dark:border-gray-600 rounded-2xl p-2 min-w-[200px]'
        : 'flex items-center gap-0.5 bg-white/95 dark:bg-gray-800/95 backdrop-blur-md shadow-lg border border-gray-200/80 dark:border-gray-600/80 rounded-full px-2 py-1.5';

    const btnBase = isCol
        ? 'flex items-center gap-3 w-full px-4 py-3 min-h-[48px] rounded-xl text-left transition-colors'
        : 'p-2 rounded-full transition-all duration-150 hover:scale-105 active:scale-95';
    const size = isCol ? iconSize : iconSizeCompact;

    return (
        <div className={containerClass}>
            {showSave && (
            <button
                onClick={(e) => { e.stopPropagation(); onSave(); }}
                disabled={isSavePending}
                className={`${btnBase} ${isSaved ? 'text-yellow-500 bg-yellow-50 dark:bg-yellow-900/30 hover:bg-yellow-100 dark:hover:bg-yellow-900/50' : 'text-gray-500 dark:text-gray-400 hover:text-yellow-600 hover:bg-yellow-50 dark:hover:bg-yellow-900/20'} disabled:opacity-50 disabled:pointer-events-none`}
                title={isSaved ? 'Unsave' : 'Save'}
            >
                {isSaved ? <BookmarkCheck size={size} className="flex-shrink-0" /> : <Bookmark size={size} className="flex-shrink-0" />}
                {isCol && <span className="text-sm font-medium">{isSaved ? 'Unsave' : 'Save'}</span>}
            </button>
            )}
            {onReact && (
                <div className={isCol ? 'flex items-center gap-2 px-4 py-2' : 'flex items-center gap-0.5 px-1'}>
                    {QUICK_EMOJIS.map((emoji) => (
                        <button
                            key={emoji}
                            onClick={(ev) => { ev.stopPropagation(); onReact(emoji); }}
                            disabled={isReactionPending}
                            className={`${isCol
                                ? 'min-w-[44px] min-h-[44px] flex items-center justify-center rounded-xl text-xl hover:bg-gray-100 dark:hover:bg-gray-700 active:scale-95 transition-transform'
                                : 'px-1.5 py-0.5 rounded-full text-base hover:bg-gray-100 dark:hover:bg-gray-700 active:bg-gray-200 transition-colors'} disabled:opacity-50 disabled:pointer-events-none`}
                            title={`React with ${emoji}`}
                        >
                            {emoji}
                        </button>
                    ))}
                </div>
            )}
            {onReply && (
                <button
                    onClick={(e) => { e.stopPropagation(); onReply(); }}
                    className={`${btnBase} text-gray-500 dark:text-gray-400 hover:text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20`}
                    title="Reply"
                >
                    <Reply size={size} className="flex-shrink-0" />
                    {isCol && <span className="text-sm font-medium">Reply</span>}
                </button>
            )}
            {onForward && (
                <button
                    onClick={(e) => { e.stopPropagation(); onForward(); }}
                    className={`${btnBase} text-gray-500 dark:text-gray-400 hover:text-purple-600 hover:bg-purple-50 dark:hover:bg-purple-900/20`}
                    title="Forward"
                >
                    <CornerDownRight size={size} className="flex-shrink-0" />
                    {isCol && <span className="text-sm font-medium">Forward</span>}
                </button>
            )}
            {onTogglePin && (
                <button
                    onClick={(e) => { e.stopPropagation(); onTogglePin(); }}
                    disabled={isPinPending}
                    className={`${btnBase} text-gray-500 dark:text-gray-400 hover:text-orange-600 hover:bg-orange-50 dark:hover:bg-orange-900/20 disabled:opacity-50 disabled:pointer-events-none`}
                    title="Pin / Unpin"
                >
                    <Pin size={size} className="flex-shrink-0" />
                    {isCol && <span className="text-sm font-medium">Pin</span>}
                </button>
            )}
            {hasImageAttachment && onCopyImage && (
                <button
                    onClick={(e) => { e.stopPropagation(); onCopyImage(); }}
                    className={`${btnBase} text-gray-500 dark:text-gray-400 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20`}
                    title="Copy image"
                >
                    <Copy size={size} className="flex-shrink-0" />
                    {isCol && <span className="text-sm font-medium">Copy image</span>}
                </button>
            )}
            {onCopyText && (
                <button
                    onClick={(e) => { e.stopPropagation(); onCopyText(); }}
                    className={`${btnBase} text-gray-500 dark:text-gray-400 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20`}
                    title="Copy text"
                >
                    <Copy size={size} className="flex-shrink-0" />
                    {isCol && <span className="text-sm font-medium">Copy text</span>}
                </button>
            )}
            {onCopyLink && (
                <button
                    onClick={(e) => { e.stopPropagation(); onCopyLink(); }}
                    className={`${btnBase} text-gray-500 dark:text-gray-400 hover:text-teal-600 hover:bg-teal-50 dark:hover:bg-teal-900/20`}
                    title="Copy link"
                >
                    <Link2 size={size} className="flex-shrink-0" />
                    {isCol && <span className="text-sm font-medium">Copy link</span>}
                </button>
            )}
            {onSelect && (
                <button
                    onClick={(e) => { e.stopPropagation(); onSelect(); }}
                    className={`${btnBase} text-gray-500 dark:text-gray-400 hover:text-indigo-600 hover:bg-indigo-50 dark:hover:bg-indigo-900/20`}
                    title="Select"
                >
                    <CheckCircle size={size} className="flex-shrink-0" />
                    {isCol && <span className="text-sm font-medium">Select</span>}
                </button>
            )}
            {isMyMessage && (
                <>
                    <button
                        onClick={(e) => { e.stopPropagation(); onEdit(); }}
                        className={`${btnBase} text-gray-500 dark:text-gray-400 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20`}
                        title="Edit"
                    >
                        <Pencil size={size} className="flex-shrink-0" />
                        {isCol && <span className="text-sm font-medium">Edit</span>}
                    </button>
                    <button
                        onClick={(e) => { e.stopPropagation(); onDelete(); }}
                        className={`${btnBase} text-gray-500 dark:text-gray-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20`}
                        title="Delete"
                    >
                        <Trash2 size={size} className="flex-shrink-0" />
                        {isCol && <span className="text-sm font-medium">Delete</span>}
                    </button>
                </>
            )}
        </div>
    );
};
