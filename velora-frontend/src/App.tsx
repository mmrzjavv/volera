import React, { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from './store/useAuthStore';
import { useThemeStore, syncThemeToDom } from './store/useThemeStore';
import { syncLocaleToDom, useLocaleStore } from './store/useLocaleStore';
import { useVersionStore } from './store/useVersionStore';
import { UpdateBanner } from './components/UpdateBanner';
import { Login } from './pages/Login';
import { Register } from './pages/Register';
import { Chat } from './pages/Chat';
import { Invite } from './pages/Invite';
import { PublicChannelPage } from './pages/PublicChannelPage';
import { Profile } from './pages/Profile';
import AdminMessages from './pages/AdminMessages';
import { ToastContainer } from './components/ui/ToastContainer';
import { ConfirmationDialog } from './components/ConfirmationDialog';
import { ensurePushSubscription } from './utils/push';
import { AdminRoute } from './components/admin/AdminRoute';
import { AdminLayout } from './components/admin/AdminLayout';
import { AdminLogin } from './pages/admin/AdminLogin';
import { AdminDashboard } from './pages/admin/AdminDashboard';
import { AdminUsers } from './pages/admin/AdminUsers';
import { AdminUserDetail } from './pages/admin/AdminUserDetail';
import { AdminChats } from './pages/admin/AdminChats';
import { AdminChatViewer } from './pages/admin/AdminChatViewer';
import { AdminMessageSearch } from './pages/admin/AdminMessageSearch';
import { AdminLimits } from './pages/admin/AdminLimits';
import { AdminAppVersion } from './pages/admin/AdminAppVersion';
import { AdminUserUsage } from './pages/admin/AdminUserUsage';
import { AdminMonitoring } from './pages/admin/AdminMonitoring';
import { AdminAudit } from './pages/admin/AdminAudit';
import { AdminErrors } from './pages/admin/AdminErrors';
import { NotificationClickHandler } from './components/NotificationClickHandler';
import { InAppNotificationBanner } from './components/InAppNotificationBanner';
import { OfflineBanner } from './components/OfflineBanner';
import { InstallBanner } from './components/InstallBanner';

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuthStore();

  // Ensure push is subscribed on every private page so 1:1 notifications work even if user didn't land on Chat first
  useEffect(() => {
    if (!isAuthenticated) return;
    ensurePushSubscription();
  }, [isAuthenticated]);

  return isAuthenticated ? <>{children}</> : <Navigate to="/login" />;
}

function App() {
  const { checkAuth } = useAuthStore();
  const theme = useThemeStore((s) => s.theme);
  const chatColorTemplate = useThemeStore((s) => s.chatColorTemplate);
  const locale = useLocaleStore((s) => s.locale);

  useEffect(() => {
    checkAuth();
  }, [checkAuth]);

  useEffect(() => {
    syncThemeToDom();
  }, [theme, chatColorTemplate]);

  useEffect(() => {
    syncLocaleToDom();
  }, [locale]);

  useEffect(() => {
    const t = setTimeout(() => syncThemeToDom(), 50);
    return () => clearTimeout(t);
  }, []);

  // Check for app update (server version vs client) so we can show "Reload to update" banner
  useEffect(() => {
    const t = setTimeout(() => {
      useVersionStore.getState().checkForUpdate();
    }, 2000);
    return () => clearTimeout(t);
  }, []);

  return (
    <BrowserRouter>
      <OfflineBanner />
      <NotificationClickHandler />
      <InAppNotificationBanner />
      <UpdateBanner />
      <InstallBanner />
      <ToastContainer />
      <ConfirmationDialog />
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/invite/:inviteCode" element={<Invite />} />
        <Route path="/c/:username" element={<PublicChannelPage />} />
        <Route
          path="/"
          element={
            <PrivateRoute>
              <Chat />
            </PrivateRoute>
          }
        />
        <Route
          path="/profile"
          element={
            <PrivateRoute>
              <Profile />
            </PrivateRoute>
          }
        />
        <Route path="/admin/login" element={<AdminLogin />} />
        <Route
          path="/admin"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminDashboard />
              </AdminLayout>
            </AdminRoute>
          }
        />
        <Route
          path="/admin/users"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminUsers />
              </AdminLayout>
            </AdminRoute>
          }
        />
        <Route
          path="/admin/users/:id"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminUserDetail />
              </AdminLayout>
            </AdminRoute>
          }
        />
        <Route
          path="/admin/chats"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminChats />
              </AdminLayout>
            </AdminRoute>
          }
        />
        <Route
          path="/admin/chats/:key"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminChatViewer />
              </AdminLayout>
            </AdminRoute>
          }
        />
        <Route
          path="/admin/messages"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminMessageSearch />
              </AdminLayout>
            </AdminRoute>
          }
        />
        <Route
          path="/admin/system-messages"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminMessages />
              </AdminLayout>
            </AdminRoute>
          }
        />
        <Route
          path="/admin/limits"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminLimits />
              </AdminLayout>
            </AdminRoute>
          }
        />
        <Route
          path="/admin/version"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminAppVersion />
              </AdminLayout>
            </AdminRoute>
          }
        />
        <Route
          path="/admin/monitoring"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminMonitoring />
              </AdminLayout>
            </AdminRoute>
          }
        />
        <Route
          path="/admin/usage"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminUserUsage />
              </AdminLayout>
            </AdminRoute>
          }
        />
        <Route
          path="/admin/errors"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminErrors />
              </AdminLayout>
            </AdminRoute>
          }
        />
        <Route
          path="/admin/audit"
          element={
            <AdminRoute>
              <AdminLayout>
                <AdminAudit />
              </AdminLayout>
            </AdminRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
