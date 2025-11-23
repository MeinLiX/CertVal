import { api } from '$lib/utils/api';
import type { DashboardStats, Certificate, ApiResponse } from '$lib/types';

export class DashboardService {
    static async getStats(): Promise<ApiResponse<DashboardStats>> {
        return api.get<DashboardStats>('/dashboard/stats');
    }

    static async getExpiringCertificates(daysAhead: number = 30, limit: number = 10): Promise<ApiResponse<Certificate[]>> {
        return api.get<Certificate[]>(`/dashboard/expiring-certificates?daysAhead=${daysAhead}&limit=${limit}`);
    }
}
