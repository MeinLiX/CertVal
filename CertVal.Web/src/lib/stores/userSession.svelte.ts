import { browser } from '$app/environment';

interface UserProfile {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
}

class UserSession {
    token = $state<string | null>(null);
    user = $state<UserProfile | null>(null);
    isAuthenticated = $derived(!!this.token);

    constructor() {
        if (browser) {
            this.token = localStorage.getItem('accessToken');
            const storedUser = localStorage.getItem('userProfile');
            if (storedUser) {
                try {
                    this.user = JSON.parse(storedUser);
                } catch (e) {
                    console.error('Failed to parse user profile', e);
                }
            }
        }
    }

    login(token: string, user: UserProfile) {
        this.token = token;
        this.user = user;
        if (browser) {
            localStorage.setItem('accessToken', token);
            localStorage.setItem('userProfile', JSON.stringify(user));
        }
    }

    logout() {
        this.token = null;
        this.user = null;
        if (browser) {
            localStorage.removeItem('accessToken');
            localStorage.removeItem('userProfile');
        }
    }

    updateUser(user: UserProfile) {
        this.user = user;
        if (browser) {
            localStorage.setItem('userProfile', JSON.stringify(user));
        }
    }
}

export const userSession = new UserSession();
