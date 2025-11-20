import { writable } from 'svelte/store';
import type { User } from '$lib/types';
import { browser } from '$app/environment';

interface AuthState {
    user: User | null;
    token: string | null;
    isAuthenticated: boolean;
    isLoading: boolean;
}

function createAuthStore() {
    const { subscribe, set, update } = writable<AuthState>({
        user: null,
        token: null,
        isAuthenticated: false,
        isLoading: false
    });

    if (browser) {
        const token = localStorage.getItem('auth_token');
        const userStr = localStorage.getItem('auth_user');

        if (token && userStr) {
            try {
                const user = JSON.parse(userStr);
                set({
                    user,
                    token,
                    isAuthenticated: true,
                    isLoading: false
                });
            } catch {
                localStorage.removeItem('auth_token');
                localStorage.removeItem('auth_user');
            }
        }
    }

    return {
        subscribe,
        login: (token: string, user: User) => {
            if (browser) {
                localStorage.setItem('auth_token', token);
                localStorage.setItem('auth_user', JSON.stringify(user));
            }
            set({
                user,
                token,
                isAuthenticated: true,
                isLoading: false
            });
        },
        logout: () => {
            if (browser) {
                localStorage.removeItem('auth_token');
                localStorage.removeItem('auth_user');
            }
            set({
                user: null,
                token: null,
                isAuthenticated: false,
                isLoading: false
            });
        },
        setLoading: (loading: boolean) => {
            update(state => ({ ...state, isLoading: loading }));
        },
        setToken: (token: string) => {
            if (browser) {
                localStorage.setItem('auth_token', token);
            }
            update(state => ({ ...state, token, isAuthenticated: !!token }));
        },
        setUser: (user: User) => {
            if (browser) {
                localStorage.setItem('auth_user', JSON.stringify(user));
            }
            update(state => ({ ...state, user }));
        }
    };
}

export const auth = createAuthStore();