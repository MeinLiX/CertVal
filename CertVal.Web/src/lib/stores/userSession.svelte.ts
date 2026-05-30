import { browser } from '$app/environment';

interface UserProfile {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
}

const ACCESS_TOKEN_KEY = 'accessToken';
const REFRESH_TOKEN_KEY = 'refreshToken';
const EXPIRES_AT_KEY = 'tokenExpiresAt';
const USER_PROFILE_KEY = 'userProfile';

class UserSession {
    token = $state<string | null>(null);
    refreshToken = $state<string | null>(null);
    expiresAt = $state<string | null>(null);
    user = $state<UserProfile | null>(null);
    isAuthenticated = $derived(!!this.token);

    constructor() {
        if (browser) {
            this.token = localStorage.getItem(ACCESS_TOKEN_KEY);
            this.refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
            this.expiresAt = localStorage.getItem(EXPIRES_AT_KEY);
            const storedUser = localStorage.getItem(USER_PROFILE_KEY);
            if (storedUser) {
                try {
                    this.user = JSON.parse(storedUser);
                } catch (e) {
                    console.error('Failed to parse user profile', e);
                }
            }
        }
    }

    login(token: string, user: UserProfile, refreshToken?: string, expiresAt?: string) {
        this.token = token;
        this.user = user;
        this.refreshToken = refreshToken ?? null;
        this.expiresAt = expiresAt ?? null;
        if (browser) {
            localStorage.setItem(ACCESS_TOKEN_KEY, token);
            localStorage.setItem(USER_PROFILE_KEY, JSON.stringify(user));
            this.persistTokens(refreshToken, expiresAt);
        }
    }

    /**
     * Updates only the token pair, e.g. after a silent refresh.
     */
    setTokens(token: string, refreshToken?: string, expiresAt?: string) {
        this.token = token;
        this.refreshToken = refreshToken ?? this.refreshToken;
        this.expiresAt = expiresAt ?? this.expiresAt;
        if (browser) {
            localStorage.setItem(ACCESS_TOKEN_KEY, token);
            this.persistTokens(this.refreshToken ?? undefined, this.expiresAt ?? undefined);
        }
    }

    /**
     * True when the access token is missing or within `skewSeconds` of expiring.
     */
    isAccessTokenExpiring(skewSeconds = 30): boolean {
        if (!this.expiresAt) return false;
        const expiryMs = new Date(this.expiresAt).getTime();
        if (Number.isNaN(expiryMs)) return false;
        return expiryMs - Date.now() <= skewSeconds * 1000;
    }

    logout() {
        this.token = null;
        this.refreshToken = null;
        this.expiresAt = null;
        this.user = null;
        if (browser) {
            localStorage.removeItem(ACCESS_TOKEN_KEY);
            localStorage.removeItem(REFRESH_TOKEN_KEY);
            localStorage.removeItem(EXPIRES_AT_KEY);
            localStorage.removeItem(USER_PROFILE_KEY);
        }
    }

    updateUser(user: UserProfile) {
        this.user = user;
        if (browser) {
            localStorage.setItem(USER_PROFILE_KEY, JSON.stringify(user));
        }
    }

    private persistTokens(refreshToken?: string, expiresAt?: string) {
        if (refreshToken) {
            localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
        }
        if (expiresAt) {
            localStorage.setItem(EXPIRES_AT_KEY, expiresAt);
        }
    }
}

export const userSession = new UserSession();
