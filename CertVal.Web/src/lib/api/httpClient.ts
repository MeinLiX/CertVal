import { userSession } from '$lib/stores/userSession.svelte';
import { refreshAccessToken, forceLogout, isRefreshableEndpoint } from '$lib/api/tokenRefresh';

export type ApiResult<T> = {
    data: T | null;
    error: string | null;
    status: number;
};

const BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api/v1';

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';

function sendRequest(
    endpoint: string,
    method: HttpMethod,
    body: unknown,
    customHeaders: Record<string, string>
): Promise<Response> {
    const headers: Record<string, string> = {
        'Content-Type': 'application/json',
        ...customHeaders,
    };

    if (userSession.token) {
        headers['Authorization'] = `Bearer ${userSession.token}`;
    }

    return fetch(`${BASE_URL}${endpoint}`, {
        method,
        headers,
        body: body ? JSON.stringify(body) : undefined,
    });
}

async function request<T>(
    endpoint: string,
    method: HttpMethod,
    body?: unknown,
    customHeaders: Record<string, string> = {}
): Promise<ApiResult<T>> {
    const refreshable = isRefreshableEndpoint(endpoint);

    // Proactively refresh when the access token is about to expire, so a long-idle
    // page transparently continues working instead of failing with a 401.
    if (refreshable && userSession.token && userSession.refreshToken && userSession.isAccessTokenExpiring()) {
        await refreshAccessToken();
    }

    try {
        let response = await sendRequest(endpoint, method, body, customHeaders);

        // Reactively refresh once on 401, then retry the original request.
        if (response.status === 401 && refreshable && userSession.refreshToken) {
            const refreshed = await refreshAccessToken();
            if (refreshed) {
                response = await sendRequest(endpoint, method, body, customHeaders);
            }
        }

        if (!response.ok) {
            // Session could not be renewed: clear it and send the user to login.
            if (response.status === 401 && refreshable) {
                forceLogout();
            }

            let errorMessage = response.statusText;
            try {
                const errorBody = await response.json();
                // Try to find error message in common problem details formats
                errorMessage = errorBody.detail || errorBody.message || errorBody.title || errorMessage;
            } catch {
                // Ignore JSON parse error
            }

            return {
                data: null,
                error: errorMessage,
                status: response.status,
            };
        }

        // Handle 204 No Content
        if (response.status === 204) {
            return {
                data: {} as T,
                error: null,
                status: response.status,
            };
        }

        const data = await response.json();
        return {
            data: data as T,
            error: null,
            status: response.status,
        };
    } catch (e) {
        return {
            data: null,
            error: e instanceof Error ? e.message : 'Network error',
            status: 0,
        };
    }
}

export const httpClient = {
    get: <T>(endpoint: string, headers?: Record<string, string>) => request<T>(endpoint, 'GET', undefined, headers),
    post: <T>(endpoint: string, body: unknown, headers?: Record<string, string>) => request<T>(endpoint, 'POST', body, headers),
    put: <T>(endpoint: string, body: unknown, headers?: Record<string, string>) => request<T>(endpoint, 'PUT', body, headers),
    delete: <T>(endpoint: string, headers?: Record<string, string>) => request<T>(endpoint, 'DELETE', undefined, headers),
    patch: <T>(endpoint: string, body: unknown, headers?: Record<string, string>) => request<T>(endpoint, 'PATCH', body, headers),
};
