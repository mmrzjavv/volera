'use client';

import { useState, useEffect, useCallback } from 'react';
import { useAuthStore } from '@/store/useAuthStore';
import { useCompanyStore } from '@/store/useCompanyStore';
import { aiWidgetApi, getAiWidgetEmbedScript } from '@/api/aiWidget';
import type { CompanyContentBlockDto, EmbedInfo, AiWidgetListItem } from '@/api/aiWidget';
import { useAiWidgetHub } from '@/hooks/useAiWidgetHub';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Select } from '@/components/ui/Select';
import { Copy, Check, RefreshCw, Bot, FileText } from 'lucide-react';

export default function AiWidgetPage() {
  const auth = useAuthStore((s) => s.auth);
  const { branches } = useCompanyStore();
  const [branchId, setBranchId] = useState('');
  const [setupDone, setSetupDone] = useState(false);
  const [setupLoading, setSetupLoading] = useState(false);
  const [contentList, setContentList] = useState<CompanyContentBlockDto[]>([]);
  const [contentLoading, setContentLoading] = useState(false);
  const [submitLoading, setSubmitLoading] = useState(false);
  const [contentText, setContentText] = useState('');
  const [embedInfo, setEmbedInfo] = useState<EmbedInfo | null>(null);
  const [widgetList, setWidgetList] = useState<AiWidgetListItem[]>([]);
  const [listLoading, setListLoading] = useState(false);
  const [copied, setCopied] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const fetchContentList = useCallback(async () => {
    if (!auth?.token || !branchId) return;
    setContentLoading(true);
    try {
      const res = await aiWidgetApi.getContentList(auth.token, branchId);
      if (res.success && res.data) setContentList(res.data);
    } catch {
      setContentList([]);
    } finally {
      setContentLoading(false);
    }
  }, [auth?.token, branchId]);

  const checkSetupAndEmbed = useCallback(async () => {
    if (!auth?.token || !branchId) {
      setSetupDone(false);
      setEmbedInfo(null);
      return;
    }
    try {
      const embedRes = await aiWidgetApi.getEmbedInfo(auth.token, branchId);
      if (embedRes.success && embedRes.data) {
        setEmbedInfo(embedRes.data);
        setSetupDone(true);
      } else {
        setSetupDone(false);
        setEmbedInfo(null);
      }
    } catch {
      setSetupDone(false);
      setEmbedInfo(null);
    }
  }, [auth?.token, branchId]);

  const fetchWidgetList = useCallback(async () => {
    if (!auth?.token) return;
    setListLoading(true);
    try {
      const res = await aiWidgetApi.getWidgetList(auth.token);
      if (res.success && res.data) setWidgetList(res.data);
      else setWidgetList([]);
    } catch {
      setWidgetList([]);
    } finally {
      setListLoading(false);
    }
  }, [auth?.token]);

  useEffect(() => {
    if (auth?.token) {
      fetchWidgetList();
    } else {
      setWidgetList([]);
    }
  }, [auth?.token, fetchWidgetList]);

  useEffect(() => {
    if (auth?.token && branchId) {
      checkSetupAndEmbed();
      fetchContentList();
    } else {
      setContentList([]);
      setSetupDone(false);
      setEmbedInfo(null);
    }
  }, [auth?.token, branchId, checkSetupAndEmbed, fetchContentList]);

  useAiWidgetHub(auth?.token, (payload) => {
    if (payload.branchId === branchId) {
      fetchContentList();
      fetchWidgetList();
      if (payload.status === 'Completed') {
        setMessage({ type: 'success', text: 'Content indexed successfully. Widget is now active.' });
        setContentText('');
        checkSetupAndEmbed();
      } else if (payload.status === 'Failed') {
        setMessage({ type: 'error', text: payload.error ?? 'Indexing failed.' });
      }
    }
  });

  const handleSetup = async () => {
    if (!auth?.token || !branchId) {
      setMessage({ type: 'error', text: 'Select a branch first.' });
      return;
    }
    setSetupLoading(true);
    setMessage(null);
    try {
      await aiWidgetApi.setup(auth.token, branchId);
      await checkSetupAndEmbed();
      setMessage({ type: 'success', text: 'AI Widget is set up. Add and index company data below to activate the widget.' });
    } catch (e) {
      setMessage({ type: 'error', text: e instanceof Error ? e.message : 'Setup failed.' });
    } finally {
      setSetupLoading(false);
    }
  };

  const handleSubmitContent = async () => {
    if (!auth?.token || !branchId) {
      setMessage({ type: 'error', text: 'Select a branch and enable AI Widget first.' });
      return;
    }
    const text = contentText?.trim() ?? '';
    if (!text) {
      setMessage({ type: 'error', text: 'Enter some text about your company.' });
      return;
    }
    setSubmitLoading(true);
    setMessage(null);
    try {
      await aiWidgetApi.submitContent(auth.token, branchId, text);
      await fetchContentList();
      setMessage({ type: 'success', text: 'Content is being indexed. You will be notified when ready.' });
    } catch (e) {
      setMessage({ type: 'error', text: e instanceof Error ? e.message : 'Submit failed.' });
    } finally {
      setSubmitLoading(false);
    }
  };

  const handleCopy = () => {
    if (!branchId) return;
    const scriptLine = getAiWidgetEmbedScript(branchId);
    if (typeof navigator !== 'undefined' && navigator.clipboard) {
      navigator.clipboard.writeText(scriptLine);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  if (!auth) return null;

  const branchOptions = branches.map((b) => ({ value: b.id, label: b.name }));
  const isWidgetActive = embedInfo?.isActive === true;
  const scriptLine = branchId
    ? getAiWidgetEmbedScript(branchId)
    : '<script src="BASE_URL/ai-widget.js" data-branch="YOUR_BRANCH_ID"></script>';

  const introText =
    'برای هر شعبه، ویجت هوش مصنوعی را فعال کنید، محتوای متنی شرکت را اینجا وارد کنید تا روی سرور ایندکس شود و بعد از اسکریپت امبد زیر برای اتصال چت‌بات استفاده کنید.';

  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900">محتوای یادگیری هوش مصنوعی</h1>
      <p className="mt-1 text-slate-600">{introText}</p>

      {message && (
        <div
          className={`mt-4 rounded-lg border p-3 text-sm ${
            message.type === 'success'
              ? 'border-green-200 bg-green-50 text-green-800'
              : 'border-red-200 bg-red-50 text-red-800'
          }`}
        >
          {message.text}
        </div>
      )}

      <Card className="mt-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Bot className="h-5 w-5" />
            Active widgets
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="mb-3 text-sm text-slate-600">
            Branches that have the AI widget set up. The widget becomes active only after at least one content block has been indexed.
          </p>
          {listLoading ? (
            <p className="text-sm text-slate-500">Loading…</p>
          ) : widgetList.length === 0 ? (
            <p className="text-sm text-slate-500">No AI widgets set up yet. Select a branch and enable the widget below.</p>
          ) : (
            <ul className="space-y-2">
              {widgetList.map((w) => (
                <li
                  key={w.branchId}
                  className="flex items-center justify-between rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2 text-sm"
                >
                  <span className="font-medium text-slate-800">{w.branchName}</span>
                  <span
                    className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${
                      w.isActive ? 'bg-green-100 text-green-800' : 'bg-amber-100 text-amber-800'
                    }`}
                  >
                    {w.isActive ? 'Active' : 'Not ready (index company data first)'}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <div className="mt-8 grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Bot className="h-5 w-5" />
              راه‌اندازی ویجت هوش مصنوعی
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <Select
              label="Branch"
              options={branchOptions}
              value={branchId}
              onChange={(e) => setBranchId(e.target.value)}
            />
            {branchId && (
              <>
                {setupDone ? (
                  <p className="text-sm text-slate-600">
                    {isWidgetActive
                      ? 'AI Widget is active. You can embed the script below.'
                      : 'AI Widget is set up. Add company data and click "Save and index" to activate.'}
                  </p>
                ) : (
                  <Button onClick={handleSetup} disabled={setupLoading} className="w-full">
                    {setupLoading ? 'Enabling…' : 'Enable AI Widget for this branch'}
                  </Button>
                )}
              </>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <FileText className="h-5 w-5" />
                محتوای یادگیری (متن شرکت)
              </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-slate-600">
              درباره شرکت، محصولات، پرسش‌های متداول، قوانین و… بنویسید. این متن برای ساخت امبد و پاسخ‌گویی هوش مصنوعی به
              سوال‌های کاربران استفاده می‌شود. (فعلاً فقط متن، بدون آپلود فایل)
            </p>
            {branchId && setupDone && (
              <>
                <textarea
                  value={contentText}
                  onChange={(e) => setContentText(e.target.value)}
                  placeholder="Paste or type your company info here..."
                  rows={5}
                  className="w-full rounded-lg border border-slate-300 p-3 text-sm"
                />
                <Button onClick={handleSubmitContent} disabled={submitLoading} className="w-full">
                  {submitLoading ? 'Submitting…' : 'Save and index'}
                </Button>
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium text-slate-700">Previous entries</span>
                  <Button variant="secondary" size="sm" onClick={fetchContentList} disabled={contentLoading}>
                    <RefreshCw className={`mr-1 h-4 w-4 ${contentLoading ? 'animate-spin' : ''}`} />
                    Refresh
                  </Button>
                </div>
                <ul className="max-h-48 space-y-2 overflow-y-auto rounded border border-slate-200 bg-slate-50 p-2 text-sm">
                  {contentLoading && contentList.length === 0 ? (
                    <li className="text-slate-500">Loading…</li>
                  ) : contentList.length === 0 ? (
                    <li className="text-slate-500">No content yet.</li>
                  ) : (
                    contentList.map((block) => (
                      <li key={block.id} className="rounded bg-white p-2 shadow-sm">
                        <p className="truncate text-slate-800">{block.contentSnippet}</p>
                        <p className="mt-1 text-xs text-slate-500">
                          وضعیت ایندکس: {block.status}
                          {block.errorMessage ? ` · خطا: ${block.errorMessage}` : ''} ·{' '}
                          {new Date(block.createdAt).toLocaleString()}
                        </p>
                      </li>
                    ))
                  )}
                </ul>
              </>
            )}
            {branchId && !setupDone && (
              <p className="text-sm text-amber-700">Enable AI Widget for this branch first.</p>
            )}
          </CardContent>
        </Card>

        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Embed code</CardTitle>
          </CardHeader>
          <CardContent>
            {setupDone && !isWidgetActive && (
              <div className="mb-4 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
                The widget will not work for visitors until company data has been indexed. Add content above and click &quot;Save and index&quot;; when indexing completes, the widget will become active.
              </div>
            )}
            <p className="mb-2 text-sm text-slate-600">
              {setupDone
                ? 'Add this script to your website. The widget works for visitors only when it is active (after at least one content block is indexed).'
                : 'Select a branch and enable the AI widget above, then add and index company data. Embed code will appear here.'}
            </p>
            <pre className="overflow-x-auto rounded-lg bg-slate-900 p-4 text-sm text-slate-100">
              {scriptLine}
            </pre>
            <Button
              variant="secondary"
              className="mt-4"
              onClick={handleCopy}
              disabled={!setupDone}
              title={!setupDone ? 'Enable widget first' : undefined}
            >
              {copied ? (
                <>
                  <Check className="mr-2 h-4 w-4" />
                  Copied
                </>
              ) : (
                <>
                  <Copy className="mr-2 h-4 w-4" />
                  Copy script
                </>
              )}
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
