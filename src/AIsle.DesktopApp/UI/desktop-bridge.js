(() => {
  'use strict';
  if (!window.chrome?.webview || window.aisleBridge) return;

  const pending = new Map();
  const nativeFetch = window.fetch.bind(window);
  let sequence = 0;

  function request(type, payload = {}) {
    const requestId = `desktop-${Date.now()}-${++sequence}`;
    return new Promise((resolve, reject) => {
      pending.set(requestId, { resolve, reject });
      window.chrome.webview.postMessage({ requestId, type, payload });
    });
  }

  window.chrome.webview.addEventListener('message', event => {
    const response = event.data;
    const waiter = response && pending.get(response.requestId);
    if (!waiter) return;
    pending.delete(response.requestId);
    if (response.ok) waiter.resolve(response.payload);
    else waiter.reject(new Error(response.error?.message || 'Desktop bridge request failed.'));
  });

  function jsonResponse(value, status = 200) {
    return new Response(JSON.stringify(value), {
      status,
      headers: { 'Content-Type': 'application/json; charset=utf-8' }
    });
  }

  window.fetch = async (input, options = {}) => {
    const rawUrl = input instanceof Request ? input.url : String(input);
    const url = new URL(rawUrl, window.location.href);
    if (url.pathname !== '/api/project') return nativeFetch(input, options);

    const method = String(options.method || (input instanceof Request ? input.method : 'GET')).toUpperCase();
    if (method === 'GET') {
      const result = await request('project.load', {});
      return jsonResponse({ layout: result.project.layout, catalog: result.project.catalog });
    }

    if (method === 'POST') {
      const body = typeof options.body === 'string' ? JSON.parse(options.body) : options.body;
      const result = await request('project.save', {
        project: {
          schemaVersion: 'aisle.project.v1',
          layout: body?.layout,
          catalog: body?.catalog
        }
      });
      return jsonResponse({
        ok: true,
        warnings: result.validation?.warnings || [],
        unreachableShelfIds: result.validation?.unreachableShelfIds || []
      });
    }

    return jsonResponse({ error: `Unsupported project method: ${method}` }, 405);
  };

  window.aisleBridge = Object.freeze({ request });

  document.addEventListener('DOMContentLoaded', async () => {
    const badge = document.createElement('div');
    badge.id = 'desktop-bridge-status';
    badge.setAttribute('role', 'status');
    Object.assign(badge.style, {
      position: 'fixed', right: '12px', bottom: '8px', zIndex: '2147483647',
      padding: '5px 9px', border: '1px solid #6b3519', borderRadius: '4px',
      background: '#2e1509', color: '#ffca58', font: '11px Consolas, monospace'
    });
    badge.textContent = 'Desktop bridge: connecting';
    document.body.appendChild(badge);

    try {
      const response = await request('app.ping', {});
      badge.textContent = response?.status === 'ready' ? 'Desktop bridge: ready' : 'Desktop bridge: invalid response';
      document.documentElement.dataset.desktopBridge = response?.status || 'invalid';
    } catch (error) {
      badge.textContent = 'Desktop bridge: unavailable';
      document.documentElement.dataset.desktopBridge = 'error';
      console.error(error);
    }
  }, { once: true });
})();
