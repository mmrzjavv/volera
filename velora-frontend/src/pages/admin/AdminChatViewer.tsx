import React, { useEffect, useState, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { adminApi, type AdminChatMessageDto, type AdminConversationResult, type AdminChatDto } from '../../services/adminApi';
import { ArrowLeft, Trash2, Upload, Users } from 'lucide-react';
import { useConfirmationStore } from '../../store/useConfirmationStore';

function askConfirm(title: string, message: string): Promise<boolean> {
  return new Promise((resolve) => {
    useConfirmationStore.getState().openDialog({
      title,
      message,
      variant: 'danger',
      onConfirm: () => resolve(true),
      onCancel: () => resolve(false),
    });
  });
}

export const AdminChatViewer: React.FC = () => {
  const { key } = useParams<{ key: string }>();
  const navigate = useNavigate();
  const [chatMeta, setChatMeta] = useState<AdminChatDto | null>(null);
  const [data, setData] = useState<AdminConversationResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [uploadingPicture, setUploadingPicture] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const loadMessages = async (before?: string) => {
    if (!key) return;
    if (before) setLoadingMore(true);
    else setLoading(true);
    try {
      const result = await adminApi.getConversationMessages(key, 50, before);
      if (result) {
        setData((prev) => {
          if (!prev) return result;
          if (before) {
            return {
              ...result,
              messages: [...result.messages, ...prev.messages],
            };
          }
          return result;
        });
      }
    } finally {
      setLoading(false);
      setLoadingMore(false);
    }
  };

  useEffect(() => {
    if (key) {
      loadMessages();
      adminApi.getChatByKey(key).then(setChatMeta).catch(() => setChatMeta(null));
    } else {
      setChatMeta(null);
    }
  }, [key]);

  const loadOlder = () => {
    if (!data?.hasMore || !data.messages.length || loadingMore) return;
    const oldest = data.messages[0]?.sentAt;
    if (oldest) loadMessages(oldest);
  };

  const handlePurge = async () => {
    if (!key) return;
    const ok = await askConfirm('Purge conversation', 'Delete ALL messages in this conversation? This cannot be undone.');
    if (!ok) return;
    try {
      const deleted = await adminApi.purgeConversation(key);
      if (deleted > 0) {
        setData((prev) => prev ? { ...prev, messages: [], hasMore: false } : null);
      }
    } catch (e) {
      console.error(e);
    }
  };

  const isGroup = chatMeta?.type === 'Group' && chatMeta?.groupId;
  const groupId = chatMeta?.groupId;
  const groupProfilePictureUrl = chatMeta?.groupProfilePictureUrl ?? null;

  const handleGroupProfilePictureChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !groupId) return;
    setUploadingPicture(true);
    e.target.value = '';
    try {
      const url = await adminApi.uploadGroupProfilePicture(groupId, file);
      setChatMeta((prev) => (prev ? { ...prev, groupProfilePictureUrl: url } : null));
    } catch (err) {
      console.error(err);
    } finally {
      setUploadingPicture(false);
    }
  };

  if (!key) return <div className="text-slate-400">Invalid conversation key.</div>;

  return (
    <div className="flex flex-col min-h-0 flex-1 md:h-[calc(100vh-8rem)]">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-4 gap-2">
        <div className="flex items-center justify-between sm:justify-start gap-2 min-w-0">
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="flex items-center gap-2 text-slate-400 hover:text-white py-2 pr-2 -ml-2 rounded-lg hover:bg-slate-800 min-h-[44px] touch-manipulation shrink-0"
          >
            <ArrowLeft size={18} /> Back
          </button>
          <h1 className="text-lg sm:text-xl font-bold truncate min-w-0 flex-1 sm:flex-initial sm:max-w-md">{data?.conversationTitle ?? chatMeta?.groupName ?? key}</h1>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          {isGroup && (
            <>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                className="hidden"
                onChange={handleGroupProfilePictureChange}
                disabled={uploadingPicture}
              />
              <button
                type="button"
                onClick={() => fileInputRef.current?.click()}
                disabled={uploadingPicture}
                className="flex items-center gap-2 px-3 py-2.5 text-teal-400 hover:bg-teal-500/20 rounded-lg disabled:opacity-50 min-h-[44px] touch-manipulation"
              >
                {uploadingPicture ? (
                  <span className="text-sm">Uploading...</span>
                ) : (
                  <>
                    <Upload size={16} /> Group picture
                  </>
                )}
              </button>
            </>
          )}
          <button
            type="button"
            onClick={handlePurge}
            className="flex items-center gap-2 px-3 py-2.5 text-red-400 hover:bg-red-500/20 rounded-lg min-h-[44px] touch-manipulation"
          >
            <Trash2 size={16} /> Purge
          </button>
        </div>
      </div>

      {isGroup && (
        <div className="mb-4 p-4 bg-slate-800 rounded-xl border border-slate-700 flex items-center gap-4">
          <div className="w-16 h-16 rounded-full bg-slate-700 flex items-center justify-center overflow-hidden shrink-0">
            {groupProfilePictureUrl ? (
              <img src={groupProfilePictureUrl} alt={chatMeta?.groupName ?? 'Group'} className="w-full h-full object-cover" />
            ) : (
              <Users size={28} className="text-slate-500" />
            )}
          </div>
          <div className="min-w-0">
            <h2 className="font-semibold text-white truncate">{chatMeta?.groupName ?? 'Group'}</h2>
            <p className="text-sm text-slate-400">Group profile picture (stored in S3). Use the button above to upload.</p>
          </div>
        </div>
      )}

      <div
        ref={containerRef}
        className="flex-1 min-h-[40vh] md:min-h-0 bg-slate-900 rounded-xl border border-slate-800 overflow-y-auto flex flex-col-reverse p-4"
      >
        {loading ? (
          <div className="text-slate-400 text-center py-8">Loading...</div>
        ) : (
          <>
            {loadingMore && <div className="text-slate-400 text-center py-2 text-sm">Loading older...</div>}
            <button
              type="button"
              onClick={loadOlder}
              disabled={!data?.hasMore || loadingMore}
              className="mb-2 py-2.5 text-slate-400 hover:text-white text-sm disabled:opacity-50 min-h-[44px] touch-manipulation"
            >
              {data?.hasMore ? 'Load older messages' : 'No more messages'}
            </button>
            <div className="space-y-3">
              {data?.messages.map((m) => (
                <ChatMessageBubble key={m.id} message={m} />
              ))}
            </div>
            <div ref={messagesEndRef} />
          </>
        )}
      </div>
    </div>
  );
};

function ChatMessageBubble({ message }: { message: AdminChatMessageDto }) {
  const name = [message.senderFirstName, message.senderLastName].filter(Boolean).join(' ') || message.senderUsername;
  const isDeleted = !!message.deletedAt;
  return (
    <div className="flex flex-col max-w-[85%] min-w-0">
      <span className="text-xs text-slate-500 mb-0.5 truncate">{name} • {new Date(message.sentAt).toLocaleString()}</span>
      <div className={`rounded-xl px-4 py-2 break-words ${isDeleted ? 'bg-slate-800 text-slate-500 italic' : 'bg-teal-600/30 text-white'}`}>
        {isDeleted ? '[deleted]' : message.content}
        {message.attachmentUrl && (
          <a href={message.attachmentUrl} target="_blank" rel="noopener noreferrer" className="block mt-1 text-teal-300 text-sm truncate">
            Attachment
          </a>
        )}
        {message.isEdited && <span className="text-xs text-slate-400 ml-1">(edited)</span>}
      </div>
    </div>
  );
}
