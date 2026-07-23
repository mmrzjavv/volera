import React, { useEffect } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../../store/useAuthStore';

const ADMIN_ROLES = ['Admin', 'Moderator', 'SuperAdmin'];

export const AdminRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, user, checkAuth } = useAuthStore();
  const location = useLocation();
  const [checked, setChecked] = React.useState(false);

  useEffect(() => {
    void checkAuth().then(() => setChecked(true));
  }, [checkAuth]);

  if (!checked) {
    return <div className="min-h-screen bg-slate-950 flex items-center justify-center text-slate-400">Loading...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/admin/login" state={{ from: location }} replace />;
  }

  const role = (user as { role?: string } | null)?.role;
  if (!role || !ADMIN_ROLES.includes(role)) {
    return <Navigate to="/admin/login" state={{ from: location, denied: true }} replace />;
  }

  return <>{children}</>;
};
