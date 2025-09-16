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

            let data;
            const contentType = response.headers.get('content-type');

            if (contentType && contentType.includes('application/json')) {
                data = await response.json();
            } else {
                const text = await response.text();
                data = text ? { message: text } : {};
            }

            if (!response.ok) {
                if (response.status === 401) {
                    auth.logout();
                }

                if (response.status === 400 && data.errors) {
                    const errorMessages = [];
                    for (const [field, messages] of Object.entries(data.errors)) {
                        if (Array.isArray(messages)) {
                            errorMessages.push(...messages);
                        } else {
                            errorMessages.push(messages as string);
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

            let data;
            const contentType = response.headers.get('content-type');

            if (contentType && contentType.includes('application/json')) {
                data = await response.json();
            } else {
                const text = await response.text();
                data = text ? { message: text } : {};
            }

            if (!response.ok) {
                 if (response.status === 401) {
                    auth.logout();
                }

                if (response.status === 400 && data.errors) {
                    const errorMessages = [];
                    for (const [field, messages] of Object.entries(data.errors)) {
                        if (Array.isArray(messages)) {
                            errorMessages.push(...messages);
                        } else {
                            errorMessages.push(messages as string);
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

    async uploadSingleCertificate<T>(workspaceId: string, file: File, description?: string) {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('workspaceId', workspaceId);
        if (description) {
            formData.append('description', description);
        }

        return this.upload<T>('/v1/certificates/upload', formData);
    }

    async uploadMultipleCertificates<T>(workspaceId: string, files: FileList | File[], description?: string) {
        const formData = new FormData();
        formData.append('workspaceId', workspaceId);

        if (description) {
            formData.append('description', description);
        }

        const fileArray = Array.from(files);
        fileArray.forEach(file => {
            formData.append('files', file);
        });

        console.log('Uploading files:', fileArray.map(f => f.name));
        console.log('FormData entries:');
        for (const [key, value] of formData.entries()) {
            console.log(key, value instanceof File ? `File: ${value.name}` : value);
        }

        return this.upload<T>('/v1/certificates/upload/multiple', formData);
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

    async searchCertificates<T>(params: {
        query?: string;
        workspaceId?: string;
        statusFilter?: 'All' | 'Valid' | 'Expiring' | 'Expired';
        daysUntilExpiry?: number;
        format?: string;
        pageSize?: number;
        pageNumber?: number;
    }) {
        const searchParams = new URLSearchParams();

        if (params.query) searchParams.set('query', params.query);
        if (params.workspaceId) searchParams.set('workspaceId', params.workspaceId);
        if (params.statusFilter && params.statusFilter !== 'All') {
            searchParams.set('statusFilter', params.statusFilter);
        }
        if (params.daysUntilExpiry) searchParams.set('daysUntilExpiry', params.daysUntilExpiry.toString());
        if (params.format) searchParams.set('format', params.format);
        if (params.pageSize) searchParams.set('pageSize', params.pageSize.toString());
        if (params.pageNumber) searchParams.set('pageNumber', params.pageNumber.toString());

        return this.request<T>(`/v1/search/certificates?${searchParams.toString()}`);
    }
}

export const api = new ApiClient();