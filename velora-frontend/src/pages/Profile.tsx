import React, { useState, useEffect, useRef } from 'react';
import { useAuthStore } from '../store/useAuthStore';
import { useToastStore } from '../store/useToastStore';
import { useThemeStore, CHAT_COLOR_TEMPLATES, type ChatColorTemplateId } from '../store/useThemeStore';
import { userService, sessionService } from '../services/api';
import type { SessionInfo } from '../types';
import { useNavigate } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { 
    Camera, Mail, ArrowLeft, User, Bell, Shield, Moon, Sun, 
    Monitor, Check, LogOut
} from 'lucide-react';
import { clsx } from 'clsx';
import { subscribeToPushNotifications } from '../utils/push';
import { useLocaleStore } from '../store/useLocaleStore';

export type ProfileTabType = 'profile' | 'appearance' | 'notifications' | 'security';

type ProfileProps = {
  embedded?: boolean;
  activeSubTab?: ProfileTabType;
  onSubTabChange?: (tab: ProfileTabType) => void;
};

export const Profile: React.FC<ProfileProps> = ({ embedded = false, activeSubTab, onSubTabChange }) => {
  const { user, setUser } = useAuthStore();
  const { addToast } = useToastStore();
  const { theme, setTheme, chatColorTemplate, setChatColorTemplate } = useThemeStore();
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [internalTab, setInternalTab] = useState<ProfileTabType>('profile');
  const activeTab = embedded && activeSubTab !== undefined ? activeSubTab : internalTab;
  const setActiveTab = embedded && onSubTabChange ? onSubTabChange : setInternalTab;

  // Profile Form State
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    bio: '',
    profilePicture: ''
  });

  // Password Form State
  const [passwordData, setPasswordData] = useState({
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
  });

  const [isLoading, setIsLoading] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [pushEnabled, setPushEnabled] = useState(Notification.permission === 'granted');
  const [sessions, setSessions] = useState<SessionInfo[]>([]);
  const [sessionsLoading, setSessionsLoading] = useState(false);
  const { locale, setLocale, t } = useLocaleStore();

  useEffect(() => {
    if (user) {
      setFormData({
        firstName: user.firstName || '',
        lastName: user.lastName || '',
        email: user.email || '',
        bio: user.bio || '',
        profilePicture: user.profilePicture || ''
      });
    }
  }, [user]);

  useEffect(() => {
    if (activeTab !== 'security') return;
    let cancelled = false;
    (async () => {
      setSessionsLoading(true);
      try {
        const list = await sessionService.getMySessions();
        if (!cancelled) setSessions(list);
      } catch {
        if (!cancelled) addToast(t('sessionsLoadFailed'), 'error');
      } finally {
        if (!cancelled) setSessionsLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [activeTab, addToast, t]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handlePasswordChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      setPasswordData({ ...passwordData, [e.target.name]: e.target.value });
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setIsUploading(true);
    try {
      const { persistValue, previewUrl } = await userService.uploadProfilePictureWithPreview(file);
      setFormData(prev => ({ ...prev, profilePicture: persistValue }));
      
      if (user) {
          setUser({ ...user, profilePicture: previewUrl || persistValue });
      }
      
      addToast('Profile picture uploaded successfully', 'success');
    } catch (error) {
      addToast('Failed to upload profile picture', 'error');
    } finally {
      setIsUploading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    try {
      await userService.updateProfile({
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        bio: formData.bio,
        profilePicture: formData.profilePicture
      });

      const updatedUser = await userService.getProfile();
      if (updatedUser) {
        setUser(updatedUser);
      }

      addToast('Profile updated successfully', 'success');
    } catch (error) {
      addToast('Failed to update profile', 'error');
    } finally {
      setIsLoading(false);
    }
  };

  const handlePasswordSubmit = async (e: React.FormEvent) => {
      e.preventDefault();
      if (passwordData.newPassword !== passwordData.confirmPassword) {
          addToast("New passwords do not match", "error");
          return;
      }
      
      setIsLoading(true);
      try {
          await userService.changePassword({
              currentPassword: passwordData.currentPassword,
              newPassword: passwordData.newPassword
          });
          addToast("Password changed successfully", "success");
          setPasswordData({ currentPassword: '', newPassword: '', confirmPassword: '' });
      } catch (error) {
          // Error handled by interceptor
      } finally {
          setIsLoading(false);
      }
  };

  const enablePushNotifications = async () => {
      const permission = await Notification.requestPermission();
      if (permission === 'granted') {
          await subscribeToPushNotifications();
          setPushEnabled(true);
          addToast("Push notifications enabled", "success");
      } else {
          addToast("Permission denied. Please enable in browser settings.", "error");
      }
  };

  const tabs = [
      { id: 'profile' as const, label: 'Edit Profile', icon: User },
      { id: 'appearance' as const, label: 'Appearance', icon: Moon },
      { id: 'notifications' as const, label: 'Notifications', icon: Bell },
      { id: 'security' as const, label: 'Security', icon: Shield },
  ];

  const contentSection = (
        <div className={clsx("bg-gray-100 dark:bg-gray-900 rounded-lg shadow-sm border border-gray-300 dark:border-gray-700 flex flex-col w-full min-w-0", embedded ? "flex-1 min-h-0 overflow-hidden" : "min-h-[500px]")}>
            {/* Content Header */}
            <div className="p-4 sm:p-6 border-b border-gray-300 dark:border-gray-700 flex-shrink-0">
                <h2 className="text-base sm:text-lg md:text-xl font-semibold text-gray-900 dark:text-white truncate">
                    {tabs.find(t => t.id === activeTab)?.label}
                </h2>
                <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
                    {activeTab === 'profile' && "Update your personal information"}
                    {activeTab === 'appearance' && "Customize how the app looks"}
                    {activeTab === 'notifications' && "Manage your notification preferences"}
                    {activeTab === 'security' && "Keep your account secure"}
                </p>
            </div>

            <div className="p-4 sm:p-6 overflow-y-auto flex-1 min-h-0 min-w-0">
                {/* Profile Tab */}
                {activeTab === 'profile' && (
                    <form onSubmit={handleSubmit} className="space-y-5 sm:space-y-6 max-w-2xl">
                        <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4 sm:gap-6 mb-6 sm:mb-8">
                            <div className="relative group">
                                <div className="w-24 h-24 rounded-full overflow-hidden bg-gray-200 dark:bg-gray-700 border-4 border-gray-100 dark:border-gray-800 shadow-sm">
                                    <img 
                                        src={formData.profilePicture || "https://via.placeholder.com/150"} 
                                        alt="Profile" 
                                        className="w-full h-full object-cover"
                                    />
                                </div>
                                <div 
                                    className="absolute inset-0 bg-black/40 flex items-center justify-center rounded-full opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
                                    onClick={() => fileInputRef.current?.click()}
                                >
                                    <Camera className="w-8 h-8 text-white" />
                                </div>
                                <input 
                                    type="file" 
                                    ref={fileInputRef} 
                                    onChange={handleFileChange} 
                                    className="hidden" 
                                    accept="image/*"
                                />
                            </div>
                            <div>
                                <h3 className="font-medium text-gray-900 dark:text-white">Profile Picture</h3>
                                <p className="text-sm text-gray-500 dark:text-gray-400 mb-3">PNG, JPG up to 10MB</p>
                                <Button 
                                    type="button" 
                                    variant="secondary" 
                                    size="sm"
                                    onClick={() => fileInputRef.current?.click()}
                                    disabled={isUploading}
                                >
                                    {isUploading ? 'Uploading...' : 'Upload New'}
                                </Button>
                            </div>
                        </div>

                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6">
                            <Input
                                label="First Name"
                                name="firstName"
                                value={formData.firstName}
                                onChange={handleChange}
                                required
                                className="bg-gray-50 dark:bg-gray-800 border-gray-300 dark:border-gray-600"
                            />
                            <Input
                                label="Last Name"
                                name="lastName"
                                value={formData.lastName}
                                onChange={handleChange}
                                required
                                className="bg-gray-50 dark:bg-gray-800 border-gray-300 dark:border-gray-600"
                            />
                        </div>

                        <div className="relative">
                            <Input
                                label="Email Address"
                                name="email"
                                type="email"
                                value={formData.email}
                                onChange={handleChange}
                                className="bg-gray-50 dark:bg-gray-800 border-gray-300 dark:border-gray-600 pl-10"
                            />
                            <Mail className="absolute left-3 top-[2.4rem] w-4 h-4 text-gray-400" />
                        </div>

                        <div className="flex flex-col gap-2">
                            <label className="text-sm font-medium text-gray-700 dark:text-gray-300">Bio</label>
                            <textarea
                                name="bio"
                                value={formData.bio}
                                onChange={handleChange}
                                rows={4}
                                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-gray-50 dark:bg-gray-800 text-gray-900 dark:text-white resize-none focus:ring-2 focus:ring-[var(--volera-accent)] focus:border-transparent"
                                placeholder="Tell us a little about yourself..."
                            />
                        </div>

                        <div className="pt-4 border-t border-gray-200 dark:border-gray-700 flex justify-end">
                            <Button type="submit" isLoading={isLoading} disabled={isUploading}>
                                Save Changes
                            </Button>
                        </div>
                    </form>
                )}

                {/* Appearance Tab */}
                {activeTab === 'appearance' && (
                    <div className="space-y-6 max-w-2xl min-w-0">
                        <div>
                            <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-4 block">Theme (night mode)</label>
                            <p className="text-xs text-gray-500 dark:text-gray-400 mb-3">App-wide light or dark theme. Stored for this session.</p>
                            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 sm:gap-4">
                                <button
                                    onClick={() => setTheme('light')}
                                    className={clsx(
                                        "flex flex-col items-center gap-2 sm:gap-3 p-3 sm:p-4 rounded-xl border-2 transition-all min-w-0",
                                        theme === 'light' 
                                            ? "border-[var(--volera-accent)] bg-[var(--volera-accent)]/10" 
                                            : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                                    )}
                                >
                                    <div className="w-full h-24 bg-gray-100 rounded-lg border border-gray-200 flex flex-col overflow-hidden">
                                        <div className="h-4 bg-white border-b border-gray-200 w-full"></div>
                                        <div className="flex-1 p-2 space-y-2">
                                            <div className="h-2 w-3/4 bg-gray-200 rounded"></div>
                                            <div className="h-2 w-1/2 bg-gray-200 rounded"></div>
                                        </div>
                                    </div>
                                    <div className="flex items-center gap-2 font-medium text-gray-900 dark:text-white">
                                        <Sun size={18} /> Light
                                    </div>
                                </button>

                                <button
                                    onClick={() => setTheme('dark')}
                                    className={clsx(
                                        "flex flex-col items-center gap-2 sm:gap-3 p-3 sm:p-4 rounded-xl border-2 transition-all min-w-0",
                                        theme === 'dark' 
                                            ? "border-[var(--volera-accent)] bg-[var(--volera-accent)]/10" 
                                            : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                                    )}
                                >
                                    <div className="w-full h-24 bg-gray-900 rounded-lg border border-gray-700 flex flex-col overflow-hidden">
                                        <div className="h-4 bg-gray-800 border-b border-gray-700 w-full"></div>
                                        <div className="flex-1 p-2 space-y-2">
                                            <div className="h-2 w-3/4 bg-gray-700 rounded"></div>
                                            <div className="h-2 w-1/2 bg-gray-700 rounded"></div>
                                        </div>
                                    </div>
                                    <div className="flex items-center gap-2 font-medium text-gray-900 dark:text-white">
                                        <Moon size={18} /> Dark
                                    </div>
                                </button>

                                <button
                                    onClick={() => setTheme('system')}
                                    className={clsx(
                                        "flex flex-col items-center gap-2 sm:gap-3 p-3 sm:p-4 rounded-xl border-2 transition-all min-w-0",
                                        theme === 'system' 
                                            ? "border-[var(--volera-accent)] bg-[var(--volera-accent)]/10" 
                                            : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                                    )}
                                >
                                    <div className="w-full h-24 bg-gradient-to-br from-gray-100 to-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 flex flex-col overflow-hidden relative">
                                        <div className="absolute inset-0 flex items-center justify-center text-gray-500">
                                            <span className="text-xs font-mono">Auto</span>
                                        </div>
                                    </div>
                                    <div className="flex items-center gap-2 font-medium text-gray-900 dark:text-white">
                                        <Monitor size={18} /> System
                                    </div>
                                </button>
                            </div>
                        </div>
                        <div>
                            <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3 block">Chat color theme</label>
                            <p className="text-xs text-gray-500 dark:text-gray-400 mb-4">Choose how your message bubbles and chat background look. Stored for this session.</p>
                            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                                {(Object.keys(CHAT_COLOR_TEMPLATES) as ChatColorTemplateId[]).map((id) => {
                                    const t = CHAT_COLOR_TEMPLATES[id];
                                    const isSelected = chatColorTemplate === id;
                                    return (
                                        <button
                                            key={id}
                                            type="button"
                                            onClick={() => setChatColorTemplate(id)}
                                            className={clsx(
                                                "flex flex-col rounded-xl border-2 transition-all overflow-hidden text-left min-w-0",
                                                isSelected
                                                    ? "border-[var(--volera-accent)] ring-2 ring-[var(--volera-accent)]/30"
                                                    : "border-gray-200 dark:border-gray-600 hover:border-gray-300 dark:hover:border-gray-500"
                                            )}
                                        >
                                            <div className="h-16 flex gap-0.5 p-1">
                                                <div className="flex-1 rounded-lg rounded-tr-none" style={{ background: t.bubbleMe }} />
                                                <div className="flex-1 rounded-lg rounded-tl-none border border-gray-200 dark:border-gray-600" style={{ background: t.bubbleOther }} />
                                            </div>
                                            <span className="px-3 py-2 text-xs font-medium text-gray-700 dark:text-gray-300 bg-gray-50 dark:bg-gray-800">
                                                {t.name}
                                            </span>
                                        </button>
                                    );
                                })}
                            </div>
                        </div>
                    </div>
                )}

                {/* Notifications Tab */}
                {activeTab === 'notifications' && (
                    <div className="space-y-6 max-w-2xl">
                        <div className="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
                            <div className="flex items-start gap-3">
                                <div className="p-2 bg-[var(--volera-accent)]/15 rounded-lg text-[var(--volera-accent)]">
                                    <Bell size={24} />
                                </div>
                                <div>
                                    <h3 className="font-medium text-gray-900 dark:text-white">Push Notifications</h3>
                                    <p className="text-sm text-gray-500 dark:text-gray-400">Receive notifications for new messages and calls</p>
                                </div>
                            </div>
                            <div className="flex items-center">
                                {pushEnabled ? (
                                    <span className="flex items-center gap-1 text-green-600 dark:text-green-400 text-sm font-medium bg-green-50 dark:bg-green-900/20 px-3 py-1 rounded-full">
                                        <Check size={14} /> Enabled
                                    </span>
                                ) : (
                                    <Button onClick={enablePushNotifications} size="sm">
                                        Enable
                                    </Button>
                                )}
                            </div>
                        </div>
                    </div>
                )}

                {/* Security Tab */}
                {activeTab === 'security' && (
                    <div className="space-y-8 max-w-2xl">
                        <div>
                            <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">{t('language')}</h3>
                            <p className="text-xs text-gray-500 dark:text-gray-400 mb-3">{t('languageHint')}</p>
                            <div className="flex gap-2">
                                <Button type="button" variant={locale === 'en' ? 'primary' : 'secondary'} onClick={() => setLocale('en')}>English</Button>
                                <Button type="button" variant={locale === 'fa' ? 'primary' : 'secondary'} onClick={() => setLocale('fa')}>فارسی</Button>
                            </div>
                        </div>

                        <div>
                            <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">{t('activeSessions')}</h3>
                            <p className="text-xs text-gray-500 dark:text-gray-400 mb-3">{t('sessionsHint')}</p>
                            {sessionsLoading ? (
                                <p className="text-sm text-gray-500">{t('loading')}</p>
                            ) : sessions.length === 0 ? (
                                <p className="text-sm text-gray-500">{t('noSessions')}</p>
                            ) : (
                                <ul className="space-y-2">
                                    {sessions.map((s) => (
                                        <li key={s.id} className="flex items-start justify-between gap-3 rounded-xl border border-[var(--volera-border)] bg-[var(--volera-surface)] p-3">
                                            <div className="min-w-0">
                                                <p className="text-sm font-medium text-gray-900 dark:text-white truncate">
                                                    {[s.deviceType, s.browser, s.os].filter(Boolean).join(' · ') || t('unknownDevice')}
                                                </p>
                                                <p className="text-xs text-gray-500 dark:text-gray-400">
                                                    {t('lastActive')}: {new Date(s.lastActivityAt).toLocaleString(locale === 'fa' ? 'fa-IR' : 'en-US')}
                                                </p>
                                            </div>
                                            <button
                                                type="button"
                                                className="shrink-0 inline-flex items-center gap-1 text-xs text-red-600 dark:text-red-400 hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--volera-accent)]"
                                                aria-label={t('revokeSession')}
                                                onClick={async () => {
                                                    try {
                                                        await sessionService.revokeSession(s.id);
                                                        setSessions((prev) => prev.filter((x) => x.id !== s.id));
                                                        addToast(t('sessionRevoked'), 'success');
                                                    } catch {
                                                        addToast(t('sessionRevokeFailed'), 'error');
                                                    }
                                                }}
                                            >
                                                <LogOut size={14} />
                                                {t('revoke')}
                                            </button>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </div>

                         <form onSubmit={handlePasswordSubmit} className="space-y-4">
                            <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-4">{t('changePassword')}</h3>
                            <Input
                                label="Current Password"
                                name="currentPassword"
                                type="password"
                                value={passwordData.currentPassword}
                                onChange={handlePasswordChange}
                                required
                                className="bg-gray-50 dark:bg-gray-800"
                            />
                            <Input
                                label="New Password"
                                name="newPassword"
                                type="password"
                                value={passwordData.newPassword}
                                onChange={handlePasswordChange}
                                required
                                className="bg-gray-50 dark:bg-gray-800"
                            />
                            <Input
                                label="Confirm New Password"
                                name="confirmPassword"
                                type="password"
                                value={passwordData.confirmPassword}
                                onChange={handlePasswordChange}
                                required
                                className="bg-gray-50 dark:bg-gray-800"
                            />
                            <div className="pt-2">
                                <Button type="submit" isLoading={isLoading} disabled={!passwordData.currentPassword || !passwordData.newPassword}>
                                    Update Password
                                </Button>
                            </div>
                        </form>
                    </div>
                )}
            </div>
        </div>
  );

  if (embedded) {
    return (
      <div className="flex-1 flex flex-col min-h-0 overflow-hidden w-full min-w-0 p-3 sm:p-4">
        {contentSection}
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-200 dark:bg-gray-950 flex flex-col min-w-0">
      <div className="md:hidden flex-shrink-0 p-4 bg-gray-100 dark:bg-gray-900 border-b border-gray-300 dark:border-gray-700 flex items-center gap-3">
        <button onClick={() => navigate('/')} className="p-2 -ml-2 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">
          <ArrowLeft size={20} />
        </button>
        <h1 className="font-semibold text-lg text-gray-900 dark:text-white truncate">Settings</h1>
      </div>
      <div className="flex-1 max-w-6xl w-full mx-auto p-3 sm:p-4 md:p-6 lg:p-8 flex flex-col lg:flex-row gap-4 sm:gap-6 min-w-0 overflow-auto">
        <div className="w-full lg:w-64 flex-shrink-0 min-w-0">
          <button onClick={() => navigate('/')} className="hidden md:flex items-center gap-2 text-gray-500 dark:text-gray-400 hover:text-[var(--volera-accent)] mb-4 transition-colors">
            <ArrowLeft size={20} />
            <span>Back to Chat</span>
          </button>
          <div className="bg-gray-100 dark:bg-gray-900 rounded-xl shadow-sm border border-gray-300 dark:border-gray-700 overflow-hidden">
            <div className="p-4 border-b border-gray-200 dark:border-gray-700">
              <div className="flex items-center gap-3">
                <img src={user?.profilePicture || "https://via.placeholder.com/150"} alt="Profile" className="w-10 h-10 rounded-full object-cover border border-gray-200 dark:border-gray-600" />
                <div className="overflow-hidden min-w-0">
                  <h3 className="font-semibold text-gray-900 dark:text-white truncate">{user?.firstName} {user?.lastName}</h3>
                  <p className="text-xs text-gray-500 dark:text-gray-400 truncate">{user?.email}</p>
                </div>
              </div>
            </div>
            <nav className="p-2">
              {tabs.map(tab => (
                <button key={tab.id} onClick={() => setActiveTab(tab.id)} className={clsx("w-full flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-colors mb-1", activeTab === tab.id ? "bg-[var(--volera-accent)]/10 text-[var(--volera-accent)]" : "text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700/50")}>
                  <tab.icon size={18} />
                  {tab.label}
                </button>
              ))}
            </nav>
          </div>
        </div>
        <div className="flex-1 min-w-0 overflow-hidden">{contentSection}</div>
      </div>
    </div>
  );
};
