/**
 * DataChat Embed — 单文件可嵌入 AI 侧栏
 * 用法: DataChatEmbed.init({ gatewayUrl, getToken, domain, userName, ... })
 */
(function (global) {
  'use strict';

  const STORAGE_PREFIX = 'datachat:';
  const DEFAULT_WIDTH = 400;

  function uid() {
    return 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
      const r = (Math.random() * 16) | 0;
      return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16);
    });
  }

  function timeGreeting() {
    const h = new Date().getHours();
    if (h < 12) return '上午';
    if (h < 18) return '下午';
    return '晚上';
  }

  function loadSession(domain) {
    const key = STORAGE_PREFIX + 'session:' + domain;
    try {
      const raw = localStorage.getItem(key);
      if (raw) return JSON.parse(raw);
    } catch (_) {}
    return { id: uid(), messages: [] };
  }

  function saveSession(domain, session) {
    localStorage.setItem(STORAGE_PREFIX + 'session:' + domain, JSON.stringify(session));
  }

  function buildHistory(messages, systemPrompt) {
    const list = [];
    if (systemPrompt) list.push({ role: 'system', content: systemPrompt });
    const recent = messages.filter(function (m) {
      return m.role === 'user' || m.role === 'assistant';
    }).slice(-40);
    recent.forEach(function (m) {
      list.push({ role: m.role, content: m.content });
    });
    return list;
  }

  function injectHostStyles() {
    if (document.getElementById('datachat-host-styles')) return;
    var s = document.createElement('style');
    s.id = 'datachat-host-styles';
    s.textContent =
      '.datachat-shell{position:fixed;top:0;right:0;height:100vh;z-index:99999;' +
      'transform:translateX(100%);transition:transform .25s ease;box-sizing:border-box;}' +
      '.datachat-shell.open{transform:translateX(0);}' +
      '.datachat-shell.collapsed{transform:translateX(calc(100% - 52px));}' +
      '.datachat-launcher{position:fixed;right:20px;bottom:24px;width:52px;height:52px;border-radius:50%;' +
      'border:none;cursor:pointer;z-index:99998;background:linear-gradient(135deg,#6366f1,#4f46e5);' +
      'color:#fff;font-size:22px;box-shadow:0 6px 20px rgba(79,70,229,.4);}';
    document.head.appendChild(s);
  }

  const STYLES = `
*, *::before, *::after { box-sizing: border-box; }
.panel {
  display: flex; flex-direction: column; height: 100vh; width: 100%; background: #fff;
  border-left: 1px solid #e8eaef; box-shadow: -4px 0 24px rgba(15,23,42,.08);
  color: #1f2937; font-size: 14px;
  font-family: "Segoe UI", "PingFang SC", "Microsoft YaHei", sans-serif;
}
.header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 14px; border-bottom: 1px solid #f0f2f5; flex-shrink: 0;
}
.header-left, .header-right { display: flex; gap: 8px; align-items: center; }
.icon-btn {
  width: 32px; height: 32px; border: none; background: #f4f6fa; border-radius: 8px;
  cursor: pointer; color: #64748b; font-size: 16px; line-height: 1;
}
.icon-btn:hover { background: #e8edf5; color: #334155; }
.body { flex: 1; min-height: 0; overflow: hidden; display: flex; flex-direction: column; }
.welcome {
  flex: 1; min-height: 0; display: flex; flex-direction: column; align-items: center;
  justify-content: flex-start; padding: 48px 24px 24px; text-align: center;
}
.orb {
  width: 72px; height: 72px; border-radius: 50%;
  background: radial-gradient(circle at 30% 30%, #a78bfa, #6366f1 45%, #38bdf8);
  box-shadow: 0 8px 32px rgba(99,102,241,.35); margin-bottom: 20px;
}
.greet { font-size: 22px; font-weight: 600; color: #111827; margin: 0; }
.messages {
  flex: 1; min-height: 0; overflow-y: auto; padding: 16px; display: none; flex-direction: column; gap: 12px;
}
.messages.active { display: flex; }
.welcome.hidden { display: none; }
.msg { max-width: 88%; padding: 10px 12px; border-radius: 12px; line-height: 1.55; white-space: pre-wrap; word-break: break-word; }
.msg.user { align-self: flex-end; background: linear-gradient(135deg,#6366f1,#4f46e5); color: #fff; }
.msg.assistant { align-self: flex-start; background: #f8fafc; border: 1px solid #e2e8f0; }
.msg.assistant .code {
  display: block; margin: 8px 0; padding: 10px 12px; background: #0f172a; color: #e2e8f0;
  border-radius: 8px; font-size: 12px; overflow-x: auto; white-space: pre-wrap;
}
.msg.assistant .data-table {
  width: 100%; border-collapse: collapse; margin: 10px 0; font-size: 13px;
}
.msg.assistant .data-table th, .msg.assistant .data-table td {
  border: 1px solid #e2e8f0; padding: 6px 8px; text-align: left;
}
.msg.assistant .data-table th { background: #f1f5f9; font-weight: 600; }
.msg.streaming { border-color: #93c5fd; }
.footer-area { flex-shrink: 0; padding: 12px 14px 10px; border-top: 1px solid #f0f2f5; }
.input-wrap {
  display: flex; align-items: flex-end; gap: 8px; background: #f8fafc;
  border: 1px solid #e2e8f0; border-radius: 14px; padding: 10px 12px;
}
.input-wrap:focus-within { border-color: #818cf8; box-shadow: 0 0 0 3px rgba(99,102,241,.15); }
textarea.input {
  flex: 1; border: none; background: transparent; resize: none; outline: none;
  font: inherit; min-height: 24px; max-height: 120px; color: #1e293b;
}
.input-actions { display: flex; gap: 4px; align-items: center; }
.chips { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 12px; }
.chip {
  padding: 8px 14px; border-radius: 20px; border: 1px solid #e2e8f0; background: #fff;
  color: #475569; font-size: 13px; cursor: pointer; transition: .15s;
}
.chip:hover { border-color: #a5b4fc; color: #4f46e5; background: #f5f3ff; }
.chip.wide { flex: 1 1 100%; text-align: center; }
.disclaimer {
  margin-top: 10px; font-size: 11px; color: #94a3b8; text-align: center;
}
.disclaimer a { color: #6366f1; text-decoration: none; }
`;

  function parseSseDelta(line) {
    if (!line.startsWith('data:')) return null;
    const data = line.slice(5).trim();
    if (data === '[DONE]') return { done: true };
    try {
      const j = JSON.parse(data);
      if (j.delta) return { text: j.delta };
      if (j.choices && j.choices[0] && j.choices[0].delta && j.choices[0].delta.content)
        return { text: j.choices[0].delta.content };
      if (j.content) return { text: j.content };
    } catch (_) {}
    return null;
  }

  function escapeHtml(text) {
    return String(text)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  function formatAssistantHtml(text) {
    if (!text) return '';
    var parts = [];
    var re = /```(\w*)\n([\s\S]*?)```/g;
    var last = 0;
    var m;
    while ((m = re.exec(text)) !== null) {
      parts.push({ type: 'text', value: text.slice(last, m.index) });
      parts.push({ type: 'code', lang: m[1], value: m[2] });
      last = m.index + m[0].length;
    }
    parts.push({ type: 'text', value: text.slice(last) });

    return parts.map(function (part) {
      if (part.type === 'code') {
        return '<pre class="code"><code>' + escapeHtml(part.value.trim()) + '</code></pre>';
      }
      return formatMarkdownTables(escapeHtml(part.value)).replace(/\n/g, '<br>');
    }).join('');
  }

  function formatMarkdownTables(text) {
    var lines = text.split('\n');
    var out = [];
    var tableRows = [];

    function flushTable() {
      if (tableRows.length < 2) {
        out.push.apply(out, tableRows);
        tableRows = [];
        return;
      }
      var header = tableRows[0].split('|').filter(function (c) { return c.trim(); }).map(function (c) { return c.trim(); });
      var body = tableRows.slice(2).map(function (row) {
        return row.split('|').filter(function (c) { return c.trim(); }).map(function (c) { return c.trim(); });
      });
      var html = '<table class="data-table"><thead><tr>';
      header.forEach(function (h) { html += '<th>' + h + '</th>'; });
      html += '</tr></thead><tbody>';
      body.forEach(function (r) {
        html += '<tr>';
        r.forEach(function (c) { html += '<td>' + c + '</td>'; });
        html += '</tr>';
      });
      html += '</tbody></table>';
      out.push(html);
      tableRows = [];
    }

    lines.forEach(function (line) {
      if (/^\|.*\|$/.test(line.trim())) tableRows.push(line.trim());
      else {
        flushTable();
        out.push(line);
      }
    });
    flushTable();
    return out.join('\n');
  }

  function setAssistantContent(el, content) {
    el.innerHTML = formatAssistantHtml(content);
  }

  async function streamChat(cfg, session, userMessage, onDelta, signal) {
    const history = buildHistory(session.messages, cfg.systemPrompt);
    history.push({ role: 'user', content: userMessage });
    const token = typeof cfg.getToken === 'function' ? await cfg.getToken() : cfg.token;
    const url = (cfg.gatewayUrl || '').replace(/\/$/, '') + '/v1/chat/stream';
    const res = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: token ? 'Bearer ' + token : ''
      },
      body: JSON.stringify({
        sessionId: session.id,
        domain: cfg.domain,
        message: userMessage,
        stream: true,
        messages: history
      }),
      signal: signal
    });
    if (!res.ok) {
      const t = await res.text();
      throw new Error('网关错误 ' + res.status + (t ? ': ' + t.slice(0, 200) : ''));
    }
    const reader = res.body.getReader();
    const decoder = new TextDecoder();
    let buf = '';
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      buf += decoder.decode(value, { stream: true });
      const lines = buf.split('\n');
      buf = lines.pop() || '';
      for (let i = 0; i < lines.length; i++) {
        const p = parseSseDelta(lines[i].trim());
        if (!p) continue;
        if (p.done) return;
        if (p.text) onDelta(p.text);
      }
    }
  }

  function createWidget(cfg) {
    injectHostStyles();
    const width = cfg.width || DEFAULT_WIDTH;
    const zIndex = cfg.zIndex || 99999;
    const shell = document.createElement('div');
    shell.className = 'datachat-shell';
    shell.style.width = width + 'px';
    shell.style.zIndex = String(zIndex);

    const launcher = document.createElement('button');
    launcher.className = 'datachat-launcher';
    launcher.type = 'button';
    launcher.title = '智能助手';
    launcher.textContent = '✦';
    launcher.setAttribute('aria-label', '打开智能助手');

    const shadow = shell.attachShadow({ mode: 'closed' });
    const style = document.createElement('style');
    style.textContent = STYLES;
    shadow.appendChild(style);

    const panel = document.createElement('div');
    panel.className = 'panel';
    panel.innerHTML = `
      <div class="header">
        <div class="header-left">
          <button type="button" class="icon-btn" data-action="new" title="新会话">+</button>
          <button type="button" class="icon-btn" data-action="history" title="会话">💬</button>
        </div>
        <div class="header-right">
          <button type="button" class="icon-btn" data-action="collapse" title="收起">›</button>
          <button type="button" class="icon-btn" data-action="close" title="关闭">×</button>
        </div>
      </div>
      <div class="body">
        <div class="welcome">
          <div class="orb"></div>
          <p class="greet" data-greet></p>
        </div>
        <div class="messages"></div>
      </div>
      <div class="footer-area">
        <div class="input-wrap">
          <textarea class="input" rows="1" placeholder=""></textarea>
          <div class="input-actions">
            <button type="button" class="icon-btn" data-action="attach" title="附件">📁</button>
            <button type="button" class="icon-btn" data-action="send" title="发送">➤</button>
          </div>
        </div>
        <div class="chips" data-chips></div>
        <p class="disclaimer">AI生成内容可能有误，请谨慎甄别使用 | <a href="#" data-privacy>隐私</a></p>
      </div>
    `;
    shadow.appendChild(panel);

    const greetEl = panel.querySelector('[data-greet]');
    const welcome = panel.querySelector('.welcome');
    const messagesEl = panel.querySelector('.messages');
    const input = panel.querySelector('textarea.input');
    const chipsEl = panel.querySelector('[data-chips]');

    const name = cfg.userName || '您';
    greetEl.textContent = name + '，' + timeGreeting() + '好！';
    input.placeholder = cfg.placeholder || '@智能体，输入您的问题…';

    (cfg.quickPrompts || []).forEach(function (text, i) {
      const chip = document.createElement('button');
      chip.type = 'button';
      chip.className = 'chip' + (text.length > 18 ? ' wide' : '');
      chip.textContent = text;
      chip.addEventListener('click', function () {
        input.value = text;
        send();
      });
      chipsEl.appendChild(chip);
    });

    let session = loadSession(cfg.domain);
    let abortCtrl = null;
    let sending = false;

    function renderMessages() {
      messagesEl.innerHTML = '';
      session.messages.forEach(function (m) {
        const div = document.createElement('div');
        div.className = 'msg ' + m.role;
        if (m.role === 'assistant') setAssistantContent(div, m.content);
        else div.textContent = m.content;
        messagesEl.appendChild(div);
      });
      messagesEl.scrollTop = messagesEl.scrollHeight;
    }

    function showChat() {
      welcome.classList.add('hidden');
      messagesEl.classList.add('active');
    }

    function openPanel() {
      shell.classList.add('open');
      shell.classList.remove('collapsed');
      launcher.style.display = 'none';
    }

    function closePanel() {
      shell.classList.remove('open');
      launcher.style.display = '';
      if (typeof cfg.onClose === 'function') cfg.onClose();
    }

    async function send() {
      const text = (input.value || '').trim();
      if (!text || sending) return;
      sending = true;
      input.value = '';
      showChat();

      session.messages.push({ role: 'user', content: text, id: uid() });
      const assistant = { role: 'assistant', content: '', id: uid() };
      session.messages.push(assistant);
      saveSession(cfg.domain, session);
      renderMessages();
      const assistantEl = messagesEl.lastElementChild;
      assistantEl.classList.add('streaming');

      abortCtrl = new AbortController();
      try {
        await streamChat(cfg, session, text, function (delta) {
          assistant.content += delta;
          setAssistantContent(assistantEl, assistant.content);
          messagesEl.scrollTop = messagesEl.scrollHeight;
        }, abortCtrl.signal);
      } catch (e) {
        if (e.name !== 'AbortError') {
          assistant.content += '\n[错误] ' + (e.message || String(e));
          setAssistantContent(assistantEl, assistant.content);
        }
      } finally {
        assistantEl.classList.remove('streaming');
        saveSession(cfg.domain, session);
        sending = false;
        abortCtrl = null;
      }
    }

    panel.querySelector('[data-action="send"]').addEventListener('click', send);
    input.addEventListener('keydown', function (e) {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        send();
      }
    });
    panel.querySelector('[data-action="new"]').addEventListener('click', function () {
      session = { id: uid(), messages: [] };
      saveSession(cfg.domain, session);
      welcome.classList.remove('hidden');
      messagesEl.classList.remove('active');
      messagesEl.innerHTML = '';
    });
    panel.querySelector('[data-action="close"]').addEventListener('click', closePanel);
    panel.querySelector('[data-action="collapse"]').addEventListener('click', function () {
      shell.classList.toggle('collapsed');
    });
    panel.querySelector('[data-action="attach"]').addEventListener('click', function () {
      if (typeof cfg.onAttach === 'function') cfg.onAttach();
      else alert('附件上传将在后续版本由网关统一接入');
    });
    panel.querySelector('[data-privacy]').addEventListener('click', function (e) {
      e.preventDefault();
      if (cfg.privacyUrl) window.open(cfg.privacyUrl, '_blank');
    });

    launcher.addEventListener('click', openPanel);

    if (session.messages.length > 0) {
      showChat();
      renderMessages();
    }

    const mount = cfg.container ? document.querySelector(cfg.container) : document.body;
    mount.appendChild(launcher);
    mount.appendChild(shell);

    if (cfg.autoOpen) openPanel();

    return {
      open: openPanel,
      close: closePanel,
      destroy: function () {
        if (abortCtrl) abortCtrl.abort();
        launcher.remove();
        shell.remove();
      },
      getSession: function () { return session; }
    };
  }

  function loadDomainFromGateway(cfg) {
    var base = (cfg.gatewayUrl || '').replace(/\/$/, '');
    var token = typeof cfg.getToken === 'function' ? cfg.getToken() : cfg.token;
    var headers = { Accept: 'application/json' };
    if (token) headers.Authorization = 'Bearer ' + token;
    return fetch(base + '/v1/domains/' + encodeURIComponent(cfg.domain), { headers: headers })
      .then(function (res) {
        if (!res.ok) return cfg;
        return res.json();
      })
      .then(function (d) {
        if (!d || !d.id) return cfg;
        var merged = Object.assign({}, cfg);
        if (d.placeholder && !merged.placeholder) merged.placeholder = d.placeholder;
        if (d.quickPrompts && d.quickPrompts.length && (!merged.quickPrompts || !merged.quickPrompts.length))
          merged.quickPrompts = d.quickPrompts;
        if (d.displayName && !merged.userName) merged.userName = merged.userName;
        return merged;
      })
      .catch(function () { return cfg; });
  }

  const instances = [];

  global.DataChatEmbed = {
    init: function (options) {
      if (!options || !options.gatewayUrl || !options.domain) {
        throw new Error('DataChatEmbed.init 需要 gatewayUrl 与 domain');
      }
      if (!options.getToken && !options.token) {
        console.warn('[DataChat] 建议提供 getToken()，避免在页面写死密钥');
      }
      var useGatewayConfig = options.loadConfigFromGateway !== false;
      if (useGatewayConfig) {
        return loadDomainFromGateway(options).then(function (merged) {
          var api = createWidget(merged);
          instances.push(api);
          return api;
        });
      }
      var api = createWidget(options);
      instances.push(api);
      return api;
    },
    destroyAll: function () {
      while (instances.length) instances.pop().destroy();
    }
  };
})(typeof window !== 'undefined' ? window : globalThis);
