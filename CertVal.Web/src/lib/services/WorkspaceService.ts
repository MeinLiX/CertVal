import { api } from '$lib/utils/api';
import type { Workspace, CreateWorkspaceRequest, UpdateWorkspaceRequest, PagedResult, ApiResponse } from '$lib/types';

export class WorkspaceService {
    static async getAll(page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<Workspace>>> {
        return api.get<PagedResult<Workspace>>(`/workspaces?pageNumber=${page}&pageSize=${pageSize}`);
    }

    static async getById(id: string): Promise<ApiResponse<Workspace>> {
        return api.get<Workspace>(`/workspaces/${id}`);
    }

    static async create(data: CreateWorkspaceRequest): Promise<ApiResponse<Workspace>> {
        return api.post<Workspace>('/workspaces', data);
    }

    static async update(id: string, data: UpdateWorkspaceRequest): Promise<ApiResponse<Workspace>> {
        return api.put<Workspace>(`/workspaces/${id}`, data);
    }

    static async delete(id: string): Promise<ApiResponse<void>> {
        return api.delete<void>(`/workspaces/${id}`);
    }

    static async getMembers(id: string): Promise<ApiResponse<any[]>> {
        return api.get<any[]>(`/workspaces/${id}/members`);
    }
}
