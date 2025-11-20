import { api } from '$lib/utils/api';
import type { ApiResponse, User, ChangePasswordRequest } from '$lib/types';

export class UserService {
    static async getProfile(): Promise<ApiResponse<User>> {
        return api.get<User>('/users/me');
    }

    static async updateProfile(data: Partial<User>): Promise<ApiResponse<User>> {
        return api.put<User>('/users/me', data);
    }

    static async changePassword(data: ChangePasswordRequest): Promise<ApiResponse<void>> {
        return api.post<void>('/users/change-password', data);
    }
}
