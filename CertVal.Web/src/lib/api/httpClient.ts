import { userSession } from '$lib/stores/userSession.svelte';

export type ApiResult<T> = {
    data: T | null;
    error: string | null;
    status: number;
};

const BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api/v1';

async function request<T>(
    endpoint: string,
    method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH',
    body?: unknown,
    customHeaders: Record<string, string> = {}
): Promise<ApiResult<T>> {
    const headers: Record<string, string> = {
        'Content-Type': 'application/json',
        ...customHeaders,
    };

    // Use the Runes-based store
    if (userSession.token) {
        headers['Authorization'] = `Bearer ${userSession.token}`;
    }

    try {
        const response = await fetch(`${BASE_URL}${endpoint}`, {
            method,
            headers,
            body: body ? JSON.stringify(body) : undefined,
        });

        if (!response.ok) {
            if (response.status === 401) {
                userSession.logout();
                // Optionally redirect to login
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
