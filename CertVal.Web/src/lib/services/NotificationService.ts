import { api } from '$lib/utils/api';
import type { NotificationRule, NotificationHistory, CreateNotificationRuleRequest, ApiResponse } from '$lib/types';

export class NotificationService {
    static async getRules(workspaceId: string): Promise<ApiResponse<NotificationRule[]>> {
        return api.get<NotificationRule[]>(`/workspaces/${workspaceId}/Notifications/rules`);
    }

    static async createRule(workspaceId: string, data: CreateNotificationRuleRequest): Promise<ApiResponse<NotificationRule>> {
        return api.post<NotificationRule>(`/workspaces/${workspaceId}/Notifications/rules`, data);
    }

    static async updateRule(workspaceId: string, ruleId: string, data: Partial<CreateNotificationRuleRequest>): Promise<ApiResponse<NotificationRule>> {
        return api.put<NotificationRule>(`/workspaces/${workspaceId}/Notifications/rules/${ruleId}`, data);
    }

    static async deleteRule(workspaceId: string, ruleId: string): Promise<ApiResponse<void>> {
        return api.delete<void>(`/workspaces/${workspaceId}/Notifications/rules/${ruleId}`);
    }

    static async getHistory(workspaceId: string): Promise<ApiResponse<NotificationHistory[]>> {
        return api.get<NotificationHistory[]>(`/workspaces/${workspaceId}/Notifications/history`);
    }
}
