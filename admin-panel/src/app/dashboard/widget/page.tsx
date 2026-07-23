'use client';

import { useState, useEffect } from 'react';
import { useAuthStore } from '@/store/useAuthStore';
import { useCompanyStore } from '@/store/useCompanyStore';
import { useWidgetStore } from '@/store/useWidgetStore';
import { companyApi, getWidgetScriptUrl } from '@/api/company';
import type { CompanyWidgetDto } from '@/api/company';
import { aiWidgetApi, type EmbedInfo } from '@/api/aiWidget';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Select } from '@/components/ui/Select';
import { Copy, Check, PlusCircle, RefreshCw, Package, AlertCircle, Trash2 } from 'lucide-react';

const POSITIONS = [
  { value: 'bottom-right', label: 'Bottom right' },
  { value: 'bottom-left', label: 'Bottom left' },
  { value: 'top-right', label: 'Top right' },
  { value: 'top-left', label: 'Top left' },
];

export default function WidgetPage() {
  const auth = useAuthStore((s) => s.auth);
  const { branches } = useCompanyStore();
  const { config, setBranchId, setColor, setPosition } = useWidgetStore();
  const [copied, setCopied] = useState(false);
  const [widgets, setWidgets] = useState<CompanyWidgetDto[]>([]);
  const [widgetsLoading, setWidgetsLoading] = useState(false);
  const [generateLoading, setGenerateLoading] = useState(false);
  const [deleteLoading, setDeleteLoading] = useState<string | null>(null);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [embedInfo, setEmbedInfo] = useState<EmbedInfo | null>(null);
  const [embedLoading, setEmbedLoading] = useState(false);
  const [embedError, setEmbedError] = useState<string | null>(null);

  const fetchWidgets = async () => {
    if (!auth?.token) return;
    setWidgetsLoading(true);
    setMessage(null);
    try {
      const res = await companyApi.getWidgets(auth.token);
      if (res.success && res.data) setWidgets(res.data);
    } catch {
      setMessage({ type: 'error', text: 'Failed to load widgets.' });
    } finally {
      setWidgetsLoading(false);
    }
  };

  const fetchEmbedInfo = async (branchId: string) => {
    if (!auth?.token || !branchId) {
      setEmbedInfo(null);
      setEmbedError(null);
      return;
    }
    setEmbedLoading(true);
    setEmbedError(null);
    try {
      const res = await aiWidgetApi.getEmbedInfo(auth.token, branchId);
      if (res.success && res.data) {
        setEmbedInfo(res.data);
      } else {
        setEmbedInfo(null);
      }
    } catch (e) {
      const text = e instanceof Error ? e.message : 'Failed to load AI learning content status.';
      setEmbedError(text);
      setEmbedInfo(null);
    } finally {
      setEmbedLoading(false);
    }
  };

  useEffect(() => {
    if (auth?.token) fetchWidgets();
  }, [auth?.token]);

  useEffect(() => {
    if (config.branchId) {
      fetchEmbedInfo(config.branchId);
    } else {
      setEmbedInfo(null);
      setEmbedError(null);
    }
  }, [config.branchId]);

  const widgetForSelectedBranch = config.branchId
    ? widgets.find((w) => w.branchId === config.branchId && w.isActive)
    : null;
  const hasWidget = !!widgetForSelectedBranch;

  // Group widgets by branch ID
  const widgetsByBranch = new Map<string, CompanyWidgetDto[]>();
  widgets.forEach((w) => {
    const existing = widgetsByBranch.get(w.branchId) || [];
    existing.push(w);
    widgetsByBranch.set(w.branchId, existing);
  });

  // Create list of branches with their widget status
  const branchesWithWidgets = branches.map((branch) => {
    const branchWidgets = widgetsByBranch.get(branch.id) || [];
    const activeWidgets = branchWidgets.filter((w) => w.isActive);
    return {
      branch,
      widgets: branchWidgets,
      activeWidgets,
      hasActiveWidget: activeWidgets.length > 0,
    };
  });

  const handleGenerate = async () => {
    if (!auth?.token || !config.branchId) {
      setMessage({ type: 'error', text: 'Select a branch first.' });
      return;
    }
    setGenerateLoading(true);
    setMessage(null);
    try {
      await companyApi.generateWidget(auth.token, config.branchId);
      await fetchWidgets();
      setMessage({ type: 'success', text: 'Widget generated. You can use the embed code for this branch.' });
    } catch (e) {
      const text = e instanceof Error ? e.message : 'Failed to generate widget.';
      setMessage({ type: 'error', text });
    } finally {
      setGenerateLoading(false);
    }
  };

  const handleDelete = async (widgetId: string, widgetDisplayId: string) => {
    if (!auth?.token) return;
    if (!confirm(`Are you sure you want to delete widget "${widgetDisplayId}"? This action cannot be undone.`)) {
      return;
    }
    setDeleteLoading(widgetId);
    setMessage(null);
    try {
      await companyApi.deleteWidget(auth.token, widgetId);
      await fetchWidgets();
      setMessage({ type: 'success', text: `Widget "${widgetDisplayId}" deleted successfully.` });
    } catch (e) {
      const text = e instanceof Error ? e.message : 'Failed to delete widget.';
      setMessage({ type: 'error', text });
    } finally {
      setDeleteLoading(null);
    }
  };

  if (!auth) return null;

  const branchOptions = branches.map((b) => ({ value: b.id, label: b.name }));
  const scriptUrl = getWidgetScriptUrl();

  const scriptLine = config.branchId
    ? `<script src="${scriptUrl}" data-branch="${config.branchId}" data-color="${config.color.replace('#', '')}" data-position="${config.position}"></script>`
    : `<script src="${scriptUrl}" data-branch="YOUR_BRANCH_ID"></script>`;

  const handleCopy = () => {
    if (typeof navigator !== 'undefined' && navigator.clipboard) {
      navigator.clipboard.writeText(scriptLine);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900">ویجت چت‌بات هوش مصنوعی</h1>
      <p className="mt-1 text-slate-600">
        برای هر شعبه که محتوای یادگیری هوش مصنوعی‌اش در بخش «محتوای یادگیری هوش مصنوعی» فعال شده، یک ویجت چت‌بات بسازید و
        کد امبد را در سایت خود قرار دهید.
      </p>

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

      <div className="mt-8 grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>تنظیمات ویجت چت‌بات</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <Select
              label="Branch"
              options={branchOptions}
              value={config.branchId}
              onChange={(e) => setBranchId(e.target.value)}
            />
            {config.branchId && (
              <div className="space-y-1 text-xs">
                {embedLoading ? (
                  <p className="text-slate-500">در حال بررسی وضعیت محتوای یادگیری هوش مصنوعی این شعبه…</p>
                ) : embedError ? (
                  <p className="text-red-600">خطا در دریافت وضعیت محتوای یادگیری: {embedError}</p>
                ) : embedInfo && embedInfo.isActive ? (
                  <p className="text-green-700">
                    برای این شعبه، محتوای یادگیری هوش مصنوعی فعال است و این ویجت از همان امبد استفاده می‌کند.
                  </p>
                ) : (
                  <p className="text-amber-700">
                    برای این شعبه هنوز محتوای یادگیری هوش مصنوعی فعال نشده است. ابتدا در صفحه «محتوای یادگیری هوش مصنوعی»
                    ویجت را راه‌اندازی کرده و حداقل یک محتوای ایندکس‌شده داشته باشید.
                  </p>
                )}
              </div>
            )}
            {config.branchId && (
              <div className="flex flex-wrap items-center gap-3">
                {widgetsLoading ? (
                  <span className="text-sm text-slate-500">Loading…</span>
                ) : hasWidget ? (
                  <span className="text-sm text-green-700">
                    This branch has an active widget ({widgetForSelectedBranch!.widgetId}).
                  </span>
                ) : (
                  <span className="text-sm text-amber-700">
                    No widget for this branch yet. Generate one below.
                  </span>
                )}
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={fetchWidgets}
                  disabled={widgetsLoading}
                >
                  <RefreshCw className="mr-1 h-4 w-4" />
                  Refresh
                </Button>
              </div>
            )}
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">
                Widget color
              </label>
              <div className="flex items-center gap-2">
                <input
                  type="color"
                  value={config.color}
                  onChange={(e) => setColor(e.target.value)}
                  className="h-10 w-14 cursor-pointer rounded border border-slate-300"
                />
                <span className="text-sm text-slate-600">{config.color}</span>
              </div>
            </div>
            <Select
              label="Position"
              options={POSITIONS}
              value={config.position}
              onChange={(e) => setPosition(e.target.value as typeof config.position)}
            />
            {config.branchId && (
              <Button
                onClick={handleGenerate}
                disabled={generateLoading}
                className="w-full"
              >
                {generateLoading ? (
                  'Generating…'
                ) : (
                  <>
                    <PlusCircle className="mr-2 h-4 w-4" />
                    {hasWidget ? 'Generate another widget for this branch' : 'Generate widget for this branch'}
                  </>
                )}
              </Button>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Embed code</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="mb-2 text-sm text-slate-600">
              Use this script on your website. The branch must have a widget generated above first.
            </p>
            <pre className="overflow-x-auto rounded-lg bg-slate-900 p-4 text-sm text-slate-100">
              {scriptLine}
            </pre>
            <Button
              variant="secondary"
              className="mt-4"
              onClick={handleCopy}
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

      <Card className="mt-8">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Branch widgets</CardTitle>
            <Button
              variant="secondary"
              size="sm"
              onClick={fetchWidgets}
              disabled={widgetsLoading}
            >
              <RefreshCw className={`mr-1 h-4 w-4 ${widgetsLoading ? 'animate-spin' : ''}`} />
              Refresh
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {widgetsLoading ? (
            <div className="py-8 text-center text-slate-500">Loading widgets…</div>
          ) : branchesWithWidgets.length === 0 ? (
            <div className="py-8 text-center text-slate-500">No branches found.</div>
          ) : (
            <div className="space-y-3">
              {branchesWithWidgets.map(({ branch, activeWidgets, hasActiveWidget, widgets: allWidgets }) => (
                <div
                  key={branch.id}
                  className={`rounded-lg border p-4 transition-colors ${
                    config.branchId === branch.id
                      ? 'border-primary-500 bg-primary-50'
                      : 'border-slate-200 bg-white hover:border-slate-300'
                  }`}
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <h3 className="font-semibold text-slate-900">{branch.name}</h3>
                        {hasActiveWidget ? (
                          <span className="inline-flex items-center gap-1 rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800">
                            <Package className="h-3 w-3" />
                            Active widget
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-800">
                            <AlertCircle className="h-3 w-3" />
                            No widget
                          </span>
                        )}
                      </div>
                      {branch.address && (
                        <p className="mt-1 text-sm text-slate-600">{branch.address}</p>
                      )}
                      {allWidgets.length > 0 && (
                        <div className="mt-2 space-y-2">
                          {allWidgets.map((widget) => (
                            <div
                              key={widget.id}
                              className={`flex items-center justify-between rounded p-2 ${
                                widget.isActive ? 'bg-slate-50' : 'bg-amber-50'
                              }`}
                            >
                              <div className="text-sm">
                                <span className="font-mono text-slate-700">Widget ID:</span>{' '}
                                <code className="rounded bg-slate-100 px-1.5 py-0.5 text-xs font-mono text-slate-900">
                                  {widget.widgetId}
                                </code>
                                {widget.isActive ? (
                                  <span className="ml-2 text-xs text-green-700">● Active</span>
                                ) : (
                                  <span className="ml-2 text-xs text-amber-700">● Inactive</span>
                                )}
                              </div>
                              <Button
                                variant="secondary"
                                size="sm"
                                onClick={() => handleDelete(widget.id, widget.widgetId)}
                                disabled={deleteLoading === widget.id}
                                className="text-red-600 hover:bg-red-50 hover:text-red-700"
                              >
                                {deleteLoading === widget.id ? (
                                  'Deleting…'
                                ) : (
                                  <>
                                    <Trash2 className="mr-1 h-3.5 w-3.5" />
                                    Delete
                                  </>
                                )}
                              </Button>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={() => setBranchId(branch.id)}
                      className="ml-4"
                    >
                      {config.branchId === branch.id ? 'Selected' : 'Select'}
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
