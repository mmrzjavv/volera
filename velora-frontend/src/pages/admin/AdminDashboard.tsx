import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { adminApi, type ExtendedMonitoringStatsDto } from '../../services/adminApi';
import { Users, MessageSquare, UsersRound, Activity, UserPlus } from 'lucide-react';

export const AdminDashboard: React.FC = () => {
  const [stats, setStats] = useState<ExtendedMonitoringStatsDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    adminApi.getExtendedStats().then(setStats).finally(() => setLoading(false));
  }, []);

  if (loading || !stats) {
    return <div className="text-slate-400">Loading...</div>;
  }

  const cards = [
    { label: 'Total Users', value: stats.totalUsers, icon: Users, path: '/admin/users', color: 'bg-teal-600' },
    { label: 'Total Messages', value: stats.totalMessages, icon: MessageSquare, path: '/admin/messages', color: 'bg-emerald-600' },
    { label: 'Total Groups', value: stats.totalGroups, icon: UsersRound, path: '/admin/chats', color: 'bg-amber-600' },
    { label: 'Online Now', value: stats.onlineUsersCount, icon: Activity, path: '/admin/monitoring', color: 'bg-cyan-600' },
    { label: 'New Users (24h)', value: stats.newUsersLast24h, icon: UserPlus, path: '/admin/users', color: 'bg-violet-600' },
    { label: 'Msgs (24h)', value: stats.messagesLast24h, icon: MessageSquare, path: '/admin/messages', color: 'bg-teal-600' },
  ];

  return (
    <div>
      <h1 className="text-xl sm:text-2xl font-bold mb-4 sm:mb-6">Dashboard</h1>
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3 sm:gap-4">
        {cards.map(({ label, value, icon: Icon, path, color }) => (
          <Link
            key={path}
            to={path}
            className="flex items-center gap-4 p-4 bg-slate-900 rounded-xl border border-slate-800 hover:border-slate-700 transition-colors min-h-[80px] touch-manipulation"
          >
            <div className={`p-2 rounded-lg ${color}`}>
              <Icon size={24} className="text-white" />
            </div>
            <div>
              <p className="text-slate-400 text-sm">{label}</p>
              <p className="text-2xl font-bold">{value.toLocaleString()}</p>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
};
