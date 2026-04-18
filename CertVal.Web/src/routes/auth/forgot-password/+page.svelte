<script lang="ts">
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import Button from '$lib/components/ui/Button.svelte';
	import FloatingInput from '$lib/components/ui/FloatingInput.svelte';
	import { goto } from '$app/navigation';

	let email = $state('');
	let errors = $state<Record<string, string>>({});
	let isLoading = $state(false);
	let successMessage = $state('');

	async function handleSubmit(event: Event) {
		event.preventDefault();
		errors = {};
		isLoading = true;
		successMessage = '';

		try {
			const response = await api.post('/auth/forgot-password', { email });
			if (response.message) {
				errors.general = response.message;
			} else {
				successMessage = t('auth.forgot.emailSent', language.current);
			}
		} catch (error) {
			errors.general = t('errors.network', language.current);
		} finally {
			isLoading = false;
		}
	}
</script>

<svelte:head>
	<title>{t('auth.forgot.title', language.current)} - CertVal</title>
</svelte:head>

<div class="auth-card" data-test-id="forgot-password-card">
	<div class="auth-card__inner">
		<div class="auth-card__header">
			<h1 class="auth-card__title">{t('auth.forgot.title', language.current)}</h1>
			<p class="auth-card__subtitle">{t('auth.forgot.subtitle', language.current)}</p>
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
				id="email"
				label={t('auth.login.email', language.current)}
				type="email"
				bind:value={email}
				required
				data-test-id="forgot-password-email-input"
			/>

			<Button
				type="submit"
				variant="primary"
				loading={isLoading}
				data-test-id="forgot-password-submit-button"
			>
				{t('auth.forgot.submit', language.current)}
			</Button>
		</form>

		<div class="auth-divider">
			<span>{t('common.or', language.current)}</span>
		</div>

		<div class="auth-footer">
			<button
				type="button"
				onclick={() => goto('/auth/login')}
				class="auth-link"
				data-test-id="forgot-password-back-button"
			>
				{t('auth.forgot.backToLogin', language.current)}
			</button>
		</div>
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

	.auth-card__subtitle {
		margin: 0;
		font-size: var(--text-sm);
		color: var(--color-text-secondary);
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

	.auth-divider {
		display: flex;
		align-items: center;
		gap: var(--space-4);
		color: var(--color-text-muted);
		font-size: var(--text-xs);
		text-transform: uppercase;
		letter-spacing: var(--tracking-wide);
	}

	.auth-divider::before,
	.auth-divider::after {
		content: '';
		flex: 1;
		height: 1px;
		background-color: var(--color-border);
	}

	.auth-footer {
		text-align: center;
		font-size: var(--text-sm);
		color: var(--color-text-secondary);
	}

	.auth-link {
		color: var(--color-text);
		font-weight: var(--font-medium);
		text-decoration: underline;
		text-underline-offset: 3px;
		background: none;
		border: 0;
		padding: 0;
		cursor: pointer;
		font-size: inherit;
		transition: color var(--transition-fast);
	}

	.auth-link:hover {
		color: var(--color-primary);
	}
</style>
