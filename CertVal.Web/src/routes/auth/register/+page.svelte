<script lang="ts">
	import { authUiState } from '$lib/stores/authUiState.svelte';
	import { AuthService } from '$lib/services/AuthService';
	import FloatingInput from '$lib/components/ui/FloatingInput.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { goto } from '$app/navigation';
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';

	let firstName = $state('');
	let lastName = $state('');
	let email = $state('');
	let password = $state('');
	let confirmPassword = $state('');
	let loading = $state(false);
	let error = $state<string | null>(null);

	$effect(() => {
		authUiState.isTyping =
			firstName.length > 0 || lastName.length > 0 || email.length > 0 || password.length > 0;
		authUiState.isValid =
			email.includes('@') &&
			password.length >= 6 &&
			password === confirmPassword &&
			firstName.length > 0 &&
			lastName.length > 0;
	});

	async function handleRegister(e: Event) {
		e.preventDefault();
		loading = true;
		error = null;

		if (password !== confirmPassword) {
			error = 'Passwords do not match';
			loading = false;
			return;
		}

		try {
			const result = await AuthService.register({ email, firstName, lastName, password });
			if (result.data) {
				goto('/auth/login?registered=true');
			} else {
				error = result.error || 'Registration failed';
			}
		} catch (err) {
			error = 'An unexpected error occurred';
		} finally {
			loading = false;
		}
	}
</script>

<div class="auth-card" data-test-id="register-page">
	<div class="auth-card__inner">
		<div class="auth-card__header">
			<h1 class="auth-card__title">{t('auth.register.title', language.current)}</h1>
			<p class="auth-card__subtitle">{t('auth.register.tagline', language.current)}</p>
		</div>

		<form onsubmit={handleRegister} class="auth-form">
			<div class="auth-form__row">
				<FloatingInput
					id="firstName"
					label={t('auth.register.firstName', language.current)}
					bind:value={firstName}
					required
					data-test-id="register-firstname"
				/>
				<FloatingInput
					id="lastName"
					label={t('auth.register.lastName', language.current)}
					bind:value={lastName}
					required
					data-test-id="register-lastname"
				/>
			</div>

			<FloatingInput
				id="email"
				label={t('auth.register.email', language.current)}
				type="email"
				bind:value={email}
				required
				data-test-id="register-email"
			/>

			<FloatingInput
				id="password"
				label={t('auth.register.password', language.current)}
				type="password"
				bind:value={password}
				required
				data-test-id="register-password"
			/>

			<FloatingInput
				id="confirmPassword"
				label={t('auth.register.confirmPassword', language.current)}
				type="password"
				bind:value={confirmPassword}
				required
				data-test-id="register-confirm-password"
			/>

			{#if error}
				<div class="auth-error">
					<span>{error}</span>
				</div>
			{/if}

			<Button
				type="submit"
				variant="primary"
				fullWidth
				{loading}
				disabled={!authUiState.isValid}
				data-test-id="register-submit-button"
			>
				{t('auth.register.submit', language.current)}
			</Button>
		</form>

		<div class="auth-footer">
			<span>{t('auth.register.haveAccount', language.current)}</span>
			<button
				type="button"
				onclick={() => goto('/auth/login')}
				class="auth-link"
				data-test-id="register-login-link"
			>
				{t('auth.register.loginLink', language.current)}
			</button>
		</div>
	</div>
</div>

<style>
	.auth-card {
		width: 100%;
		max-width: 480px;
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
		font-size: var(--text-sm);
		color: var(--color-text-secondary);
		margin: 0;
	}

	.auth-form {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
	}

	.auth-form__row {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: var(--space-4);
	}

	@media (max-width: 480px) {
		.auth-form__row {
			grid-template-columns: 1fr;
		}
	}

	.auth-error {
		padding: var(--space-3) var(--space-4);
		background-color: var(--color-error-light);
		border: 1px solid var(--color-error);
		border-radius: var(--radius-md);
		color: var(--color-error);
		font-size: var(--text-sm);
	}

	.auth-link {
		color: var(--color-text);
		background: none;
		border: none;
		padding: 0;
		cursor: pointer;
		font-size: inherit;
		font-weight: var(--font-medium);
		text-decoration: underline;
		text-underline-offset: 3px;
		transition: color var(--transition-fast);
	}

	.auth-link:hover {
		color: var(--color-primary);
	}

	.auth-footer {
		text-align: center;
		font-size: var(--text-sm);
		color: var(--color-text-secondary);
	}

	.auth-footer .auth-link {
		margin-left: var(--space-1);
	}
</style>
