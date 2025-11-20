import { api } from '$lib/utils/api';
import type { ApiResponse, ApiToken, CreateApiTokenRequest, CreateApiTokenResponse } from '$lib/types';

export class ApiTokenService {
    static async getTokens(): Promise<ApiResponse<ApiToken[]>> {
        return api.get<ApiToken[]>('/apitokens');
    }

    static async createToken(data: CreateApiTokenRequest): Promise<ApiResponse<CreateApiTokenResponse>> {
        return api.post<CreateApiTokenResponse>('/apitokens', data);
    }

    static async revokeToken(id: string): Promise<ApiResponse<void>> {
        return api.delete<void>(`/apitokens/${id}`);
    }
}
