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
	let googleLoaded = $state(false);
	let error = $state<string | null>(null);

	onMount(() => {
		if (window.google) {
			googleLoaded = true;
		}
	});

	$effect(() => {
		authUiState.isTyping = email.length > 0 || password.length > 0;
		authUiState.isValid = email.includes('@') && password.length >= 6;
	});

	function handleGoogleLoad() {
		googleLoaded = true;
	}

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

{#if !googleLoaded}
	<GlobalLoader />
{/if}

<div class="w-full max-w-md" class:invisible={!googleLoaded}>
	<div
		class="card bg-base-100/20 overflow-hidden border border-white/20 shadow-2xl backdrop-blur-xl"
	>
		<div class="card-body gap-6 p-8">
			<div class="mb-2 text-center">
				<h1
					class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-3xl font-bold text-transparent"
				>
					{t('auth.login.welcome', language.current)}
				</h1>
				<p class="text-base-content/60 mt-2">{t('auth.login.title', language.current)}</p>
			</div>

			<form onsubmit={handleLogin} class="flex flex-col gap-4">
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
					<div class="alert alert-error rounded-lg py-2 text-sm">
						<span>{error}</span>
					</div>
				{/if}

				<div class="flex items-center justify-between text-sm">
					<label class="label cursor-pointer gap-2">
						<input type="checkbox" class="checkbox checkbox-primary checkbox-sm" />
						<span class="label-text">{t('auth.login.rememberMe', language.current)}</span>
					</label>
					<button
						type="button"
						onclick={() => goto('/auth/forgot-password')}
						class="link link-primary no-underline hover:underline"
					>
						{t('auth.login.forgot', language.current)}
					</button>
				</div>

				<Button
					type="submit"
					variant="primary"
					class="mt-2 w-full"
					{loading}
					disabled={!authUiState.isValid}
					data-testid="login-submit"
				>
					{t('auth.login.submit', language.current)}
				</Button>
			</form>

			<div class="divider text-base-content/40 text-xs">
				{t('auth.login.orContinueWith', language.current)}
			</div>

			<GoogleSignInButton
				onSuccess={handleGoogleSuccess}
				onError={handleGoogleError}
				onLoad={handleGoogleLoad}
			/>

			<div class="mt-4 text-center text-sm">
				<span class="text-base-content/60">{t('auth.login.noAccount', language.current)}</span>
				<button
					type="button"
					onclick={() => goto('/auth/register')}
					class="link link-primary ml-1 font-medium no-underline hover:underline"
				>
					{t('auth.login.signup', language.current)}
				</button>
			</div>
		</div>
	</div>
</div>
