import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export type AppLocale = 'en' | 'fa';

const strings = {
  en: {
    language: 'Language',
    languageHint: 'Sets layout direction (LTR/RTL) for Volera chrome.',
    activeSessions: 'Active sessions',
    sessionsHint: 'Devices signed into your account. Revoke any session you do not recognize.',
    loading: 'Loading…',
    noSessions: 'No other sessions listed.',
    unknownDevice: 'Unknown device',
    lastActive: 'Last active',
    revokeSession: 'Revoke session',
    revoke: 'Revoke',
    sessionRevoked: 'Session revoked',
    sessionRevokeFailed: 'Could not revoke session',
    sessionsLoadFailed: 'Could not load sessions',
    changePassword: 'Change password',
    chats: 'Chats',
    search: 'Search',
    emptyInbox: 'Select a chat to start messaging',
    emptyInboxHint: 'Your conversations appear here. Volera keeps drafts and queued sends offline.',
  },
  fa: {
    language: 'زبان',
    languageHint: 'جهت چیدمان (راست‌چین/چپ‌چین) رابط ولرا را تنظیم می‌کند.',
    activeSessions: 'نشست‌های فعال',
    sessionsHint: 'دستگاه‌هایی که وارد حساب شما شده‌اند. نشست‌های ناشناس را لغو کنید.',
    loading: 'در حال بارگذاری…',
    noSessions: 'نشست دیگری ثبت نشده است.',
    unknownDevice: 'دستگاه ناشناس',
    lastActive: 'آخرین فعالیت',
    revokeSession: 'لغو نشست',
    revoke: 'لغو',
    sessionRevoked: 'نشست لغو شد',
    sessionRevokeFailed: 'لغو نشست ناموفق بود',
    sessionsLoadFailed: 'بارگذاری نشست‌ها ناموفق بود',
    changePassword: 'تغییر گذرواژه',
    chats: 'گفتگوها',
    search: 'جستجو',
    emptyInbox: 'یک گفتگو را انتخاب کنید',
    emptyInboxHint: 'گفتگوهای شما اینجا نمایش داده می‌شوند. ولرا پیش‌نویس و صف ارسال را آفلاین نگه می‌دارد.',
  },
} as const;

type StringKey = keyof typeof strings.en;

type LocaleState = {
  locale: AppLocale;
  setLocale: (locale: AppLocale) => void;
  t: (key: StringKey) => string;
  dir: 'ltr' | 'rtl';
};

export const useLocaleStore = create<LocaleState>()(
  persist(
    (set, get) => ({
      locale: 'en',
      dir: 'ltr',
      setLocale: (locale) => {
        const dir = locale === 'fa' ? 'rtl' : 'ltr';
        set({ locale, dir });
        document.documentElement.lang = locale === 'fa' ? 'fa' : 'en';
        document.documentElement.dir = dir;
      },
      t: (key) => strings[get().locale][key] ?? strings.en[key],
    }),
    { name: 'volera-locale' }
  )
);

export function syncLocaleToDom() {
  const { locale, dir } = useLocaleStore.getState();
  document.documentElement.lang = locale === 'fa' ? 'fa' : 'en';
  document.documentElement.dir = dir;
}
