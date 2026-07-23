import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { adminApi, type ExtendedMonitoringStatsDto, type AdminUserListDto, type MessagesPerDayDto, type MostActiveUserDto, type MostActiveGroupDto, type TableRowCountsDto, type PaginatedResult } from '../../services/adminApi';
import { Users, MessageSquare, UsersRound, AlertTriangle, Activity, UserPlus, Clock, ChevronLeft, ChevronRight } from 'lucide-react';

const colorMap: Record<string, string> = {
  emerald: 'text-emerald-500',
  amber: 'text-amber-500',
  rose: 'text-rose-500',
  cyan: 'text-cyan-500',
  violet: 'text-violet-500',
  teal: 'text-teal-500',
  orange: 'text-orange-500',
  blue: 'text-blue-500',
};

function StatCard({ icon: Icon, label, value, color }: { icon: any; label: string; value: number; color: string }) {
  return (
    <div className="flex items-center gap-4 p-4 bg-slate-900 rounded-xl border border-slate-800 min-w-0">
      <Icon className={`${colorMap[color] || 'text-slate-500'} shrink-0`} size={28} />
      <div className="min-w-0 flex-1">
        <p className="text-slate-400 text-xs truncate">{label}</p>
        <p className="text-xl font-bold break-all">{value.toLocaleString()}</p>
      </div>
    </div>
  );
}

