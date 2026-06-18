<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDate, formatDateTime } from '$lib/utils/date';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
	import type {
		MonitoredEndpoint,
		CreateMonitoredEndpointRequest
	} from '$lib/types';

	const workspaceId = $derived(page.params.id);
	const lang = $derived(language.current);

	let endpoints = $state<MonitoredEndpoint[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let submitting = $state(false);

	let form = $state<CreateMonitoredEndpointRequest>({ host: '', port: 443, checkIntervalMinutes: 360 });

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
		const res = await api.get<MonitoredEndpoint[]>(`/workspaces/${workspaceId}/endpoints`);
		if (res.data) endpoints = res.data;
		else error = res.message ?? 'Failed to load endpoints';
		isLoading = false;
	}

	async function addEndpoint() {
		if (!form.host.trim()) return;
		submitting = true;
		error = null;
		const res = await api.post<MonitoredEndpoint>(`/workspaces/${workspaceId}/endpoints`, {
			host: form.host.trim(),
			port: form.port,
			checkIntervalMinutes: form.checkIntervalMinutes
		});
		submitting = false;
		if (res.data) {
			form = { host: '', port: 443, checkIntervalMinutes: 360 };
			await load();
		} else {
			error = res.message ?? 'Failed to add endpoint';
		}
	}

	async function toggleEnabled(e: MonitoredEndpoint) {
		const res = await api.put<MonitoredEndpoint>(`/workspaces/${workspaceId}/endpoints/${e.id}`, {
			host: e.host,
			port: e.port,
			isEnabled: !e.isEnabled,
			checkIntervalMinutes: e.checkIntervalMinutes
		});
		if (res.data) await load();
		else error = res.message ?? 'Failed to update endpoint';
	}

	async function remove(e: MonitoredEndpoint) {
		const res = await api.delete(`/workspaces/${workspaceId}/endpoints/${e.id}`);
		if (!res.message) await load();
		else error = res.message;
	}

	function gradeClass(grade?: string): string {
		if (!grade) return 'g--none';
		const l = grade.charAt(0).toUpperCase();
		if (l === 'A') return 'g--a';
		if (l === 'B' || l === 'C') return 'g--b';
		if (l === 'D') return 'g--d';
		return 'g--f';
	}
</script>

<svelte:head>
	<title>{t('workspaces.epTitle', lang)} – CertVal</title>
</svelte:head>

