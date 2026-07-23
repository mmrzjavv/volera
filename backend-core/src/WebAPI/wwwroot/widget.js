(function () {
  'use strict';

  var script = document.currentScript;
  if (!script) return;

  var dataBranch = script.getAttribute('data-branch');
  var dataWidgetId = script.getAttribute('data-widget-id');
  var dataColor = (script.getAttribute('data-color') || '88207a').replace(/^#/, '');
  var dataPosition = script.getAttribute('data-position') || 'bottom-right';

  var base = script.src ? script.src.replace(/\/widget\.js.*$/, '') : (window.location.origin || 'http://localhost:5002');
  var apiBase = base.replace(/\/$/, '');

  var STORAGE_TOKEN = 'company_widget_client_token';
  var STORAGE_EXPIRY = 'company_widget_client_expiry';
  var connection = null;

  function getStoredToken() {
    try {
      var token = localStorage.getItem(STORAGE_TOKEN);
      var expiry = localStorage.getItem(STORAGE_EXPIRY);
      if (!token || !expiry || new Date(expiry) <= new Date()) return null;
      return token;
    } catch (e) { return null; }
  }

  function setStoredToken(token, expiresAt) {
    try {
      localStorage.setItem(STORAGE_TOKEN, token);
      localStorage.setItem(STORAGE_EXPIRY, expiresAt);
    } catch (e) {}
  }

  function resolveWidgetId(cb) {
    if (dataWidgetId) {
      cb(null, dataWidgetId);
      return;
    }
    if (!dataBranch) {
      cb(new Error('data-branch or data-widget-id required'));
      return;
    }
    fetch(apiBase + '/api/v1/company/widget/by-branch/' + dataBranch)
      .then(function (r) {
        return r.json().then(function (json) {
          if (r.ok && json && json.data && json.data.widgetId) {
            cb(null, json.data.widgetId);
          } else {
            var msg = json && json.message
              ? (Array.isArray(json.message) ? json.message[0] : json.message)
              : 'No widget found for this branch.';
            cb(new Error(msg + ' Generate a widget in the admin panel (Dashboard → Widget → select branch → Generate widget).'));
          }
        });
      })
      .catch(function (err) {
        cb(err && err.message ? err : new Error('Network error. Is the API running at ' + apiBase + '?'));
      });
  }

  function createSession(widgetId, opts, cb) {
    fetch(apiBase + '/api/v1/company/widget/client/session', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        widgetId: widgetId,
        firstName: opts.firstName || null,
        lastName: opts.lastName || null,
        email: opts.email || null,
        mobile: opts.mobile || null
      })
    })
      .then(function (r) {
        return r.json().then(function (json) {
          if (r.ok && json && json.data && json.data.clientToken) {
            setStoredToken(json.data.clientToken, json.data.expiresAt);
            cb(null, json.data.clientToken);
          } else {
            var msg = json && json.message
              ? (Array.isArray(json.message) ? json.message[0] : json.message)
              : (r.status === 404 ? 'Widget not found. Generate a widget for this branch in the admin panel.' : 'Failed to start chat.');
            cb(new Error(msg));
          }
        });
      })
      .catch(function (err) {
        cb(err && err.message ? err : new Error('Network error. Is the API running at ' + apiBase + '?'));
      });
  }

  function loadHistory(token, cb) {
    fetch(apiBase + '/api/v1/company/widget/messages?limit=50', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'X-Company-Client-Token': token
      }
    })
      .then(function (r) {
        return r.json().then(function (json) {
          if (!r.ok || !json || json.success === false) {
            var msg = json && json.message
              ? (Array.isArray(json.message) ? json.message[0] : json.message)
              : 'Failed to load messages';
            throw new Error(msg);
          }
          return json;
        });
      })
      .then(function (json) {
        var items = (json && json.data) || [];
        try {
          items.sort(function (a, b) {
            var ta = new Date(a.sentAt || a.SentAt || 0).getTime();
            var tb = new Date(b.sentAt || b.SentAt || 0).getTime();
            return ta - tb;
          });
        } catch (e) {}
        messagesEl.innerHTML = '';
        for (var i = 0; i < items.length; i++) {
          var m = items[i];
          var content = (m && (m.content || m.Content)) || '';
          var sentAt = m.sentAt || m.SentAt;
          var hasSupportSender = !!(m.supportSenderId || m.SupportSenderId);
          var isSent = !hasSupportSender;
          var msgId = m.id || m.Id;
          var attachmentUrl = m.attachmentUrl || m.AttachmentUrl;
          var attachmentType = m.attachmentType || m.AttachmentType;
          var replyTo = m.replyToMessage || m.ReplyToMessage;
          var replyToContent = replyTo && (replyTo.content || replyTo.Content || replyTo.contentSnippet || replyTo.ContentSnippet)
            ? (replyTo.content || replyTo.Content || replyTo.contentSnippet || replyTo.ContentSnippet || '').slice(0, 60)
            : null;
          var reactions = m.messageReactions || m.MessageReactions || [];
          appendMsg(content, isSent, sentAt, { id: msgId, attachmentUrl: attachmentUrl, attachmentType: attachmentType, replyToContent: replyToContent, canReply: !isSent, reactions: reactions });
        }
        if (cb) cb(null);
      })
      .catch(function (err) {
        if (cb) cb(err && err.message ? err : new Error('Failed to load messages'));
      });
  }

  function sendMessage(token, content, cb, replyToMessageId, attachmentUrl, attachmentType) {
    var body = { content: content || '' };
    if (replyToMessageId) body.replyToMessageId = replyToMessageId;
    if (attachmentUrl) body.attachmentUrl = attachmentUrl;
    if (attachmentType) body.attachmentType = attachmentType;
    fetch(apiBase + '/api/v1/company/widget/messages', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Company-Client-Token': token
      },
      body: JSON.stringify(body)
    })
      .then(function (r) { return r.json(); })
      .then(function (json) {
        if (json && json.success) cb(null);
        else cb(new Error(json && json.message ? (Array.isArray(json.message) ? json.message[0] : json.message) : 'Send failed'));
      })
      .catch(cb);
  }

  var positionMap = {
    'bottom-right': { bottom: '20px', right: '20px', left: 'auto', top: 'auto' },
    'bottom-left': { bottom: '20px', left: '20px', right: 'auto', top: 'auto' },
    'top-right': { top: '20px', right: '20px', left: 'auto', bottom: 'auto' },
    'top-left': { top: '20px', left: '20px', right: 'auto', bottom: 'auto' }
  };
  var pos = positionMap[dataPosition] || positionMap['bottom-right'];

  var style = document.createElement('style');
  style.textContent = [
    '#cw-root { font-family: system-ui, -apple-system, sans-serif; font-size: 14px; box-sizing: border-box; }',
    '#cw-root * { box-sizing: border-box; }',
    '#cw-container { position: fixed; z-index: 2147483647; ' +
      'bottom: ' + pos.bottom + '; right: ' + pos.right + '; left: ' + pos.left + '; top: ' + pos.top + '; }',
    '#cw-toggle { width: 56px; height: 56px; border-radius: 50%; border: none; cursor: pointer; ' +
      'background: #' + dataColor + '; color: #fff; display: flex; align-items: center; justify-content: center; ' +
      'box-shadow: 0 4px 16px rgba(0,0,0,0.2); transition: transform 0.2s, background 0.2s; }',
    '#cw-toggle:hover { transform: scale(1.05); filter: brightness(1.1); }',
    '#cw-toggle svg { width: 28px; height: 28px; }',
    '#cw-panel { display: none; position: absolute; bottom: 68px; width: 360px; max-width: calc(100vw - 40px); height: 480px; max-height: 80vh; ' +
      'background: #fff; border-radius: 12px; box-shadow: 0 8px 32px rgba(0,0,0,0.12); flex-direction: column; overflow: hidden; ' +
      (dataPosition.indexOf('left') !== -1 ? 'left: 0; right: auto;' : 'right: 0; left: auto;') + ' }',
    '#cw-panel.open { display: flex; }',
    '#cw-header { padding: 14px 16px; border-bottom: 1px solid #eee; background: #' + dataColor + '; color: #fff; font-weight: 600; }',
    '#cw-close { background: none; border: none; color: inherit; cursor: pointer; padding: 4px; font-size: 20px; line-height: 1; opacity: 0.9; }',
    '#cw-close:hover { opacity: 1; }',
    '#cw-messages { flex: 1; overflow-y: auto; padding: 12px; display: flex; flex-direction: column; gap: 8px; background: #f5f5f5; }',
    '.cw-msg { max-width: 85%; padding: 10px 14px; border-radius: 14px; word-break: break-word; font-size: 14px; }',
    '.cw-msg.sent { align-self: flex-end; background: #' + dataColor + '; color: #fff; border-bottom-right-radius: 4px; }',
    '.cw-msg.recv { align-self: flex-start; background: #fff; border: 1px solid #eee; border-bottom-left-radius: 4px; }',
    '#cw-msg .time { font-size: 11px; opacity: 0.8; margin-top: 4px; }',
    '#cw-form-start { padding: 20px; display: flex; flex-direction: column; gap: 10px; }',
    '#cw-form-start label { font-size: 13px; color: #444; }',
    '#cw-form-start input { padding: 10px 12px; border: 1px solid #ddd; border-radius: 8px; font-size: 14px; }',
    '#cw-form-start button { padding: 12px; background: #' + dataColor + '; color: #fff; border: none; border-radius: 8px; cursor: pointer; font-weight: 500; }',
    '#cw-form-start button:disabled { cursor: not-allowed; opacity: 0.8; }',
    '#cw-form-start .err { color: #c00; font-size: 13px; min-height: 1.25em; margin-top: 8px; display: block; }',
    '#cw-input-row { padding: 10px 12px; border-top: 1px solid #eee; display: flex; gap: 8px; align-items: center; background: #fff; }',
    '#cw-input-row input[type="text"] { flex: 1; padding: 10px 14px; border: 1px solid #e0e0e0; border-radius: 20px; font-size: 14px; }',
    '#cw-input-row button.cw-send { width: 40px; height: 40px; border-radius: 50%; background: #' + dataColor + '; color: #fff; border: none; cursor: pointer; flex-shrink: 0; }',
    '.cw-reply-to { font-size: 11px; opacity: 0.9; margin-bottom: 4px; padding: 4px 6px; background: rgba(0,0,0,0.08); border-radius: 6px; }',
    '.cw-msg.recv .cw-reply-to { background: rgba(255,255,255,0.2); }',
    '.cw-attachment { display: block; font-size: 12px; margin-top: 4px; text-decoration: underline; opacity: 0.9; }',
    '.cw-reply-btn { margin-top: 4px; font-size: 11px; background: none; border: none; cursor: pointer; padding: 0; opacity: 0.8; }',
    '.cw-msg.sent .cw-reply-btn { color: rgba(255,255,255,0.9); }',
    '.cw-msg.recv .cw-reply-btn { color: #666; }',
    '#cw-reply-preview { display: none; padding: 6px 10px; margin: 0 12px 8px; background: #f5f5f5; border-radius: 8px; font-size: 12px; }',
    '#cw-reply-preview-text { margin: 0; }',
    '#cw-reply-preview-clear { background: none; border: none; cursor: pointer; padding: 2px 6px; float: right; font-size: 14px; color: #888; }',
    '.cw-reactions { display: flex; flex-wrap: wrap; gap: 4px; margin-top: 4px; font-size: 11px; }',
    '.cw-reaction-pill { display: inline-flex; align-items: center; gap: 2px; padding: 2px 6px; border-radius: 10px; background: rgba(0,0,0,0.08); }',
    '.cw-msg.recv .cw-reaction-pill { background: rgba(255,255,255,0.25); }',
    '.cw-reaction-emoji { font-size: 12px; }'
  ].join('\n');
  document.head.appendChild(style);

  var root = document.createElement('div');
  root.id = 'cw-root';
  root.innerHTML =
    '<div id="cw-container">' +
    '  <div id="cw-panel">' +
    '    <div id="cw-header" style="display:flex;align-items:center;justify-content:space-between;">' +
    '      <span>Chat</span>' +
    '      <button type="button" id="cw-close" aria-label="Close">&times;</button>' +
    '    </div>' +
    '    <div id="cw-form-start">' +
    '      <label>Name (optional)</label>' +
    '      <input type="text" id="cw-name" placeholder="Your name" />' +
    '      <label>Email or Mobile (optional)</label>' +
    '      <input type="text" id="cw-contact" placeholder="Email or phone" />' +
    '      <div class="err" id="cw-start-err"></div>' +
    '      <button type="button" id="cw-start-btn">Start chat</button>' +
    '    </div>' +
    '    <div id="cw-chat-area" style="display:none; flex:1; flex-direction:column; overflow:hidden;">' +
    '      <div id="cw-messages"></div>' +
    '      <div id="cw-reply-preview"><span id="cw-reply-preview-text"></span><button type="button" id="cw-reply-preview-clear" aria-label="Clear" style="float:right;background:none;border:none;cursor:pointer;padding:2px 6px;">&#215;</button></div>' +
    '      <div id="cw-input-row">' +
    '        <input type="file" id="cw-file" accept="image/*,.pdf,.doc,.docx,.txt" style="display:none" />' +
    '        <button type="button" id="cw-attach" aria-label="Attach" style="background:transparent;color:#666;padding:6px;">&#128206;</button>' +
    '        <input type="text" id="cw-input" placeholder="Type a message..." maxlength="2000" />' +
    '        <button type="button" id="cw-send" class="cw-send" aria-label="Send">&#9650;</button>' +
    '      </div>' +
    '    </div>' +
    '  </div>' +
    '  <button type="button" id="cw-toggle" aria-label="Open chat">' +
    '    <svg viewBox="0 0 24 24" fill="currentColor"><path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H5.17L4 17.17V4h16v12z"/></svg>' +
    '  </button>' +
    '</div>';
  document.body.appendChild(root);

  var panel = document.getElementById('cw-panel');
  var toggle = document.getElementById('cw-toggle');
  var closeBtn = document.getElementById('cw-close');
  var formStart = document.getElementById('cw-form-start');
  var chatArea = document.getElementById('cw-chat-area');
  var messagesEl = document.getElementById('cw-messages');
  var inputEl = document.getElementById('cw-input');
  var sendBtn = document.getElementById('cw-send');
  var attachBtn = document.getElementById('cw-attach');
  var fileInput = document.getElementById('cw-file');
  var startErr = document.getElementById('cw-start-err');
  var startBtn = document.getElementById('cw-start-btn');
  var nameInput = document.getElementById('cw-name');
  var contactInput = document.getElementById('cw-contact');

  var clientToken = null;

  function showStartForm() {
    formStart.style.display = 'flex';
    chatArea.style.display = 'none';
    startErr.textContent = '';
  }

  function showChat() {
    formStart.style.display = 'none';
    chatArea.style.display = 'flex';
  }

  function reactionDisplayName(r) {
    var n = r.userName || r.UserName || r.supportUserName || r.SupportUserName;
    if (n) return n;
    var u = r.user || r.User;
    if (u) return [u.firstName || u.FirstName, u.lastName || u.LastName].filter(Boolean).join(' ') || u.username || u.Username || '';
    var s = r.supportUser || r.SupportUser;
    if (s) return [s.firstName || s.FirstName, s.lastName || s.LastName].filter(Boolean).join(' ') || s.username || s.Username || '';
    return '';
  }

  function buildReactionsInnerHtml(reactions) {
    if (!reactions || !reactions.length) return '';
    var list = Array.isArray(reactions) ? reactions : [];
    var seen = {};
    var parts = [];
    for (var i = 0; i < list.length; i++) {
      var r = list[i];
      var emoji = (r.emoji || r.Emoji || '').trim();
      if (!emoji) continue;
      var name = reactionDisplayName(r);
      var key = emoji + '\0' + name;
      if (seen[key]) continue;
      seen[key] = true;
      parts.push('<span class="cw-reaction-pill"><span class="cw-reaction-emoji">' + escapeHtml(emoji) + '</span>' + (name ? '<span class="cw-reaction-name">' + escapeHtml(name) + '</span>' : '') + '</span>');
    }
    return parts.join('');
  }

  function buildReactionsHtml(reactions) {
    var inner = buildReactionsInnerHtml(reactions);
    return inner ? '<div class="cw-reactions">' + inner + '</div>' : '';
  }

  function formatMessageTime(isoOrDate) {
    if (isoOrDate == null) return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    var d = typeof isoOrDate === 'string' ? new Date(isoOrDate) : isoOrDate;
    if (isNaN(d.getTime())) return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    var now = new Date();
    var today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    var msgDay = new Date(d.getFullYear(), d.getMonth(), d.getDate());
    var timeStr = d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    if (msgDay.getTime() === today.getTime()) return 'Today ' + timeStr;
    var yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);
    if (msgDay.getTime() === yesterday.getTime()) return 'Yesterday ' + timeStr;
    return d.toLocaleDateString([], { month: 'short', day: 'numeric' }) + ', ' + timeStr;
  }

  function appendMsg(content, isSent, time, opts) {
    opts = opts || {};
    var div = document.createElement('div');
    div.className = 'cw-msg ' + (isSent ? 'sent' : 'recv');
    if (opts.id) div.setAttribute('data-message-id', opts.id);
    var t = formatMessageTime(time);
    var html = '';
    if (opts.replyToContent) html += '<div class="cw-reply-to">Reply to: ' + escapeHtml(opts.replyToContent) + '</div>';
    html += '<span class="text"></span>';
    if (opts.attachmentUrl) html += '<a class="cw-attachment" href="' + escapeHtml(opts.attachmentUrl) + '" target="_blank" rel="noopener">Attachment</a>';
    html += buildReactionsHtml(opts.reactions);
    html += '<div class="time">' + escapeHtml(t) + '</div>';
    if (opts.canReply && opts.id) html += '<button type="button" class="cw-reply-btn" data-msg-id="' + escapeHtml(opts.id) + '" data-msg-content="' + escapeHtml((content || '').slice(0, 80)) + '">Reply</button>';
    div.innerHTML = html;
    div.querySelector('.text').textContent = content || '\u00A0';
    messagesEl.appendChild(div);
    messagesEl.scrollTop = messagesEl.scrollHeight;
    if (opts.canReply && opts.id) {
      var replyBtn = div.querySelector('.cw-reply-btn');
      if (replyBtn) replyBtn.addEventListener('click', function () {
        var id = this.getAttribute('data-msg-id');
        var preview = this.getAttribute('data-msg-content');
        if (typeof onReplyToMessage === 'function') onReplyToMessage(id, preview);
      });
    }
  }

  function updateMessageReactions(messageId, reactions) {
    if (!messagesEl) return;
    var row = messagesEl.querySelector('[data-message-id="' + messageId + '"]');
    if (!row) return;
    var container = row.querySelector('.cw-reactions');
    var inner = buildReactionsInnerHtml(reactions);
    if (container) {
      container.innerHTML = inner;
      if (!inner) container.remove();
    } else if (inner) {
      var timeEl = row.querySelector('.time');
      var wrap = document.createElement('div');
      wrap.className = 'cw-reactions';
      wrap.innerHTML = inner;
      if (timeEl && timeEl.parentNode) timeEl.parentNode.insertBefore(wrap, timeEl);
    }
  }
  function escapeHtml(s) {
    if (!s) return '';
    var div = document.createElement('div');
    div.textContent = s;
    return div.innerHTML;
  }
  var replyToMessageId = null;
  var replyToPreview = null;
  var attachmentUrl = null;
  var attachmentType = null;
  function onReplyToMessage(id, preview) {
    replyToMessageId = id;
    replyToPreview = preview || '…';
    updateReplyPreview();
  }
  function updateReplyPreview() {
    var wrap = document.getElementById('cw-reply-preview');
    if (!wrap) return;
    if (replyToPreview || attachmentUrl) {
      wrap.style.display = 'block';
      var text = document.getElementById('cw-reply-preview-text');
      var parts = [];
      if (replyToPreview) parts.push('Replying: ' + replyToPreview);
      if (attachmentUrl) parts.push('Attachment attached');
      if (text) text.textContent = parts.join(' \u00B7 ');
      var clearBtn = document.getElementById('cw-reply-preview-clear');
      if (clearBtn) clearBtn.style.display = 'inline';
    } else {
      wrap.style.display = 'none';
    }
  }

  function openPanel() {
    panel.classList.add('open');
  }

  function closePanel() {
    panel.classList.remove('open');
  }

  toggle.addEventListener('click', openPanel);
  closeBtn.addEventListener('click', closePanel);

  function ensureSignalR(callback) {
    if (window.signalR && window.signalR.HubConnectionBuilder) {
      callback();
      return;
    }
    var existing = document.querySelector('script[data-cw-signalr="1"]');
    if (existing) {
      existing.addEventListener('load', function () { callback(); });
      return;
    }
    var s = document.createElement('script');
    s.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js';
    s.async = true;
    s.setAttribute('data-cw-signalr', '1');
    s.onload = function () { callback(); };
    s.onerror = function () { callback(); };
    document.head.appendChild(s);
  }

  function connectSignalR(token) {
    ensureSignalR(function () {
      if (!window.signalR || !window.signalR.HubConnectionBuilder) {
        return;
      }
      var hubUrl = apiBase.replace(/\/$/, '') + '/companyWidgetHub';
      var url = hubUrl + '?access_token=' + encodeURIComponent(token);
      if (connection) {
        try { connection.stop(); } catch (e) {}
        connection = null;
      }
      connection = new window.signalR.HubConnectionBuilder()
        .withUrl(url)
        .withAutomaticReconnect()
        .build();

      connection.on('ReceiveMessage', function (msg) {
        if (!msg) return;
        var content = (msg.content || msg.Content) || '';
        var senderId = msg.senderId || msg.SenderId;
        var targetId = msg.targetReceiverUserId || msg.TargetReceiverUserId;
        var isSent = senderId && targetId && String(senderId) === String(targetId);
        var msgId = msg.messageId || msg.MessageId;
        var attachmentUrl = msg.attachmentUrl || msg.AttachmentUrl;
        var attachmentType = msg.attachmentType || msg.AttachmentType;
        var replyTo = msg.replyToMessage || msg.ReplyToMessage;
        var replyToContent = replyTo && (replyTo.content || replyTo.Content || replyTo.contentSnippet || replyTo.ContentSnippet)
          ? (replyTo.content || replyTo.Content || replyTo.contentSnippet || replyTo.ContentSnippet || '').slice(0, 60)
          : null;
        var reactions = msg.reactions || msg.Reactions || msg.messageReactions || msg.MessageReactions || [];
        appendMsg(content, isSent, msg.sentAt || msg.SentAt, { id: msgId, attachmentUrl: attachmentUrl, attachmentType: attachmentType, replyToContent: replyToContent, canReply: !isSent, reactions: reactions });
      });

      connection.on('MessageReactionsUpdated', function (payload) {
        if (!payload || !payload.messageId) return;
        var msgId = payload.messageId;
        var reactions = payload.reactions || payload.Reactions || [];
        if (typeof updateMessageReactions === 'function') updateMessageReactions(msgId, reactions);
      });

      connection.start().catch(function () { });
    });
  }

  function tryStartWithStoredToken() {
    clientToken = getStoredToken();
    if (clientToken) {
      showChat();
      loadHistory(clientToken);
      connectSignalR(clientToken);
      return;
    }
    showStartForm();
  }

  var resolvedWidgetId = null;
  var resolveError = null;

  function onStartChatClick() {
    startErr.textContent = '';
    var name = (nameInput && nameInput.value) ? nameInput.value.trim() : '';
    var contact = (contactInput && contactInput.value) ? contactInput.value.trim() : '';
    var firstName = name ? name.split(/\s+/)[0] : '';
    var lastName = name ? name.split(/\s+/).slice(1).join(' ') : '';
    var email = contact && contact.indexOf('@') !== -1 ? contact : null;
    var mobile = contact && contact.indexOf('@') === -1 ? contact : null;

    function doCreateSession(wid) {
      startBtn.disabled = true;
      startBtn.textContent = 'Starting…';
      createSession(wid, { firstName: firstName, lastName: lastName, email: email, mobile: mobile }, function (err2, token) {
        startBtn.disabled = false;
        startBtn.textContent = 'Start chat';
        if (err2) {
          startErr.textContent = err2.message || 'Failed to start chat.';
          startErr.style.display = 'block';
          return;
        }
        clientToken = token;
        showChat();
        loadHistory(clientToken);
        connectSignalR(clientToken);
      });
    }

    if (resolvedWidgetId) {
      doCreateSession(resolvedWidgetId);
      return;
    }
    if (dataWidgetId) {
      doCreateSession(dataWidgetId);
      return;
    }
    if (!dataBranch) {
      startErr.textContent = 'Widget is not configured (missing branch).';
      startErr.style.display = 'block';
      return;
    }
    startErr.textContent = 'Resolving…';
    startErr.style.display = 'block';
    resolveWidgetId(function (err, wid) {
      startErr.textContent = '';
      startErr.style.display = 'none';
      if (err) {
        startErr.textContent = err.message || 'Could not load widget.';
        startErr.style.display = 'block';
        return;
      }
      resolvedWidgetId = wid;
      doCreateSession(wid);
    });
  }

  startBtn.addEventListener('click', onStartChatClick);

  if (attachBtn && fileInput) {
    attachBtn.addEventListener('click', function () { fileInput.click(); });
    fileInput.addEventListener('change', function () {
      var file = fileInput.files && fileInput.files[0];
      if (!file || !clientToken) return;
      fileInput.value = '';
      var formData = new FormData();
      formData.append('file', file);
      fetch(apiBase.replace(/\/$/, '') + '/api/v1/company/widget/client/upload', {
        method: 'POST',
        headers: { 'X-Company-Client-Token': clientToken },
        body: formData
      })
        .then(function (r) { return r.json(); })
        .then(function (json) {
          if (json && json.success && json.data && json.data.url) {
            attachmentUrl = json.data.url;
            attachmentType = file.type || 'application/octet-stream';
            updateReplyPreview();
          }
        })
        .catch(function () {});
    });
  }

  var replyPreviewClear = document.getElementById('cw-reply-preview-clear');
  if (replyPreviewClear) {
    replyPreviewClear.addEventListener('click', function () {
      replyToMessageId = null;
      replyToPreview = null;
      attachmentUrl = null;
      attachmentType = null;
      updateReplyPreview();
    });
  }

  sendBtn.addEventListener('click', function () {
    var content = (inputEl && inputEl.value) ? inputEl.value.trim() : '';
    if ((!content && !attachmentUrl) || !clientToken) return;
    inputEl.value = '';
    var rid = replyToMessageId;
    var aurl = attachmentUrl;
    var atype = attachmentType;
    replyToMessageId = null;
    replyToPreview = null;
    attachmentUrl = null;
    attachmentType = null;
    updateReplyPreview();
    appendMsg(content || (aurl ? 'Attachment' : ''), true, null, { attachmentUrl: aurl, attachmentType: atype });
    sendMessage(clientToken, content, function (err3) {
      if (err3) appendMsg('Failed to send: ' + (err3.message || 'error'), true);
    }, rid, aurl, atype);
  });

  inputEl.addEventListener('keydown', function (e) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendBtn.click();
    }
  });

  resolveWidgetId(function (err, widgetId) {
    if (err) {
      resolveError = err;
      startErr.textContent = err.message || 'Could not load widget. Click "Start chat" to retry.';
      startErr.style.display = 'block';
    } else {
      resolvedWidgetId = widgetId;
      startErr.textContent = '';
      startErr.style.display = 'none';
    }
    tryStartWithStoredToken();
  });
})();
