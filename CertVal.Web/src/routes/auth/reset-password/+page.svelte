<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import Button from '$lib/components/ui/Button.svelte';
	import FloatingInput from '$lib/components/ui/FloatingInput.svelte';

	let password = $state('');
	let confirmPassword = $state('');
	let errors = $state<Record<string, string>>({});
	let isLoading = $state(false);
	let successMessage = $state('');
	let token = $state('');

	onMount(() => {
		token = page.url.searchParams.get('token') || '';
		if (!token) {
			goto('/auth/login');
		}
	});

	async function handleSubmit(event: Event) {
		event.preventDefault();
		errors = {};
		isLoading = true;
		if (password !== confirmPassword) {
			errors.confirmPassword = t('errors.passwordsNotMatch', language.current);
			isLoading = false;
			return;
		}

		try {
			const response = await api.post('/auth/reset-password', { token, newPassword: password });
			if (response.data) {
				successMessage = t('success.passwordChanged', language.current);
				setTimeout(() => goto('/auth/login'), 2000);
			} else {
				errors.general = response.message || t('errors.general', language.current);
			}
		} catch (error) {
			errors.general = t('errors.network', language.current);
		} finally {
			isLoading = false;
		}
	}
</script>

<svelte:head>
	<title>{t('auth.reset.title', language.current)} - CertVal</title>
</svelte:head>

<div class="auth-card" data-test-id="reset-password-card">
	<div class="auth-card__inner">
		<div class="auth-card__header">
			<h1 class="auth-card__title">{t('auth.reset.title', language.current)}</h1>
		</div>

		<form onsubmit={handleSubmit} class="auth-form">
			{#if successMessage}
				<div class="auth-success">
					<span>{successMessage}</span>
				</div>
			{/if}

			{#if errors.general}
				<div class="auth-error">
					<span>{errors.general}</span>
				</div>
			{/if}

			<FloatingInput
				id="password"
				label={t('auth.reset.newPassword', language.current)}
				type="password"
				bind:value={password}
				required
				data-test-id="reset-password-input"
			/>
			<FloatingInput
				id="confirmPassword"
				label={t('auth.reset.confirmPassword', language.current)}
				type="password"
				bind:value={confirmPassword}
				required
				error={errors.confirmPassword}
				data-test-id="reset-confirm-password-input"
			/>

			<Button
				type="submit"
				variant="primary"
				loading={isLoading}
				data-test-id="reset-password-submit-button"
			>
				{t('auth.reset.submit', language.current)}
			</Button>
		</form>
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
		gap: var(--space-8);
	}

	.auth-card__header {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
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

	.auth-form {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
	}

	.auth-success {
		padding: var(--space-3) var(--space-4);
		background-color: var(--color-success-light);
		border: 1px solid var(--color-success);
		border-radius: var(--radius-md);
		color: var(--color-success);
		font-size: var(--text-sm);
	}

	.auth-error {
		padding: var(--space-3) var(--space-4);
		background-color: var(--color-error-light);
		border: 1px solid var(--color-error);
		border-radius: var(--radius-md);
		color: var(--color-error);
		font-size: var(--text-sm);
	}
</style>
