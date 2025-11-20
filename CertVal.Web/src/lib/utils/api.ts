import { auth } from '$lib/stores/auth';
import { language } from '$lib/stores/language.svelte';
import { get } from 'svelte/store';
import { t } from '$lib/i18n';
import type { ApiResponse, ErrorResponseDto, ProblemDetails } from '$lib/types';

const API_BASE = import.meta.env.VITE_API_BASE_URL || '/api/v1';

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null;
}

class ApiClient {
    private isJsonResponse(response: Response): boolean {
        const ct = response.headers.get('content-type')?.toLowerCase() || '';
        return ct.includes('application/json') || ct.includes('application/problem+json') || ct.includes('+json') || ct.includes('json');
    }
    private parseFilenameFromContentDisposition(header: string | null): string | null {
        if (!header) return null;
        try {
            let cleanedHeader = header.trim();

            const encodedFilenameMatch = cleanedHeader.match(/filename\*=UTF-8''([^;,]+)/);
            if (encodedFilenameMatch && encodedFilenameMatch[1]) {
                return decodeURIComponent(encodedFilenameMatch[1]);
            }

            const filenameMatches = cleanedHeader.match(/filename="?([^";,]+)"?/g);
            if (filenameMatches && filenameMatches.length > 0) {
                const lastMatch = filenameMatches[filenameMatches.length - 1];
                const filenameMatch = lastMatch.match(/filename="?([^";,]+)"?/);
                if (filenameMatch && filenameMatch[1]) {
                    let name = filenameMatch[1].trim();
                    name = name.replace(/^UTF-8''/, '').replace(/^\w+''/, '');
                    name = name.split(';')[0].trim();
                    name = name.replace(/^["']|["']$/g, '');
                    return name;
                }
            }
        } catch {
        }
        return null;
    }

    private getAuthHeader(): Record<string, string> {
        const authState = get(auth);
        return authState.token ? { Authorization: `Bearer ${authState.token}` } : {};
    }

    private handleError(errorData: unknown, fallbackKey: string): string {
        const currentLang = language.current;

        if (isRecord(errorData)) {
            const bag = errorData['errors'];
            if (isRecord(bag)) {
                const collected: string[] = [];
                for (const value of Object.values(bag as Record<string, unknown>)) {
                    if (Array.isArray(value)) collected.push(...(value as string[]));
                    else if (typeof value === 'string') collected.push(value);
                }
                if (collected.length) return collected.join(', ');
            }

            const msg = (errorData['message'] ?? errorData['title'] ?? errorData['detail']) as string | undefined;
            if (typeof msg === 'string' && msg.trim()) return msg;
        }

        return t(fallbackKey, currentLang);
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

            if (response.ok) {
                if (this.isJsonResponse(response)) {
                    const data = (await response.json()) as T;
                    return { data };
                }
                return { data: null as unknown as T };
            }

            if (response.status === 401) {
                auth.logout();
            }

            if (this.isJsonResponse(response)) {
                const errorData = (await response.json()) as ProblemDetails | ErrorResponseDto;
                const errorMessage = this.handleError(errorData, 'errors.api.requestFailed');
                throw new Error(errorMessage);
            }

            const currentLang = language.current;
            throw new Error(t('errors.api.requestFailed', currentLang));

        } catch (error) {
            console.error('API Error:', error);
            const currentLang = language.current;
            return {
                message: error instanceof Error ? error.message : t('errors.api.unknownError', currentLang)
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

            if (response.ok) {
                if (this.isJsonResponse(response)) {
                    const data = (await response.json()) as T;
                    return { data };
                }
                return { data: null as unknown as T };
            }

            if (response.status === 401) {
                auth.logout();
            }

            if (this.isJsonResponse(response)) {
                const errorData = (await response.json()) as ProblemDetails | ErrorResponseDto;
                const errorMessage = this.handleError(errorData, 'errors.api.uploadFailed');
                throw new Error(errorMessage);
            }

            const currentLang = language.current;
            throw new Error(t('errors.api.uploadFailed', currentLang));

        } catch (error) {
            console.error('Upload Error:', error);
            const currentLang = language.current;
            return {
                message: error instanceof Error ? error.message : t('errors.api.uploadFailed', currentLang)
            };
        }
    }

