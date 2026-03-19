/**
 * CineBook — Auth Utility  (wwwroot/js/auth-utils.js)
 * =====================================================
 * Place at: wwwroot/js/auth-utils.js
 * Include on every page: <script src="/js/auth-utils.js"></script>
 *
 * Tokens are now stored in httpOnly cookies set by the server.
 * The browser sends them automatically on every request.
 * The frontend NEVER reads or stores tokens — only user info (role, name).
 *
 * Flow when access token expires:
 *  1. authFetch() sends request → server returns 401
 *  2. Silently calls POST /api/Auth/refresh (sends refreshToken cookie automatically)
 *  3. Server validates refresh token, sets new cookies, returns user info
 *  4. authFetch() retries the original request → user stays on page, notices nothing
 *  5. If refresh also fails → clear user info, show toast, redirect to login
 */

/* ══════════════════════════════════════════════════════
   USER INFO  (non-sensitive — stored in localStorage)
   Tokens are in httpOnly cookies — we never touch them
══════════════════════════════════════════════════════ */

function getUser() {
    try { return JSON.parse(localStorage.getItem('cb_user') || '{}'); } catch { return {}; }
}

function setUser(data) {
    // Only save non-sensitive user info — NOT the token
    localStorage.setItem('cb_user', JSON.stringify({
        userId: data.userId,
        fullName: data.fullName,
        role: data.role
    }));
}

function clearUser() {
    localStorage.removeItem('cb_user');
}

/**
 * Call this after a successful login/register response.
 * Saves user info to localStorage (tokens are already in httpOnly cookies).
 */
function setAuth(data) {
    setUser(data);
}

function clearAuth() {
    clearUser();
    // Tell the server to clear the httpOnly cookies too
    fetch('/api/Auth/logout', { method: 'POST', credentials: 'include' }).catch(() => { });
}

/* ══════════════════════════════════════════════════════
   SESSION EXPIRED HANDLER
══════════════════════════════════════════════════════ */

let _expiredShown = false;

function _handleSessionExpired() {
    if (_expiredShown) return;
    _expiredShown = true;

    clearUser(); // clear localStorage user info

    const msg = '⏱ Session expired. Please log in again.';
    if (typeof showToast === 'function') {
        showToast(msg, 'warning', 3500);
    } else {
        const d = document.createElement('div');
        d.style.cssText = [
            'position:fixed', 'top:20px', 'right:20px', 'z-index:99999',
            'background:#141414', 'border:1px solid #2a2a2a',
            'border-left:4px solid #f59e0b', 'border-radius:12px',
            'padding:14px 20px', 'color:#fff', 'font-size:14px',
            'box-shadow:0 8px 32px rgba(0,0,0,.7)', 'max-width:340px',
            "font-family:'DM Sans',sans-serif"
        ].join(';');
        d.textContent = msg;
        document.body.appendChild(d);
        setTimeout(() => d.remove(), 4000);
    }

    setTimeout(() => {
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/Auth/Login?returnUrl=${returnUrl}`;
    }, 1800);
}

/* ══════════════════════════════════════════════════════
   SILENT REFRESH
══════════════════════════════════════════════════════ */

let _refreshPromise = null;

async function _silentRefresh() {
    if (_refreshPromise) return _refreshPromise;

    _refreshPromise = (async () => {
        try {
            // credentials:'include' sends the httpOnly refreshToken cookie automatically
            const res = await fetch('/api/Auth/refresh', {
                method: 'POST',
                credentials: 'include',
                headers: { 'Content-Type': 'application/json' }
            });

            if (!res.ok) return false;

            const data = await res.json();
            if (!data.success) return false;

            // Server set new cookies — just update user info in localStorage
            if (data.data) setUser(data.data);
            return true;

        } catch {
            return false;
        } finally {
            _refreshPromise = null;
        }
    })();

    return _refreshPromise;
}

/* ══════════════════════════════════════════════════════
   authFetch — drop-in replacement for fetch()
══════════════════════════════════════════════════════ */

/**
 * Use everywhere instead of fetch() for API calls.
 * credentials:'include' tells the browser to send the httpOnly cookies automatically.
 *
 * Examples:
 *   const res  = await authFetch('/api/profile');
 *   const res  = await authFetch('/api/movies', { method: 'POST', body: JSON.stringify(data) });
 *   const json = await res.json();
 *
 * NO manual Authorization headers needed — cookies handle it.
 */
async function authFetch(url, options = {}) {
    const opts = {
        ...options,
        credentials: 'include',   // ← sends httpOnly cookies automatically
        headers: {
            'Content-Type': 'application/json',
            ...(options.headers || {})
        }
    };

    let res;
    try {
        res = await fetch(url, opts);
    } catch (networkErr) {
        throw networkErr;
    }

    // 401 → try silent refresh
    if (res.status === 401) {
        const refreshed = await _silentRefresh();

        if (refreshed) {
            // Retry original request — new access token cookie is now set
            res = await fetch(url, opts);
            if (res.status !== 401) return res;
        }

        // Still 401 after refresh → session dead
        _handleSessionExpired();
    }

    return res;
}

/* ══════════════════════════════════════════════════════
   requireAuth — call at top of every protected page
══════════════════════════════════════════════════════ */

/**
 * Checks localStorage for user info.
 * If missing, attempts a silent refresh (user might just have refreshed the page
 * — access token cookie still valid even if localStorage was cleared).
 * Only redirects to login if refresh also fails.
 *
 * Usage:
 *   await requireAuth();
 *   await requireAuth('Admin');
 *   await requireAuth('CinemaManager');
 */
async function requireAuth(role = null) {
    let user = getUser();

    // No user info in localStorage — try to restore from cookies via refresh
    if (!user.userId) {
        const refreshed = await _silentRefresh();
        if (!refreshed) {
            const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
            window.location.href = `/Auth/Login?returnUrl=${returnUrl}`;
            return false;
        }
        user = getUser(); // re-read after refresh
    }

    // Role check
    if (role && user.role !== role) {
        window.location.href = '/';
        return false;
    }

    return true;
}

/* ══════════════════════════════════════════════════════
   HELPERS
══════════════════════════════════════════════════════ */

function authLogout() {
    // Tell server to clear httpOnly cookies
    fetch('/api/Auth/logout', { method: 'POST', credentials: 'include' })
        .finally(() => {
            clearUser();
            window.location.href = '/Auth/Login';
        });
}

function redirectAfterLogin(role) {
    const params = new URLSearchParams(window.location.search);
    const returnUrl = params.get('returnUrl');
    if (returnUrl) { window.location.href = decodeURIComponent(returnUrl); return; }
    if (role === 'Admin') window.location.href = '/Admin/Movies';
    else if (role === 'CinemaManager') window.location.href = '/Manager/Cinema';
    else window.location.href = '/';
}

// Legacy compat — keep getToken() returning null so existing code doesn't crash
// All auth is cookie-based now; no token needed in JS
function getToken() { return null; }