export const AdminMonitoring: React.FC = () => {
  const [stats, setStats] = useState<ExtendedMonitoringStatsDto | null>(null);
  const [overLimitResult, setOverLimitResult] = useState<PaginatedResult<AdminUserListDto> | null>(null);
  const [limitKey, setLimitKey] = useState('MaxSavedMessagesCount');
  const [overLimitPage, setOverLimitPage] = useState(1);
  const [mostActiveUsers, setMostActiveUsers] = useState<MostActiveUserDto[]>([]);
  const [mostActiveGroups, setMostActiveGroups] = useState<MostActiveGroupDto[]>([]);
  const [tableCounts, setTableCounts] = useState<TableRowCountsDto | null>(null);
  const [messagesPerDay, setMessagesPerDay] = useState<MessagesPerDayDto[]>([]);

  useEffect(() => {
    adminApi.getExtendedStats().then(setStats);
  }, []);

  useEffect(() => {
    setOverLimitPage(1);
  }, [limitKey]);

  useEffect(() => {
    adminApi.getUsersOverLimit(limitKey, overLimitPage, 20).then(setOverLimitResult);
  }, [limitKey, overLimitPage]);


  useEffect(() => {
    adminApi.getMostActiveUsers(1, 10).then((r) => setMostActiveUsers(r.items));
  }, []);

  useEffect(() => {
    adminApi.getMostActiveGroups(1, 10).then((r) => setMostActiveGroups(r.items));
  }, []);

  useEffect(() => {
    adminApi.getTableRowCounts().then(setTableCounts);
  }, []);

  useEffect(() => {
    adminApi.getMessagesPerDay(14).then(setMessagesPerDay);
  }, []);

  if (!stats) return <div className="text-slate-400">Loading...</div>;

  return (
    <div>
      <h1 className="text-xl sm:text-2xl font-bold mb-4 sm:mb-6">Monitoring</h1>
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3 sm:gap-4 mb-6 sm:mb-8">
        <StatCard icon={Users} label="Total Users" value={stats.totalUsers} color="teal" />
        <StatCard icon={MessageSquare} label="Total Messages" value={stats.totalMessages} color="emerald" />
        <StatCard icon={UsersRound} label="Total Groups" value={stats.totalGroups} color="amber" />
        <StatCard icon={Activity} label="Online Now" value={stats.onlineUsersCount} color="cyan" />
        <StatCard icon={UserPlus} label="New (24h)" value={stats.newUsersLast24h} color="violet" />
        <StatCard icon={MessageSquare} label="Msgs (24h)" value={stats.messagesLast24h} color="teal" />
        <StatCard icon={AlertTriangle} label="Disabled" value={stats.disabledUsersCount} color="rose" />
        <StatCard icon={Clock} label="Suspended" value={stats.suspendedUsersCount} color="orange" />
        <StatCard icon={MessageSquare} label="Unread" value={stats.unreadMessagesCount} color="blue" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-6">
          <h2 className="font-semibold mb-4">Most Active Users (Top 10)</h2>
          <ul className="space-y-2">
            {mostActiveUsers.map((u) => (
              <li key={u.userId} className="flex justify-between">
                <Link to={`/admin/users/${u.userId}`} className="text-teal-400 hover:underline">{u.username}</Link>
                <span className="text-slate-400">{u.messageCount} msgs</span>
              </li>
            ))}
            {mostActiveUsers.length === 0 && <li className="text-slate-500">No data</li>}
          </ul>
        </div>
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-6">
          <h2 className="font-semibold mb-4">Most Active Groups (Top 10)</h2>
          <ul className="space-y-2">
            {mostActiveGroups.map((g) => (
              <li key={g.groupId} className="flex justify-between">
                <Link to={`/admin/chats/group_${g.groupId}`} className="text-teal-400 hover:underline">{g.groupName}</Link>
                <span className="text-slate-400">{g.messageCount} msgs</span>
              </li>
            ))}
            {mostActiveGroups.length === 0 && <li className="text-slate-500">No data</li>}
          </ul>
        </div>
      </div>

      {messagesPerDay.length > 0 && (
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-6 mb-8">
          <h2 className="font-semibold mb-4">Messages Per Day (Last 14 Days)</h2>
          <div className="flex flex-wrap gap-2">
            {messagesPerDay.map((d) => (
              <span key={d.date} className="px-2 py-1 bg-slate-800 rounded text-sm">
                {new Date(d.date).toLocaleDateString()}: {d.count}
              </span>
            ))}
          </div>
        </div>
      )}

      {tableCounts && Object.keys(tableCounts.counts).length > 0 && (
        <div className="bg-slate-900 rounded-xl border border-slate-800 p-6 mb-8">
          <h2 className="font-semibold mb-4">Database Table Row Counts</h2>
          <div className="flex flex-wrap gap-4">
            {Object.entries(tableCounts.counts).map(([k, v]) => (
              <span key={k} className="px-3 py-1 bg-slate-800 rounded-lg text-sm">{k}: {v.toLocaleString()}</span>
            ))}
          </div>
        </div>
      )}
      <div className="bg-slate-900 rounded-xl border border-slate-800 p-6">
        <h2 className="font-semibold mb-4">Users Over Limit</h2>
        <select
          value={limitKey}
          onChange={(e) => setLimitKey(e.target.value)}
          className="mb-4 px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white"
        >
          <option value="MaxSavedMessagesCount">MaxSavedMessagesCount</option>
          <option value="MaxPinnedMessages">MaxPinnedMessages</option>
          <option value="MaxSavedMessagesSizeBytes">MaxSavedMessagesSizeBytes</option>
        </select>
        {!overLimitResult ? (
          <p className="text-slate-400">Loading...</p>
        ) : overLimitResult.items.length === 0 ? (
          <p className="text-slate-400">No users over limit.</p>
        ) : (
          <>
            <ul className="space-y-2">
              {overLimitResult.items.map((u) => (
                <li key={u.id}>
                  <Link to={`/admin/users/${u.id}`} className="text-teal-400 hover:underline">{u.username}</Link>
                  {' '}({u.savedMessagesCount} saved, limit varies)
                </li>
              ))}
            </ul>
            {overLimitResult.totalPages > 1 && (
              <div className="flex flex-wrap items-center justify-between gap-2 mt-4 pt-3 border-t border-slate-700">
                <span className="text-slate-400 text-sm">Page {overLimitResult.page} of {overLimitResult.totalPages}</span>
                <div className="flex gap-2">
                  <button type="button" onClick={() => setOverLimitPage((p) => Math.max(1, p - 1))} disabled={overLimitPage <= 1} className="p-2.5 rounded-lg bg-slate-800 disabled:opacity-50 min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation" aria-label="Previous page">
                    <ChevronLeft size={18} />
                  </button>
                  <button type="button" onClick={() => setOverLimitPage((p) => Math.min(overLimitResult.totalPages, p + 1))} disabled={overLimitPage >= overLimitResult.totalPages} className="p-2.5 rounded-lg bg-slate-800 disabled:opacity-50 min-h-[44px] min-w-[44px] flex items-center justify-center touch-manipulation" aria-label="Next page">
                    <ChevronRight size={18} />
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
};
