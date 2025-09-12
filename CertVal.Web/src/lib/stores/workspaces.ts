import { writable } from 'svelte/store';
import type { Workspace } from '$lib/types';

function createWorkspacesStore() {
    const { subscribe, set, update } = writable<Workspace[]>([]);

    return {
        subscribe,
        set,
        add: (workspace: Workspace) => {
            update(workspaces => [...workspaces, workspace]);
        },
        update: (id: string, updates: Partial<Workspace>) => {
            update(workspaces =>
                workspaces.map(w => w.id === id ? { ...w, ...updates } : w)
            );
        },
        remove: (id: string) => {
            update(workspaces => workspaces.filter(w => w.id !== id));
        }
    };
}

export const workspaces = createWorkspacesStore();