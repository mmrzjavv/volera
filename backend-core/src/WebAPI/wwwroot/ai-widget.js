(function () {
  'use strict';

  var script = document.currentScript;
  if (!script) return;

  var dataBranch = script.getAttribute('data-branch');
  var dataWidgetId = script.getAttribute('data-widget-id');
  var dataColor = (script.getAttribute('data-color') || '0d9488').replace(/^#/, '');
  var dataPosition = script.getAttribute('data-position') || 'bottom-right';

  var base = script.src ? script.src.replace(/\/ai-widget\.js.*$/, '') : (window.location.origin || 'http://localhost:5002');
  var apiBase = base.replace(/\/$/, '');

  var STORAGE_PREFIX = 'ai_widget_';
  var connection = null;
  var sessionId = null;
  var resolvedBranchId = null;

  function getStorageKey() {
    return dataWidgetId ? (STORAGE_PREFIX + dataWidgetId) : (STORAGE_PREFIX + 'branch');
  }

  function getStoredToken() {
    try {
      var key = getStorageKey();
      var token = localStorage.getItem(key + '_token');
      var expiry = localStorage.getItem(key + '_expiry');
      var branch = localStorage.getItem(key + '_branch');
      if (!token || !expiry || new Date(expiry) <= new Date()) return null;
      if (dataBranch && branch !== dataBranch) return null;
      if (dataWidgetId && branch) resolvedBranchId = branch;
      return token;
    } catch (e) { return null; }
  }

  function setStoredToken(token, expiresAt, branchId) {
    try {
      var key = getStorageKey();
      localStorage.setItem(key + '_token', token);
      localStorage.setItem(key + '_expiry', expiresAt);
      localStorage.setItem(key + '_branch', branchId || dataBranch || '');
    } catch (e) {}
  }

  function createSession(cb) {
    if (!dataBranch && !dataWidgetId) {
      cb(new Error('data-branch or data-widget-id is required on the script tag.'));
      return;
    }
    var body = dataWidgetId
      ? { widgetId: dataWidgetId }
      : { branchId: dataBranch };
    fetch(apiBase + '/api/v1/company/ai-widget/session', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    })
      .then(function (r) { return r.json(); })
      .then(function (json) {
        if (json && json.data && json.data.token) {
          var data = json.data;
          var branchId = data.branchId || dataBranch;
          setStoredToken(data.token, new Date(Date.now() + (data.expiresIn || 86400) * 1000).toISOString(), branchId);
          if (branchId) resolvedBranchId = branchId;
          cb(null, data.token);
        } else {
          var msg = (json && json.message && (Array.isArray(json.message) ? json.message[0] : json.message)) || 'Failed to create session.';
          cb(new Error(msg));
        }
      })
      .catch(function (err) { cb(err || new Error('Network error')); });
  }

  function runWidget() {
    if (typeof signalR === 'undefined') {
      console.warn('AI Widget: SignalR not loaded. Add <script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js"></script> before ai-widget.js');
      return;
    }

    var token = getStoredToken();
    if (!token) {
      createSession(function (err, t) {
        if (err) {
          console.error('AI Widget:', err.message);
          return;
        }
        token = t;
        initUI(token);
      });
    } else {
      initUI(token);
    }
  }

  function initUI(token) {
    var branchIdGuid = resolvedBranchId || dataBranch;
    if (!branchIdGuid) {
      console.error('AI Widget: missing branch ID.');
      return;
    }
    var hubUrl = apiBase + '/aiWidgetHub?access_token=' + encodeURIComponent(token);
    connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();

    var pending = {};
    connection.on('AiReply', function (correlationId, answer) {
      if (pending[correlationId]) {
        pending[correlationId](answer);
        delete pending[correlationId];
      }
      appendMessage(answer, false);
    });
    connection.on('MessageReceived', function (correlationId) {
      if (pending[correlationId]) pending[correlationId](null);
    });

    connection.start().then(function () {
      renderWidget();
      bindSend(branchIdGuid, pending);
    }).catch(function (err) {
      console.error('AI Widget connection failed:', err);
      renderWidget();
    });
  }

  function appendMessage(content, isSent) {
    var container = document.getElementById('ai-widget-messages');
    if (!container) return;
    var div = document.createElement('div');
    div.className = 'ai-widget-msg ' + (isSent ? 'ai-widget-msg-sent' : 'ai-widget-msg-received');
    div.textContent = content;
    container.appendChild(div);
    container.scrollTop = container.scrollHeight;
  }

  function bindSend(branchIdGuid, pending) {
    var input = document.getElementById('ai-widget-input');
    var btn = document.getElementById('ai-widget-send');
    if (!input || !btn) return;
    function send() {
      var text = (input.value || '').trim();
      if (!text || !connection) return;
      input.value = '';
      btn.disabled = true;
      appendMessage(text, true);
      var correlationId = 'c' + Date.now() + Math.random().toString(36).slice(2, 9);
      connection.invoke('SendMessage', branchIdGuid, sessionId || '', text).catch(function (err) {
        appendMessage('Failed to send: ' + (err && err.message ? err.message : 'Error'), false);
      });
      btn.disabled = false;
    }
    btn.addEventListener('click', send);
    input.addEventListener('keydown', function (e) {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        send();
      }
    });
  }

  function renderWidget() {
    if (document.getElementById('ai-widget-root')) return;

    var position = dataPosition || 'bottom-right';
    var isBottom = position.indexOf('bottom') !== -1;
    var isRight = position.indexOf('right') !== -1;
    var bottom = isBottom ? '20px' : 'auto';
    var top = isBottom ? 'auto' : '20px';
    var left = isRight ? 'auto' : '20px';
    var right = isRight ? '20px' : 'auto';

    var root = document.createElement('div');
    root.id = 'ai-widget-root';
    root.innerHTML =
      '<div id="ai-widget-panel" style="display:none; position:absolute; ' + (isBottom ? 'bottom:64px' : 'top:64px') + '; ' + (isRight ? 'right:0' : 'left:0') + '; width:360px; max-width:calc(100vw - 40px); height:420px; background:#fff; border-radius:12px; box-shadow:0 8px 32px rgba(0,0,0,0.08); flex-direction:column; overflow:hidden; border:1px solid rgba(0,0,0,0.06); z-index:9998;">' +
      '<div style="padding:14px 16px; border-bottom:1px solid #eee; display:flex; align-items:center; justify-content:space-between;">' +
      '<strong style="font-size:0.9375rem;">AI Assistant</strong>' +
      '<button type="button" id="ai-widget-close" style="background:none; border:none; font-size:1.2rem; cursor:pointer; color:#888;">×</button>' +
      '</div>' +
      '<div id="ai-widget-messages" style="flex:1; overflow-y:auto; padding:12px; display:flex; flex-direction:column; gap:8px; background:#fafafa; min-height:200px;"></div>' +
      '<div style="padding:10px 12px; border-top:1px solid #eee; display:flex; gap:8px; align-items:center;">' +
      '<input type="text" id="ai-widget-input" placeholder="Type a message..." style="flex:1; padding:10px 14px; border:1px solid #e5e5e5; border-radius:20px; font-size:0.875rem;" maxlength="2000" />' +
      '<button type="button" id="ai-widget-send" style="width:38px; height:38px; border-radius:50%; background:#' + dataColor + '; color:#fff; border:none; cursor:pointer; font-size:0.875rem;">↑</button>' +
      '</div>' +
      '</div>' +
      '<button type="button" id="ai-widget-toggle" style="width:52px; height:52px; border-radius:50%; background:#' + dataColor + '; color:#fff; border:none; cursor:pointer; display:flex; align-items:center; justify-content:center; box-shadow:0 2px 12px rgba(0,0,0,0.15); position:fixed; ' +
      'bottom:' + bottom + '; top:' + top + '; left:' + left + '; right:' + right + '; z-index:9999;">' +
      '<svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor"><path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H5.17L4 17.17V4h16v12z"/></svg>' +
      '</button>';

    root.style.cssText = 'position:fixed; bottom:' + bottom + '; top:' + top + '; left:' + left + '; right:' + right + '; z-index:9999; font-family:system-ui,-apple-system,sans-serif; font-size:14px;';
    document.body.appendChild(root);

    var panel = document.getElementById('ai-widget-panel');
    var toggle = document.getElementById('ai-widget-toggle');
    var closeBtn = document.getElementById('ai-widget-close');

    if (toggle) {
      toggle.addEventListener('click', function () {
        if (panel) {
          panel.style.display = panel.style.display === 'none' ? 'flex' : 'none';
          toggle.classList.toggle('open', panel.style.display !== 'none');
        }
      });
    }
    if (closeBtn && panel) {
      closeBtn.addEventListener('click', function () {
        panel.style.display = 'none';
        if (toggle) toggle.classList.remove('open');
      });
    }

    var style = document.createElement('style');
    style.textContent = '.ai-widget-msg { max-width:82%; padding:9px 12px; border-radius:14px; word-break:break-word; font-size:0.875rem; } .ai-widget-msg-sent { align-self:flex-end; background:#' + dataColor + '; color:#fff; border-bottom-right-radius:4px; } .ai-widget-msg-received { align-self:flex-start; background:#fff; border:1px solid #eee; border-bottom-left-radius:4px; }';
    document.head.appendChild(style);
  }

  function loadSignalR(cb) {
    if (typeof signalR !== 'undefined') {
      cb();
      return;
    }
    var s = document.createElement('script');
    s.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js';
    s.onload = cb;
    s.onerror = function () {
      console.error('AI Widget: Failed to load SignalR.');
      cb();
    };
    document.head.appendChild(s);
  }

  loadSignalR(runWidget);
})();
