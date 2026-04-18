<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	import { auth } from '$lib/stores/auth';
	import Button from '$lib/components/ui/Button.svelte';

	let message = $state(t('workspaces.join.processing', language.current));
	let error = $state('');
	let isLoading = $state(true);
	let token = $state('');
	let workspaceId = $state('');

	onMount(async () => {
		token = page.url.searchParams.get('token') || '';
		workspaceId = page.params.id;

		if (!token) {
			error = t('workspaces.join.invalidToken', language.current);
			isLoading = false;
			return;
		}

		const postLoginToken = sessionStorage.getItem('invitation_token');
		if (postLoginToken === token && $auth.isAuthenticated) {
			sessionStorage.removeItem('invitation_token');
			await handleAccept();
			return;
		}

		try {
			const response = await api.get<any>(`/invitations/${token}`);
			if (response.data) {
				message = t('workspaces.join.inviteDetails', language.current, {
					workspaceName: response.data.workspaceName
				});
			} else {
				error = response.message || t('workspaces.join.invalidToken', language.current);
			}
		} catch (e) {
			error = t('workspaces.join.invalidToken', language.current);
		} finally {
			isLoading = false;
		}
	});

	async function handleAccept() {
		if (!$auth.isAuthenticated) {
			sessionStorage.setItem('invitation_token', token);
			goto(`/auth/login?redirect=${page.url.pathname}${page.url.search}`);
			return;
		}

		isLoading = true;
		error = '';
		try {
			const response = await api.post(`/invitations/${token}/accept`);
			if (!response.message) {
				goto(`/workspaces/${workspaceId}`);
			} else {
				error = response.message;
			}
		} catch (e) {
			error = t('workspaces.join.acceptError', language.current);
		} finally {
			isLoading = false;
		}
	}
</script>

<svelte:head>
	<title>{t('workspaces.join.title', language.current)}</title>
</svelte:head>

<div class="page">
	<div class="card">
		<h1 class="card__title">{t('workspaces.join.title', language.current)}</h1>

		{#if isLoading}
			<div class="state state--loading">
				<div class="spinner"></div>
				<p class="state__message">{message}</p>
			</div>
		{:else if error}
			<div class="state state--error">
				<div class="state__icon state__icon--error">
					<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<circle cx="12" cy="12" r="10" />
						<line x1="12" y1="8" x2="12" y2="12" />
						<line x1="12" y1="16" x2="12.01" y2="16" />
					</svg>
				</div>
				<p class="state__message state__message--error">{error}</p>
				<Button variant="secondary" onclick={() => goto('/auth/login')}>
					← {t('auth.login.title', language.current)}
				</Button>
			</div>
		{:else}
			<div class="state state--success">
				<div class="state__icon">
					<svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
						<rect x="3" y="3" width="7" height="7" />
						<rect x="14" y="3" width="7" height="7" />
						<rect x="14" y="14" width="7" height="7" />
						<rect x="3" y="14" width="7" height="7" />
					</svg>
				</div>
				<p class="state__message">{message}</p>
				<Button size="lg" onclick={handleAccept} loading={isLoading} class="full-width">
					{#if $auth.isAuthenticated}
						{t('workspaces.join.acceptButton', language.current)}
					{:else}
						{t('workspaces.join.loginToAccept', language.current)}
					{/if}
				</Button>
			</div>
		{/if}
	</div>
</div>

<style>
	.page {
		display: flex;
		align-items: center;
		justify-content: center;
		min-height: calc(100vh - 10rem);
		padding: var(--space-6);
	}

	.card {
		width: 100%;
		max-width: 440px;
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-xl);
		padding: var(--space-8);
	}

	.card__title {
		font-family: var(--font-display);
		font-size: var(--text-2xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		line-height: var(--leading-tight);
		color: var(--color-text);
		text-align: left;
		margin: 0 0 var(--space-6);
	}

	.state {
		display: flex;
		flex-direction: column;
		align-items: center;
		text-align: center;
		gap: var(--space-4);
	}

	.state--loading {
		padding: var(--space-8) 0;
	}

	.spinner {
		width: 40px;
		height: 40px;
		border: 3px solid var(--color-border);
		border-top-color: var(--color-primary);
		border-radius: 50%;
		animation: spin 0.8s linear infinite;
	}

	@keyframes spin {
		to { transform: rotate(360deg); }
	}

	.state__icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 80px;
		height: 80px;
		background: var(--color-surface-elevated);
		border-radius: var(--radius-xl);
		color: var(--color-text-muted);
	}

	.state__icon--error {
		background: var(--color-error-bg);
		color: var(--color-error);
	}

	.state__message {
		font-size: var(--text-lg);
		color: var(--color-text-muted);
		margin: 0;
	}

	.state__message--error {
		color: var(--color-error);
		font-weight: 500;
	}
</style>
