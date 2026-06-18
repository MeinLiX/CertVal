<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import Button from '$lib/components/ui/Button.svelte';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
	import type { AuditLogEntry } from '$lib/types';

	const workspaceId = $derived(page.params.id);
	const lang = $derived(language.current);

	let entries = $state<AuditLogEntry[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await load();
	});

	async function load() {
		isLoading = true;
		error = null;
		const res = await api.get<AuditLogEntry[]>(`/workspaces/${workspaceId}/audit?take=200`);
		if (res.data) entries = res.data;
		else error = res.message ?? 'Failed to load audit log';
		isLoading = false;
	}

	function categoryClass(category: string): string {
		switch (category) {
			case 'Certificate':
				return 'cat--cert';
			case 'Workspace':
				return 'cat--ws';
			case 'Notification':
				return 'cat--notif';
			case 'ApiToken':
				return 'cat--token';
			default:
				return 'cat--other';
		}
	}
</script>

<svelte:head>
	<title>{t('workspaces.auditLogTitle', lang)} – CertVal</title>
</svelte:head>

<div class="audit">
	<div class="audit__head">
		<button type="button" class="audit__back" onclick={() => goto(`/workspaces/${workspaceId}`)}>
			← {t('workspaces.auditLogBack', lang)}
		</button>
		<h1>{t('workspaces.auditLogTitle', lang)}</h1>
	</div>

	{#if isLoading}
		<GlobalLoader />
	{:else if error}
		<p class="audit__error">{error}</p>
		<Button variant="secondary" onclick={load}>↻</Button>
	{:else if entries.length === 0}
		<p class="audit__empty">{t('workspaces.auditLogEmpty', lang)}</p>
	{:else}
		<ul class="timeline">
			{#each entries as e (e.id)}
				<li class="event">
					<span class="event__badge {categoryClass(e.category)}">{e.category}</span>
					<div class="event__body">
						<span class="event__desc">{e.description}</span>
						<span class="event__time">{formatDateTime(e.occurredAt)}</span>
					</div>
				</li>
			{/each}
		</ul>
	{/if}
</div>

<style>
	.audit {
		display: flex;
		flex-direction: column;
		gap: var(--space-5);
		max-width: 760px;
	}
	.audit__head {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}
	.audit__back {
		align-self: flex-start;
		background: none;
		border: 0;
		color: var(--color-text-secondary);
		cursor: pointer;
		font-size: var(--text-sm);
		padding: 0;
	}
	.audit__back:hover {
		color: var(--color-text);
	}
	h1 {
		font-family: var(--font-display);
		margin: 0;
	}
	.audit__error {
		color: var(--color-error);
	}
	.audit__empty {
		color: var(--color-text-muted);
	}
	.timeline {
		list-style: none;
		margin: 0;
		padding: 0;
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}
	.event {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		padding: var(--space-3);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		background: var(--color-surface);
	}
	.event__badge {
		flex-shrink: 0;
		font-size: var(--text-xs);
		font-weight: var(--font-semibold);
		padding: 0.15rem 0.5rem;
		border-radius: var(--radius-sm);
		min-width: 84px;
		text-align: center;
	}
	.cat--cert {
		background: var(--color-primary-light);
		color: var(--color-primary);
	}
	.cat--ws {
		background: var(--color-success-light);
		color: var(--color-success);
	}
	.cat--notif {
		background: var(--color-warning-light, #fef3c7);
		color: var(--color-warning, #b45309);
	}
	.cat--token {
		background: var(--color-surface-inset);
		color: var(--color-text-secondary);
	}
	.cat--other {
		background: var(--color-surface-inset);
		color: var(--color-text-muted);
	}
	.event__body {
		display: flex;
		justify-content: space-between;
		align-items: center;
		gap: var(--space-3);
		flex: 1;
		min-width: 0;
	}
	.event__desc {
		font-size: var(--text-sm);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.event__time {
		flex-shrink: 0;
		color: var(--color-text-muted);
		font-size: var(--text-xs);
		font-variant-numeric: tabular-nums;
	}
</style>
