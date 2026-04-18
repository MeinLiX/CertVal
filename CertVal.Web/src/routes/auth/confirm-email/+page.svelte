<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	import { auth } from '$lib/stores/auth';
	import { userSession } from '$lib/stores/userSession.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import type { LoginResponse } from '$lib/types';

	let message = $state(t('auth.confirm.processing', language.current));
	let isSuccess = $state(false);
	let isLoggingIn = $state(false);
	let isError = $state(false);

	onMount(async () => {
		const token = page.url.searchParams.get('token');
		if (!token) {
			message = t('auth.confirm.invalidToken', language.current);
			isError = true;
			return;
		}

		try {
			const response = await api.post<LoginResponse>('/auth/confirm-email', { token });
			if (response.data) {
				message = t('auth.confirm.success', language.current);
				isSuccess = true;
				isLoggingIn = true;

				auth.login(response.data.token, response.data.user);
				userSession.login(response.data.token, response.data.user);

				setTimeout(() => {
					goto('/dashboard');
				}, 1500);
			} else {
				message = response.message || t('errors.general', language.current);
				isError = true;
			}
		} catch (error) {
			message = t('errors.network', language.current);
			isError = true;
		}
	});
</script>

<svelte:head>
	<title>{t('auth.confirm.title', language.current)} - CertVal</title>
</svelte:head>

<div class="auth-card" data-test-id="confirm-email-card">
	<div class="auth-card__inner">
		<div class="auth-card__header">
			<h1 class="auth-card__title">{t('auth.confirm.title', language.current)}</h1>
		</div>

		<p class="auth-message">{message}</p>

		{#if isSuccess}
			{#if isLoggingIn}
				<div class="auth-loading">
					<span class="spinner"></span>
					<p class="auth-loading__text">{t('auth.confirm.redirecting', language.current)}</p>
				</div>
			{:else}
				<Button
					variant="primary"
					onclick={() => goto('/dashboard')}
					data-test-id="confirm-email-continue-button"
				>
					{t('auth.confirm.continue', language.current)}
				</Button>
			{/if}
		{/if}

		{#if isError}
			<Button
				variant="primary"
				onclick={() => goto('/auth/login')}
				data-test-id="confirm-email-login-button"
			>
				{t('auth.confirm.login', language.current)}
			</Button>
		{/if}
	</div>
</div>

<style>
	.auth-card {
		width: 100%;
		max-width: 420px;
	}

	.auth-card__inner {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: var(--space-6);
		text-align: center;
	}

	.auth-card__header {
		margin-bottom: var(--space-2);
	}

	.auth-card__title {
		font-family: var(--font-display);
		font-size: var(--text-3xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		line-height: var(--leading-tight);
		color: var(--color-text);
		margin: 0;
	}

	.auth-message {
		font-size: var(--text-lg);
		color: var(--color-text-secondary);
	}

	.auth-loading {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: var(--space-3);
	}

	.spinner {
		width: 24px;
		height: 24px;
		border: 2px solid var(--color-border);
		border-top-color: var(--color-primary);
		border-radius: 50%;
		animation: spin 0.8s linear infinite;
	}

	@keyframes spin {
		to {
			transform: rotate(360deg);
		}
	}

	.auth-loading__text {
		font-size: var(--text-sm);
		color: var(--color-text-muted);
	}
</style>