    async download(endpoint: string): Promise<{ blob: Blob; filename: string } | { message: string }> {
        try {
            const response = await fetch(`${API_BASE}${endpoint}`, {
                headers: Object.assign(
                    this.getAuthHeader(),
                ),
            });

            if (response.ok) {
                const contentDisposition = response.headers.get('content-disposition');
                let filename = this.parseFilenameFromContentDisposition(contentDisposition) || 'downloaded_file';
                const blob = await response.blob();
                return { blob, filename };
            }

            if (response.status === 401) {
                auth.logout();
            }

            if (this.isJsonResponse(response)) {
                const errorData = (await response.json()) as ProblemDetails | ErrorResponseDto;
                const errorMessage = this.handleError(errorData, 'errors.api.downloadFailed');
                throw new Error(errorMessage);
            }

            const currentLang = language.current;
            throw new Error(t('errors.api.downloadFailed', currentLang));

        } catch (error) {
            console.error('API Download Error:', error);
            const currentLang = language.current;
            return {
                message: error instanceof Error ? error.message : t('errors.api.downloadFailed', currentLang)
            };
        }
    }

    async downloadAndSave(
        endpoint: string,
        opts?: { onProgress?: (percent: number) => void; suggestedFileName?: string; signal?: AbortSignal }
    ): Promise<{ success: true } | { message: string }> {
        try {
            const response = await fetch(`${API_BASE}${endpoint}`, {
                headers: Object.assign(
                    this.getAuthHeader(),
                ),
                signal: opts?.signal
            });

            if (response.ok) {
                const total = Number(response.headers.get('content-length')) || 0;
                const contentType = response.headers.get('content-type') || 'application/octet-stream';
                const headerName = this.parseFilenameFromContentDisposition(response.headers.get('content-disposition'));
                const filename = headerName || opts?.suggestedFileName || 'downloaded_file';

                if (!response.body) {
                    const blob = await response.blob();
                    this.saveBlob(blob, filename);
                    return { success: true };
                }

                const reader = response.body.getReader();
                const chunks: Uint8Array[] = [];
                let received = 0;

                try {
                    while (true) {
                        const { done, value } = await reader.read();
                        if (done) break;
                        if (value) {
                            chunks.push(value);
                            received += value.length;
                            if (total > 0 && opts?.onProgress) {
                                const percent = Math.min(100, Math.round((received / total) * 100));
                                opts.onProgress(percent);
                            }
                        }
                    }
                } finally {
                    reader.releaseLock();
                }

                const parts: BlobPart[] = chunks.map((c) => c.slice(0).buffer as ArrayBuffer);
                const blob = new Blob(parts, { type: contentType });
                this.saveBlob(blob, filename);
                if (opts?.onProgress) opts.onProgress(100);
                return { success: true };
            }

            if (response.status === 401) {
                auth.logout();
            }

            if (this.isJsonResponse(response)) {
                const errorData = (await response.json()) as ProblemDetails | ErrorResponseDto;
                const errorMessage = this.handleError(errorData, 'errors.api.downloadFailed');
                throw new Error(errorMessage);
            }

            const currentLang = language.current;
            throw new Error(t('errors.api.downloadFailed', currentLang));

        } catch (error) {
            console.error('API Download Error:', error);
            const currentLang = language.current;
            return { message: error instanceof Error ? error.message : t('errors.api.downloadFailed', currentLang) };
        }
    }

    private saveBlob(blob: Blob, filename: string) {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(url);
    }

    async uploadSingleCertificate<T>(workspaceId: string, file: File, description?: string) {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('workspaceId', workspaceId);
        if (description) {
            formData.append('description', description);
        }

        return this.upload<T>('/certificates/upload', formData);
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

        return this.upload<T>('/certificates/upload/multiple', formData);
    }

    async inviteMember<T>(workspaceId: string, email: string, role: string) {
        let requestData = {
            request: {
                email: email,
                role: role
            }
        };

        console.log('Trying nested request structure:', requestData);

        let response = await this.post<T>(`/workspaces/${workspaceId}/members/invite`, requestData);

        if (response.message && response.message.includes('validation')) {
            console.log('Nested structure failed, trying flat structure');
            requestData = {
                email: email,
                role: role
            } as any;

            response = await this.post<T>(`/workspaces/${workspaceId}/members/invite`, requestData);
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

        return this.request<T>(`/search/certificates?${searchParams.toString()}`);
    }
}

export const api = new ApiClient();