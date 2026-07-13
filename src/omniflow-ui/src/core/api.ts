import axios from 'axios';
import type { AxiosRequestConfig } from 'axios';
import { API_URL, CONNECT_URL, TOKEN_KEY, REFRESH_TOKEN_KEY } from './config';

// The base instance configuration pointing to the OmniFlow API.
export const api = axios.create({
    baseURL: API_URL,
    timeout: 10000,
});

/**
 * List endpoints migrated to server-side grids return PagedResponse
 * ({ items, totalCount }); legacy ones still return raw arrays.
 * Components that just need "the list" (pickers, portals) use this to
 * accept both shapes.
 */
export function unwrapList<T = any>(data: any): T[] {
    if (Array.isArray(data)) return data;
    if (data && Array.isArray(data.items)) return data.items;
    return [];
}

// Request Interceptor: attach language + bearer token.
api.interceptors.request.use((config) => {
    const userLanguage = localStorage.getItem('app-lang') || 'en-US';
    config.headers['Accept-Language'] = userLanguage;

    const token = localStorage.getItem(TOKEN_KEY);
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
}, (error) => Promise.reject(error));

// ── Silent refresh-token flow ──────────────────────────────────────────────
// On a 401 we try the refresh_token grant exactly once. Concurrent 401s while a
// refresh is in flight are queued and replayed with the new token, so a burst of
// parallel requests triggers only one refresh. A failed refresh logs the user out.
let isRefreshing = false;
let refreshWaiters: Array<(token: string | null) => void> = [];

function notifyWaiters(token: string | null) {
    refreshWaiters.forEach((resolve) => resolve(token));
    refreshWaiters = [];
}

async function requestNewAccessToken(): Promise<string | null> {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    if (!refreshToken) return null;

    try {
        const params = new URLSearchParams();
        params.append('grant_type', 'refresh_token');
        params.append('refresh_token', refreshToken);
        params.append('scope', 'openid email profile roles offline_access');

        // Bare axios (not `api`) so this call is never itself intercepted.
        const response = await axios.post(`${CONNECT_URL}/token`, params, {
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        });

        const { access_token, refresh_token } = response.data;
        localStorage.setItem(TOKEN_KEY, access_token);
        if (refresh_token) localStorage.setItem(REFRESH_TOKEN_KEY, refresh_token);
        return access_token;
    } catch {
        return null;
    }
}

// Response Interceptor: silent refresh, then structured error surfacing.
api.interceptors.response.use((response) => response, async (error) => {
    const original = error.config as (AxiosRequestConfig & { _retry?: boolean }) | undefined;
    const status = error.response?.status;

    // Attempt a one-shot silent refresh on the first 401 of a request.
    if (status === 401 && original && !original._retry && localStorage.getItem(REFRESH_TOKEN_KEY)) {
        original._retry = true;

        if (isRefreshing) {
            // A refresh is already running — wait for it, then replay.
            const newToken = await new Promise<string | null>((resolve) => refreshWaiters.push(resolve));
            if (newToken) {
                original.headers = { ...original.headers, Authorization: `Bearer ${newToken}` };
                return api(original);
            }
        } else {
            isRefreshing = true;
            const newToken = await requestNewAccessToken();
            isRefreshing = false;
            notifyWaiters(newToken);

            if (newToken) {
                original.headers = { ...original.headers, Authorization: `Bearer ${newToken}` };
                return api(original);
            }
        }

        // Refresh failed — fall through to logout below.
        window.dispatchEvent(new CustomEvent('auth-error'));
        return Promise.reject(error);
    }

    if (status === 401) {
        window.dispatchEvent(new CustomEvent('auth-error'));
    }

    // The API returns RFC 7807 ProblemDetails; messageKey/traceId/errors are
    // custom extensions serialized at the top level of the body.
    const problem = error.response?.data;
    const messageKey = problem?.messageKey || 'api.error.generic';
    const traceId = problem?.traceId;
    const errors = problem?.errors; // field-keyed validation errors, if any

    window.dispatchEvent(new CustomEvent('api-error', {
        detail: { messageKey, traceId, errors, detail: problem?.detail }
    }));

    console.error('API Error:', status, problem);
    return Promise.reject(error);
});
