import React, { useState, useEffect } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { authService } from '../../services/api';
import { useAuthStore } from '../../store/useAuthStore';
import { LayoutDashboard, Users, MessageSquare, FileText, Settings, Activity, ScrollText, LogOut, Megaphone, BarChart2, AlertTriangle, Tag, Menu, X } from 'lucide-react';

const adminNav = [
  { path: '/admin', label: 'Dashboard', icon: LayoutDashboard },
  { path: '/admin/users', label: 'Users', icon: Users },
  { path: '/admin/chats', label: 'Chats', icon: MessageSquare },
  { path: '/admin/messages', label: 'Message Search', icon: FileText },
  { path: '/admin/system-messages', label: 'System Messages', icon: Megaphone },
  { path: '/admin/errors', label: 'Error Logs', icon: AlertTriangle },
  { path: '/admin/limits', label: 'Limits', icon: Settings },
  { path: '/admin/version', label: 'App Version', icon: Tag },
  { path: '/admin/monitoring', label: 'Monitoring', icon: Activity },
  { path: '/admin/usage', label: 'User Usage', icon: BarChart2 },
  { path: '/admin/audit', label: 'Audit Log', icon: ScrollText },
];

export const AdminLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  // Close mobile menu on route change (e.g. after clicking a nav link)
  useEffect(() => {
    setMobileMenuOpen(false);
  }, [location.pathname]);

  // Prevent body scroll when mobile drawer is open
  useEffect(() => {
    if (mobileMenuOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
    return () => {
      document.body.style.overflow = '';
    };
  }, [mobileMenuOpen]);

  const handleLogout = () => {
    logout();
    authService.logout();
    navigate('/admin/login');
    setMobileMenuOpen(false);
  };

  const navLinkClass = (path: string) =>
    `flex items-center gap-3 px-4 py-3 rounded-lg text-sm transition-colors min-h-[44px] touch-manipulation ${
      location.pathname === path || (path !== '/admin' && location.pathname.startsWith(path))
        ? 'bg-teal-600 text-white'
        : 'text-slate-400 hover:bg-slate-800 hover:text-slate-200'
    }`;

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col md:flex-row">
      {/* Mobile header with hamburger */}
      <header className="md:hidden flex items-center justify-between px-4 py-3 pt-[max(0.75rem,env(safe-area-inset-top))] bg-slate-900 border-b border-slate-800 shrink-0">
        <button
          type="button"
          onClick={() => setMobileMenuOpen(true)}
          className="p-2 -ml-2 rounded-lg text-slate-400 hover:bg-slate-800 hover:text-white min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation"
          aria-label="Open menu"
        >
          <Menu size={24} />
        </button>
        <h1 className="font-bold text-lg">Volera Admin</h1>
        <div className="w-10" aria-hidden />
      </header>

      {/* Mobile drawer overlay */}
      {mobileMenuOpen && (
        <div
          className="fixed inset-0 z-50 md:hidden bg-black/60"
          onClick={() => setMobileMenuOpen(false)}
          aria-hidden
        />
      )}

      {/* Sidebar: hidden on mobile, drawer when open */}
      <aside
        className={`
          fixed inset-y-0 left-0 z-50 w-72 max-w-[85vw] bg-slate-900 border-r border-slate-800 flex flex-col pl-[env(safe-area-inset-left)]
          transform transition-transform duration-200 ease-out
          md:static md:transform-none md:translate-x-0 md:w-64 md:max-w-none md:shrink-0 md:pl-0
          ${mobileMenuOpen ? 'translate-x-0' : '-translate-x-full'}
        `}
      >
        <div className="flex items-center justify-between p-4 pt-[max(1rem,env(safe-area-inset-top))] border-b border-slate-800 md:justify-start md:pt-4">
          <div>
            <h1 className="font-bold text-lg">Volera Admin</h1>
            <p className="text-xs text-slate-400 mt-1">{user?.username}</p>
          </div>
          <button
            type="button"
            onClick={() => setMobileMenuOpen(false)}
            className="md:hidden p-2 rounded-lg text-slate-400 hover:bg-slate-800 hover:text-white min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation"
            aria-label="Close menu"
          >
            <X size={22} />
          </button>
        </div>
        <nav className="flex-1 p-2 space-y-1 overflow-y-auto overflow-x-hidden">
          {adminNav.map(({ path, label, icon: Icon }) => (
            <Link
              key={path}
              to={path}
              className={navLinkClass(path)}
            >
              <Icon size={20} className="shrink-0" />
              <span>{label}</span>
            </Link>
          ))}
        </nav>
        <div className="p-2 border-t border-slate-800 space-y-1">
          <Link
            to="/"
            className="flex items-center gap-3 px-4 py-3 rounded-lg text-sm text-slate-400 hover:bg-slate-800 hover:text-slate-200 min-h-[44px] touch-manipulation"
            onClick={() => setMobileMenuOpen(false)}
          >
            ← Back to App
          </Link>
          <button
            type="button"
            onClick={handleLogout}
            className="w-full flex items-center gap-3 px-4 py-3 rounded-lg text-sm text-slate-400 hover:bg-slate-800 hover:text-red-400 min-h-[44px] touch-manipulation"
          >
            <LogOut size={20} className="shrink-0" />
            Logout
          </button>
        </div>
      </aside>

      <main className="flex-1 overflow-auto p-4 md:p-6 min-w-0 pl-[max(1rem,env(safe-area-inset-left))] pr-[max(1rem,env(safe-area-inset-right))] max-w-7xl mx-auto w-full">{children}</main>
    </div>
  );
};
