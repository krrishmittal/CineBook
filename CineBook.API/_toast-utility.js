/* ================================================================
   CineBook — Shared Toast + Responsive Utilities
   Include this <style> block and <script> in every page
   ================================================================ */

/* ── PASTE IN <style> ─────────────────────────────────── */
/*
#toast-container {
    position: fixed; top: 20px; right: 20px; z-index: 9999;
    display: flex; flex-direction: column; gap: 10px; pointer-events: none;
}
.toast {
    pointer-events: all; min-width: 280px; max-width: 360px;
    background: #141414; border: 1px solid #2a2a2a; border-radius: 12px;
    padding: 14px 18px; display: flex; align-items: flex-start; gap: 12px;
    box-shadow: 0 8px 32px rgba(0,0,0,0.6);
    animation: toastIn 0.35s cubic-bezier(0.34,1.56,0.64,1) forwards;
    font-family: 'DM Sans', sans-serif; font-size: 14px; color: #fff;
}
.toast.removing { animation: toastOut 0.25s ease forwards; }
.toast-success { border-left: 3px solid #22c55e; }
.toast-error   { border-left: 3px solid #E50914; }
.toast-info    { border-left: 3px solid #3b82f6; }
.toast-warning { border-left: 3px solid #f59e0b; }
@keyframes toastIn  { from{opacity:0;transform:translateX(100px);}to{opacity:1;transform:translateX(0);} }
@keyframes toastOut { from{opacity:1;transform:translateX(0);}to{opacity:0;transform:translateX(100px);} }
*/

/* ── PASTE IN <body> ──────────────────────────────────── */
/*
<div id="toast-container"></div>
*/

/* ── PASTE IN <script> ────────────────────────────────── */

function showToast(message, type = 'info', duration = 4000) {
    const icons = { success: '✓', error: '✕', info: 'ℹ', warning: '⚠' };
    const colors = { success: '#22c55e', error: '#E50914', info: '#3b82f6', warning: '#f59e0b' };
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.style.cssText = 'position:fixed;top:20px;right:20px;z-index:9999;display:flex;flex-direction:column;gap:10px;pointer-events:none;';
        document.body.appendChild(container);
    }
    const toast = document.createElement('div');
    toast.style.cssText = `
        pointer-events:all; min-width:280px; max-width:360px;
        background:#141414; border:1px solid #2a2a2a; border-left:3px solid ${colors[type]};
        border-radius:12px; padding:14px 18px; display:flex; align-items:flex-start; gap:12px;
        box-shadow:0 8px 32px rgba(0,0,0,0.6); font-family:'DM Sans',sans-serif;
        font-size:14px; color:#fff; animation:toastIn 0.35s cubic-bezier(0.34,1.56,0.64,1) forwards;`;
    toast.innerHTML = `
        <div style="width:22px;height:22px;border-radius:50%;background:${colors[type]}20;border:1px solid ${colors[type]}50;
                    display:flex;align-items:center;justify-content:center;flex-shrink:0;
                    font-size:12px;color:${colors[type]};font-weight:700;">${icons[type]}</div>
        <div style="flex:1;line-height:1.5;">${message}</div>
        <button onclick="this.parentElement.remove()" style="background:none;border:none;color:#555;
                cursor:pointer;font-size:18px;padding:0;line-height:1;flex-shrink:0;">✕</button>`;

    // Inject keyframes once
    if (!document.getElementById('toast-keyframes')) {
        const s = document.createElement('style');
        s.id = 'toast-keyframes';
        s.textContent = `
            @keyframes toastIn  { from{opacity:0;transform:translateX(100px);}to{opacity:1;transform:translateX(0);} }
            @keyframes toastOut { from{opacity:1;transform:translateX(0);}to{opacity:0;transform:translateX(100px);} }`;
        document.head.appendChild(s);
    }

    container.appendChild(toast);
    setTimeout(() => {
        toast.style.animation = 'toastOut 0.25s ease forwards';
        setTimeout(() => toast.remove(), 250);
    }, duration);
    return toast;
}