<div class="ep">
	<div class="ep__head">
		<button type="button" class="ep__back" onclick={() => goto(`/workspaces/${workspaceId}`)}>
			← {t('workspaces.epBack', lang)}
		</button>
		<h1>{t('workspaces.epTitle', lang)}</h1>
		<p class="ep__subtitle">{t('workspaces.epSubtitle', lang)}</p>
	</div>

	<form
		class="ep__add"
		onsubmit={(e) => {
			e.preventDefault();
			addEndpoint();
		}}
	>
		<Input label={t('workspaces.epHost', lang)} bind:value={form.host} placeholder={t('workspaces.epHostPlaceholder', lang)} />
		<Input label={t('workspaces.epPort', lang)} type="number" bind:value={form.port} />
		<Input label={t('workspaces.epInterval', lang)} type="number" bind:value={form.checkIntervalMinutes} />
		<Button type="submit" loading={submitting}>{t('workspaces.epAdd', lang)}</Button>
	</form>

	{#if error}<p class="ep__error">{error}</p>{/if}

	{#if isLoading}
		<GlobalLoader />
	{:else if endpoints.length === 0}
		<p class="ep__empty">{t('workspaces.epEmpty', lang)}</p>
	{:else}
		<div class="ep__list">
			{#each endpoints as e (e.id)}
				<div class="card" class:card--disabled={!e.isEnabled}>
					<div class="card__main">
						<span class="card__target">{e.host}:{e.port}</span>
						{#if e.leafSubject}<span class="card__subject">{e.leafSubject}</span>{/if}
					</div>
					<div class="card__stats">
						<span class="grade {gradeClass(e.lastGrade)}">{e.lastGrade ?? '—'}</span>
						<div class="stat">
							<span class="stat__label">{t('workspaces.epStatus', lang)}</span>
							<span class="stat__value">
								{#if e.lastCheckedAt}
									{e.lastReachable ? t('workspaces.epReachable', lang) : t('workspaces.epUnreachable', lang)}
								{:else}{t('workspaces.epNever', lang)}{/if}
							</span>
						</div>
						<div class="stat">
							<span class="stat__label">{t('workspaces.epExpiry', lang)}</span>
							<span class="stat__value">{e.leafNotAfter ? formatDate(e.leafNotAfter) : '—'}</span>
						</div>
						<div class="stat">
							<span class="stat__label">{t('workspaces.epLastChecked', lang)}</span>
							<span class="stat__value">{e.lastCheckedAt ? formatDateTime(e.lastCheckedAt) : '—'}</span>
						</div>
					</div>
					<div class="card__actions">
						<label class="toggle">
							<input type="checkbox" checked={e.isEnabled} onchange={() => toggleEnabled(e)} />
							<span>{t('workspaces.epEnabled', lang)}</span>
						</label>
						<Button variant="secondary" onclick={() => remove(e)}>{t('workspaces.epDelete', lang)}</Button>
					</div>
				</div>
			{/each}
		</div>
	{/if}
</div>

<style>
	.ep {
		display: flex;
		flex-direction: column;
		gap: var(--space-5);
		max-width: 900px;
	}
	.ep__head {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}
	.ep__back {
		align-self: flex-start;
		background: none;
		border: 0;
		color: var(--color-text-secondary);
		cursor: pointer;
		font-size: var(--text-sm);
		padding: 0;
	}
	.ep__back:hover {
		color: var(--color-text);
	}
	h1 {
		font-family: var(--font-display);
		margin: 0;
	}
	.ep__subtitle {
		color: var(--color-text-secondary);
		margin: 0;
		font-size: var(--text-sm);
	}
	.ep__add {
		display: grid;
		grid-template-columns: 2fr 1fr 1fr auto;
		gap: var(--space-3);
		align-items: end;
	}
	@media (max-width: 640px) {
		.ep__add {
			grid-template-columns: 1fr;
		}
	}
	.ep__error {
		color: var(--color-error);
		font-size: var(--text-sm);
	}
	.ep__empty {
		color: var(--color-text-muted);
	}
	.ep__list {
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
	}
	.card {
		display: grid;
		grid-template-columns: 1.4fr 2fr auto;
		gap: var(--space-4);
		align-items: center;
		padding: var(--space-4);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		background: var(--color-surface);
	}
	@media (max-width: 760px) {
		.card {
			grid-template-columns: 1fr;
		}
	}
	.card--disabled {
		opacity: 0.6;
	}
	.card__main {
		display: flex;
		flex-direction: column;
		gap: var(--space-1);
		min-width: 0;
	}
	.card__target {
		font-family: var(--font-mono);
		font-weight: var(--font-semibold);
	}
	.card__subject {
		font-size: var(--text-xs);
		color: var(--color-text-muted);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.card__stats {
		display: flex;
		align-items: center;
		gap: var(--space-4);
		flex-wrap: wrap;
	}
	.grade {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-width: 2.2rem;
		height: 2.2rem;
		border-radius: var(--radius-md);
		font-weight: 800;
		color: #fff;
	}
	.g--a {
		background: var(--color-success);
	}
	.g--b {
		background: #b45309;
	}
	.g--d {
		background: #c2410c;
	}
	.g--f {
		background: var(--color-error);
	}
	.g--none {
		background: var(--color-text-muted);
	}
	.stat {
		display: flex;
		flex-direction: column;
	}
	.stat__label {
		font-size: var(--text-xs);
		color: var(--color-text-muted);
	}
	.stat__value {
		font-size: var(--text-sm);
	}
	.card__actions {
		display: flex;
		align-items: center;
		gap: var(--space-3);
	}
	.toggle {
		display: inline-flex;
		align-items: center;
		gap: var(--space-2);
		font-size: var(--text-sm);
		cursor: pointer;
	}
</style>
