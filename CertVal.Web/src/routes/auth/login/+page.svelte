<script lang="ts">
	import { onMount } from 'svelte';
	import { authUiState } from '$lib/stores/authUiState.svelte';
	import { AuthService } from '$lib/services/AuthService';
	import FloatingInput from '$lib/components/ui/FloatingInput.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import GoogleSignInButton from '$lib/components/auth/GoogleSignInButton.svelte';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
	import { goto } from '$app/navigation';
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';

	let email = $state('');
	let password = $state('');
	let loading = $state(false);
	let error = $state<string | null>(null);
	let rememberMe = $state(false);

	$effect(() => {
		authUiState.isTyping = email.length > 0 || password.length > 0;
		authUiState.isValid = email.includes('@') && password.length >= 6;
	});

	async function handleGoogleSuccess(token: string) {
		loading = true;
		error = null;
		try {
			const result = await AuthService.googleLogin(token);
			if (result.data) {
				goto('/dashboard');
			} else {
				error = result.error || t('errors.googleLoginFailed', language.current);
			}
		} catch (err) {
			error = t('errors.network', language.current);
		} finally {
			loading = false;
		}
	}

	function handleGoogleError(msg: string) {
		error = msg;
	}

	async function handleLogin(e: Event) {
		e.preventDefault();
		loading = true;
		error = null;

		try {
			const result = await AuthService.login({ email, password });
			if (result.data) {
				goto('/dashboard');
			} else {
				error = result.error || 'Login failed';
			}
		} catch (err) {
			error = 'An unexpected error occurred';
		} finally {
			loading = false;
		}
	}
</script>

<div class="auth-card" data-test-id="login-page">
	<div class="auth-card__inner">
		<div class="auth-card__header">
			<h1 class="auth-card__title">{t('auth.login.welcome', language.current)}</h1>
			<p class="auth-card__subtitle">{t('auth.login.title', language.current)}</p>
		</div>

		<form onsubmit={handleLogin} class="auth-form">
			<FloatingInput
				id="email"
				label={t('auth.login.email', language.current)}
				type="email"
				bind:value={email}
				required
				data-test-id="login-email"
			/>

			<FloatingInput
				id="password"
				label={t('auth.login.password', language.current)}
				type="password"
				bind:value={password}
				required
				data-test-id="login-password"
			/>

			{#if error}
				<div class="auth-error">
					<span>{error}</span>
				</div>
			{/if}

			<div class="auth-form__options">
				<label class="auth-checkbox">
					<input
						type="checkbox"
						bind:checked={rememberMe}
						data-test-id="login-remember-me-checkbox"
					/>
					<span>{t('auth.login.rememberMe', language.current)}</span>
				</label>
				<button
					type="button"
					onclick={() => goto('/auth/forgot-password')}
					class="auth-link"
					data-test-id="login-forgot-password-button"
				>
					{t('auth.login.forgot', language.current)}
				</button>
			</div>

			<Button
				type="submit"
				variant="primary"
				fullWidth
				{loading}
				disabled={!authUiState.isValid}
				data-test-id="login-submit-button"
			>
				{t('auth.login.submit', language.current)}
			</Button>
		</form>

		<div class="auth-divider">
			<span>{t('auth.login.orContinueWith', language.current)}</span>
		</div>

		<GoogleSignInButton onSuccess={handleGoogleSuccess} onError={handleGoogleError} />

		<div class="auth-footer">
			<span>{t('auth.login.noAccount', language.current)}</span>
			<button
				type="button"
				onclick={() => goto('/auth/register')}
				class="auth-link"
				data-test-id="login-signup-button"
			>
				{t('auth.login.signup', language.current)}
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
		font-size: var(--text-sm);
		color: var(--color-text-secondary);
		margin: 0;
	}

	.auth-form {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
	}

	.auth-error {
		padding: var(--space-3) var(--space-4);
		background-color: var(--color-error-light);
		border: 1px solid var(--color-error);
		border-radius: var(--radius-md);
		color: var(--color-error);
		font-size: var(--text-sm);
	}

	.auth-form__options {
		display: flex;
		align-items: center;
		justify-content: space-between;
		font-size: var(--text-sm);
		margin-top: var(--space-1);
	}

	.auth-checkbox {
		display: inline-flex;
		align-items: center;
		gap: var(--space-2);
		cursor: pointer;
		color: var(--color-text-secondary);
	}

	.auth-checkbox input {
		width: 16px;
		height: 16px;
		accent-color: var(--color-primary);
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

	.auth-footer .auth-link {
		margin-left: var(--space-1);
	}
</style>
