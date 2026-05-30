import { browser } from '$app/environment';
import { goto } from '$app/navigation';
import { userSession } from '$lib/stores/userSession.svelte';
import { auth } from '$lib/stores/auth';

const BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api/v1';

// Endpoints that must never trigger a token refresh (they either don't require auth
// or are part of the auth flow itself, where a refresh would cause a loop).
const NO_REFRESH_ENDPOINTS = [
    '/auth/login',
    '/auth/register',
    '/auth/refresh',
    '/auth/logout',
    '/auth/forgot-password',
    '/auth/reset-password',
    '/auth/validate-reset-token',
    '/auth/confirm-email',
    '/auth/resend-confirmation',
];

export function isRefreshableEndpoint(endpoint: string): boolean {
    return !NO_REFRESH_ENDPOINTS.some((path) => endpoint.startsWith(path));
}

interface RefreshResponse {
    token: string;
    expiresAt: string;
    refreshToken: string;
    refreshTokenExpiresAt: string;
    user: {
        id: string;
        email: string;
        firstName: string;
        lastName: string;
    };
}

// Ensures only a single refresh request is in flight at a time. Concurrent callers
// (e.g. several API requests failing with 401 simultaneously) await the same promise.
let refreshPromise: Promise<boolean> | null = null;

/**
 * Attempts to silently obtain a new access token using the stored refresh token.
 * Returns true when the session was successfully renewed.
 */
export function refreshAccessToken(): Promise<boolean> {
    refreshPromise ??= performRefresh().finally(() => {
        refreshPromise = null;
    });
    return refreshPromise;
}

async function performRefresh(): Promise<boolean> {
    const refreshToken = userSession.refreshToken;
    if (!refreshToken) {
        return false;
    }

    try {
        const response = await fetch(`${BASE_URL}/auth/refresh`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken }),
        });

        if (!response.ok) {
            return false;
        }

        const data: RefreshResponse = await response.json();
        userSession.setTokens(data.token, data.refreshToken, data.expiresAt);
        auth.setToken(data.token);
        if (data.user) {
            userSession.updateUser(data.user);
        }
        return true;
    } catch {
        return false;
    }
}

/**
 * Clears the session and redirects to the login page. Used when the session can no
 * longer be renewed, so the user understands they must re-authenticate.
 */
export function forceLogout(): void {
    userSession.logout();
    auth.logout();

    if (browser && window.location.pathname !== '/auth/login') {
        const redirectTo = encodeURIComponent(window.location.pathname + window.location.search);
        void goto(`/auth/login?redirectTo=${redirectTo}`);
    }
}
