import { useToastStore } from '../../store/useToastStore';
import { X, CheckCircle, AlertCircle, Info, AlertTriangle } from 'lucide-react';
import { clsx } from 'clsx';

const icons = {
  success: <CheckCircle size={18} className="text-green-500 shrink-0" />,
  error: <AlertCircle size={18} className="text-red-500 shrink-0" />,
  info: <Info size={18} className="text-blue-500 shrink-0" />,
  warning: <AlertTriangle size={18} className="text-yellow-500 shrink-0" />,
};

export const ToastContainer = () => {
  const { toasts, removeToast } = useToastStore();

  return (
    <div 
      className="fixed left-1/2 -translate-x-1/2 top-0 z-[100] flex flex-col gap-2 pointer-events-none items-center w-full max-w-sm px-4 pt-[max(0.75rem,env(safe-area-inset-top))]"
    >
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={clsx(
            "pointer-events-auto flex items-center justify-between gap-3 px-3 py-2.5 rounded-xl shadow-md transition-all animate-in slide-in-from-top-4 fade-in duration-200 min-w-[180px] max-w-full",
            "bg-gray-800/95 backdrop-blur-sm text-white border border-gray-700/50"
          )}
        >
          <div className="flex items-center gap-2.5 min-w-0 flex-1">
            {icons[toast.type]}
            <p className="text-xs font-medium line-clamp-2 min-w-0 break-words">{toast.message}</p>
          </div>

          <div className="flex items-center gap-2 shrink-0">
            {toast.action && (
              <button
                onClick={() => {
                  toast.action?.onClick();
                  removeToast(toast.id);
                }}
                className="text-yellow-400 hover:text-yellow-300 text-xs font-semibold uppercase tracking-wide transition-colors"
              >
                {toast.action.label}
              </button>
            )}
            <button
              onClick={() => removeToast(toast.id)}
              className="shrink-0 text-gray-500 hover:text-white transition-colors p-0.5 -m-0.5 rounded"
            >
              <X size={14} />
            </button>
          </div>
        </div>
      ))}
    </div>
  );
};
