import { auth } from '$lib/stores/auth';
import { get } from 'svelte/store';
import type { ApiResponse } from '$lib/types';

const API_BASE = 'http://localhost:9990/api';

class ApiClient {
    private getAuthHeader() {
        const authState = get(auth);
        return authState.token ? { Authorization: `Bearer ${authState.token}` } : {};
    }

    async request<T>(
        endpoint: string,
        options: RequestInit = {}
    ): Promise<ApiResponse<T>> {
        try {
            const response = await fetch(`${API_BASE}${endpoint}`, {
                ...options,
                headers: Object.assign(
                    {},
                    { 'Content-Type': 'application/json' },
                    this.getAuthHeader(),
                    options.headers ?? {}
                )
            });

            if (response.status === 401) {
                auth.logout();
                throw new Error('Unauthorized');
            }

            const data = await response.json();

            if (!response.ok) {
                throw new Error(data.message || 'Request failed');
            }

            return { data };
        } catch (error) {
            console.error('API Error:', error);
            return {
                message: error instanceof Error ? error.message : 'Unknown error'
            };
        }
    }

    get<T>(endpoint: string) {
        return this.request<T>(endpoint);
    }

    post<T>(endpoint: string, data?: any) {
        return this.request<T>(endpoint, {
            method: 'POST',
            body: data ? JSON.stringify(data) : undefined
        });
    }

    put<T>(endpoint: string, data?: any) {
        return this.request<T>(endpoint, {
            method: 'PUT',
            body: data ? JSON.stringify(data) : undefined
        });
    }

    delete<T>(endpoint: string) {
        return this.request<T>(endpoint, {
            method: 'DELETE'
        });
    }

    async upload<T>(endpoint: string, formData: FormData) {
        try {
            const response = await fetch(`${API_BASE}${endpoint}`, {
                method: 'POST',
                headers: Object.assign(
                    this.getAuthHeader(),
                ),
                body: formData
            });

            if (response.status === 401) {
                auth.logout();
                throw new Error('Unauthorized');
            }

            const data = await response.json();

            if (!response.ok) {
                throw new Error(data.message || 'Upload failed');
            }

            return { data };
        } catch (error) {
            console.error('Upload Error:', error);
            return {
                message: error instanceof Error ? error.message : 'Upload failed'
            };
        }
    }
}

export const api = new ApiClient();