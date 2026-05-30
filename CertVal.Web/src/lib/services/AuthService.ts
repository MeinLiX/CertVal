import { httpClient } from '$lib/api/httpClient';
import { userSession } from '$lib/stores/userSession.svelte';
import { auth } from '$lib/stores/auth';
import type { User } from '$lib/types';

export interface LoginCommand {
    email: string;
    password?: string;
    twoFactorCode?: string;
    twoFactorRecoveryCode?: string;
}

export interface RegisterUserCommand {
    email: string;
    password?: string;
    firstName?: string;
    lastName?: string;
}

export interface ConfirmEmailCommand {
    userId: string;
    code: string;
}

export interface LoginResponse {
    token: string;
    expiresAt: string;
    refreshToken: string;
    refreshTokenExpiresAt: string;
    user: User;
}

export class AuthService {
    static async login(command: LoginCommand) {
        const result = await httpClient.post<LoginResponse>('/auth/login', command);
        if (result.data) {
            userSession.login(result.data.token, result.data.user, result.data.refreshToken, result.data.expiresAt);
            auth.login(result.data.token, result.data.user);
        }
        return result;
    }

    static async googleLogin(idToken: string) {
        const result = await httpClient.post<LoginResponse>('/auth/login/google', { idToken });
        if (result.data) {
            userSession.login(result.data.token, result.data.user, result.data.refreshToken, result.data.expiresAt);
            auth.login(result.data.token, result.data.user);
        }
        return result;
    }

    static async register(command: RegisterUserCommand) {
        return await httpClient.post<User>('/auth/register', command);
    }

    static async getProfile() {
        return await httpClient.get<User>('/auth/profile');
    }

    static async confirmEmail(command: ConfirmEmailCommand) {
        const result = await httpClient.post<LoginResponse>('/auth/confirm-email', command);
        if (result.data) {
            userSession.login(result.data.token, result.data.user, result.data.refreshToken, result.data.expiresAt);
            auth.login(result.data.token, result.data.user);
        }
        return result;
    }

    static async logout() {
        const refreshToken = userSession.refreshToken;
        if (refreshToken) {
            // Best-effort server-side revocation; ignore failures so logout always proceeds.
            await httpClient.post('/auth/logout', { refreshToken });
        }
        userSession.logout();
        auth.logout();
    }
}
