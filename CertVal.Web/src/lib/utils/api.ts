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

            let data;
            const contentType = response.headers.get('content-type');
            
            if (contentType && contentType.includes('application/json')) {
                data = await response.json();
            } else {
                const text = await response.text();
                data = text ? { message: text } : {};
            }

            if (!response.ok) {
                if (response.status === 400 && data.errors) {
                    const errorMessages = [];
                    for (const [field, messages] of Object.entries(data.errors)) {
                        if (Array.isArray(messages)) {
                            errorMessages.push(...messages);
                        } else {
                            errorMessages.push(messages);
                        }
                    }
                    throw new Error(errorMessages.join(', ') || 'Validation error');
                }
                
                throw new Error(data.message || data.title || 'Request failed');
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

            let data;
            const contentType = response.headers.get('content-type');
            
            if (contentType && contentType.includes('application/json')) {
                data = await response.json();
            } else {
                const text = await response.text();
                data = text ? { message: text } : {};
            }

            if (!response.ok) {
                if (response.status === 400 && data.errors) {
                    const errorMessages = [];
                    for (const [field, messages] of Object.entries(data.errors)) {
                        if (Array.isArray(messages)) {
                            errorMessages.push(...messages);
                        } else {
                            errorMessages.push(messages);
                        }
                    }
                    throw new Error(errorMessages.join(', ') || 'Upload failed');
                }
                
                throw new Error(data.message || data.title || 'Upload failed');
            }

            return { data };
        } catch (error) {
            console.error('Upload Error:', error);
            return {
                message: error instanceof Error ? error.message : 'Upload failed'
            };
        }
    }

    async inviteMember<T>(workspaceId: string, email: string, role: string) {
        let requestData = {
            request: {
                email: email,
                role: role
            }
        };

        console.log('Trying nested request structure:', requestData);
        
        let response = await this.post<T>(`/v1/workspaces/${workspaceId}/members/invite`, requestData);
        
        if (response.message && response.message.includes('validation')) {
            console.log('Nested structure failed, trying flat structure');
            requestData = {
                email: email,
                role: role
            } as any;
            
            response = await this.post<T>(`/v1/workspaces/${workspaceId}/members/invite`, requestData);
        }

        return response;
    }
}

export const api = new ApiClient();