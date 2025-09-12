import { writable, derived } from 'svelte/store';
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

    return {
        subscribe,
        login: (token: string, user: User) => {
            if (browser) {
                localStorage.setItem('auth_token', token);
                localStorage.setItem('user', JSON.stringify(user));
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
                localStorage.removeItem('user');
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
        initialize: () => {
            if (!browser) return;
            
            const token = localStorage.getItem('auth_token');
            const userStr = localStorage.getItem('user');

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
                    localStorage.removeItem('user');
                }
            }
        }
    };
}

export const auth = createAuthStore();