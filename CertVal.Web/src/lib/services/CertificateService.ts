import { api } from '$lib/utils/api';
import type { Certificate, PagedResult, ApiResponse, BulkUploadResultDto } from '$lib/types';

export interface CertificateFilter {
    pageNumber: number;
    pageSize: number;
    workspaceId?: string;
    subject?: string;
    issuer?: string;
    statusFilter?: string;
    sortBy?: string;
    sortDescending?: boolean;
}

export class CertificateService {
    static async getAll(filter: CertificateFilter): Promise<ApiResponse<PagedResult<Certificate>>> {
        const params = new URLSearchParams();
        params.set('pageNumber', filter.pageNumber.toString());
        params.set('pageSize', filter.pageSize.toString());
        if (filter.workspaceId) params.set('workspaceId', filter.workspaceId);
        if (filter.subject) params.set('subject', filter.subject);
        if (filter.issuer) params.set('issuer', filter.issuer);
        if (filter.statusFilter && filter.statusFilter !== 'All') params.set('statusFilter', filter.statusFilter);
        if (filter.sortBy) params.set('sortBy', filter.sortBy);
        if (filter.sortDescending !== undefined) params.set('sortDescending', filter.sortDescending.toString());

        return api.get<PagedResult<Certificate>>(`/certificates?${params.toString()}`);
    }

    static async getById(id: string): Promise<ApiResponse<Certificate>> {
        return api.get<Certificate>(`/certificates/${id}`);
    }

    static async upload(workspaceId: string, files: FileList): Promise<ApiResponse<BulkUploadResultDto>> {
        const formData = new FormData();
        formData.append('workspaceId', workspaceId);
        for (let i = 0; i < files.length; i++) {
            formData.append('files', files[i]);
        }
        return api.upload<BulkUploadResultDto>('/certificates/upload', formData);
    }

    static async delete(id: string): Promise<ApiResponse<void>> {
        return api.delete<void>(`/certificates/${id}`);
    }
}
